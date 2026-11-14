using System.Data;
using System.Text.Json;
using Dapper;
using ET;

namespace DAL;

/// <summary>Quyền và metadata ảnh luôn được kiểm lại trên SQL trong giao dịch đang thao tác.</summary>
public sealed partial class DAL_ANHXQUANG(IDbTransaction? transaction=null) : dbconnect(transaction)
{
    private const string AssignedScope="""
        (EXISTS(SELECT 1 FROM dbo.PHAN_CONG_ANH_XQUANG pc WITH(HOLDLOCK) WHERE pc.MaDVThucHien=s.MaDVThucHien AND pc.MaNVKyThuat=@MaNV AND pc.TrangThai=N'Đang áp dụng')
        OR(s.MaNVThucHien=@MaNV AND NOT EXISTS(SELECT 1 FROM dbo.PHAN_CONG_ANH_XQUANG pc WITH(HOLDLOCK) WHERE pc.MaDVThucHien=s.MaDVThucHien)))
        """;
    /// <summary>
    /// Lấy tối đa 200 lượt nháp còn mở thuộc dịch vụ X-quang mà kỹ thuật viên được giao; quản trị viên xem toàn bộ phạm vi này.
    /// </summary>
    public IReadOnlyList<LuotAnhDuocGiao> LayLuotDuocGiao(NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var a=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);
        if(!a.LaAdmin&&(!a.CoQuyen(QuyenLamViec.XemHoSo)||!Technician(a)))throw DAL_QUYENNGHIEPVU.TuChoi();
        return db.Query<LuotAnhDuocGiao>($"""
            SELECT DISTINCT TOP(200) l.MaLS,h.MaBN,b.HoTenBN,l.ThoiGianKham
            FROM dbo.DICH_VU_THUC_HIEN s WITH(HOLDLOCK) JOIN dbo.DICH_VU d ON d.MaDV=s.MaDV
            JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=s.MaLS JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu
            JOIN dbo.HO_SO_BENH_AN h ON h.MaHS=p.MaHS JOIN dbo.BENH_NHAN b ON b.MaBN=h.MaBN
            WHERE d.MaCK=N'CK004' AND(@LaAdmin=1 OR {AssignedScope}) AND l.TrangThai=N'Nháp'
             AND p.TrangThai IN(N'Chờ khám',N'Đang khám') ORDER BY l.ThoiGianKham,l.MaLS
            """,new{a.MaNV,a.LaAdmin},tx).ToArray();
    });
    /// <summary>
    /// Đọc ảnh theo phạm vi lần khám và dịch vụ được giao, đồng thời tính quyền tải, ẩn và phân công.
    /// </summary>
    /// <param name="maLS">Mã lần khám cần đọc ảnh.</param>
    /// <param name="hienAnhAn">true để kèm metadata ảnh đã ẩn trong danh sách; không cấp quyền mở lại tệp.</param>
    /// <param name="user">Người dùng được xác thực lại và giới hạn theo quan hệ với lần khám.</param>
    public DanhSachAnhXquang LayDanhSach(string maLS,bool hienAnhAn,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var(c,a)=Context(db,tx,maLS,user,false);
        var services=Services(db,tx,c,a);
        var rows=db.Query<ET_ANHXQUANGLANKHAM,string,AnhXquangHienThi>($"""
            SELECT x.*,n.HoTen NguoiTai FROM dbo.ANH_XQUANG_LAN_KHAM x WITH(HOLDLOCK)
            JOIN dbo.NHAN_VIEN n ON n.MaNV=x.MaNVUpload
            WHERE x.MaLS=@maLS AND(@hienAnhAn=1 OR x.TrangThai=N'Đang sử dụng')
              AND(@technician=0 OR EXISTS(SELECT 1 FROM dbo.DICH_VU_THUC_HIEN s JOIN dbo.DICH_VU d ON d.MaDV=s.MaDV
                WHERE s.MaDVThucHien=x.MaDVThucHien AND s.MaLS=x.MaLS AND {AssignedScope} AND d.MaCK=N'CK004'))
            ORDER BY x.NgayTao DESC,x.MaAnh
            """,(x,name)=>new(x,name,Editable(c)&&x.TrangThai=="Đang sử dụng"&&(a.LaAdmin||Owner(c,a)||Technician(a)&&x.MaNVUpload==a.MaNV)),
            new{maLS,hienAnhAn,technician=Technician(a),a.MaNV},transaction:tx,splitOn:"NguoiTai").ToArray();
        return new DanhSachAnhXquang(maLS,c.MaBN,c.HoTenBN,Editable(c)&&(a.LaAdmin||Owner(c,a)||services.Count>0&&Technician(a)),services,
            rows,Editable(c)&&(a.LaAdmin||Owner(c,a)));
    });

    /// <summary>
    /// Kiểm tra lượt còn mở, dịch vụ thuộc phạm vi được giao và người tải có quyền cập nhật.
    /// </summary>
    /// <returns>Ngữ cảnh người dùng đã xác thực dùng ghi nhận người tải trong metadata.</returns>
    public NguCanhNghiepVu KiemTraTaiLen(string maLS,string? maDVThucHien,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var(c,a)=Context(db,tx,maLS,user,true);
        if(!Editable(c))throw new LoiAnhXquangException("LuotDaKhoa","Lần khám đã hoàn tất hoặc không còn mở; ảnh lịch sử chỉ được xem.");
        var services=Services(db,tx,c,a);
        if(maDVThucHien is not null&&!services.Any(s=>s.MaDVThucHien==maDVThucHien))throw DAL_QUYENNGHIEPVU.TuChoi();
        if(!a.LaAdmin&&!Owner(c,a)&&(!Technician(a)||maDVThucHien is null))throw DAL_QUYENNGHIEPVU.TuChoi();
        return a;
    });
    /// <summary>
    /// Tìm ảnh đang sử dụng có cùng lần khám, SHA-256, người tải và nguồn dịch vụ dưới khóa cập nhật.
    /// </summary>
    /// <returns>Metadata ảnh trùng để dùng lại khi thử tải lần nữa, hoặc null nếu chưa có.</returns>
    public ET_ANHXQUANGLANKHAM? TimAnhTrung(string maLS,string hash,string? source,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var(_,a)=Context(db,tx,maLS,user,true);
        return db.QuerySingleOrDefault<ET_ANHXQUANGLANKHAM>("""
            SELECT TOP(1) * FROM dbo.ANH_XQUANG_LAN_KHAM WITH(UPDLOCK,HOLDLOCK)
            WHERE MaLS=@maLS AND Sha256=@hash AND MaNVUpload=@MaNV AND TrangThai=N'Đang sử dụng'
             AND(MaDVThucHien=@source OR MaDVThucHien IS NULL AND @source IS NULL) ORDER BY NgayTao,MaAnh
            """,new{maLS,hash,source,a.MaNV},tx);
    });
    /// <summary>
    /// Kiểm tra lại quyền tải và người tải trước khi chèn metadata cùng nhật ký trong giao dịch hiện tại.
    /// </summary>
    /// <remarks>Tệp ảnh do BUS lưu trước; phương thức này chỉ ghi thông tin liên kết trong SQL.</remarks>
    public ET_ANHXQUANGLANKHAM LuuMetadata(ET_ANHXQUANGLANKHAM row,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var a=new DAL_ANHXQUANG(tx).KiemTraTaiLen(row.MaLS,row.MaDVThucHien,user);
        if(row.MaNVUpload!=a.MaNV)throw DAL_QUYENNGHIEPVU.TuChoi();
        var saved=db.QuerySingle<ET_ANHXQUANGLANKHAM>("""
            INSERT dbo.ANH_XQUANG_LAN_KHAM(MaAnh,MaLS,MaDVThucHien,TenTepGoc,TenTepLuu,DuongDanTuongDoi,MimeType,KichThuocBytes,Sha256,GhiChu,MaNVUpload)
            VALUES(@MaAnh,@MaLS,@MaDVThucHien,@TenTepGoc,@TenTepLuu,@DuongDanTuongDoi,@MimeType,@KichThuocBytes,@Sha256,@GhiChu,@MaNVUpload);
            SELECT * FROM dbo.ANH_XQUANG_LAN_KHAM WHERE MaAnh=@MaAnh;
            """,row,tx);
        Audit(db,tx,a,"XR_UPLOAD",saved,"Thành công");return saved;
    });
    /// <summary>
    /// Đọc metadata ảnh đang sử dụng sau khi kiểm tra lần khám và phạm vi dịch vụ của kỹ thuật viên.
    /// </summary>
    public ET_ANHXQUANGLANKHAM LayAnh(string maLS,string maAnh,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var(c,a)=Context(db,tx,maLS,user,false);DAL_QUYENNGHIEPVU.KiemTraMa(maAnh);
        var row=db.QuerySingleOrDefault<ET_ANHXQUANGLANKHAM>("SELECT * FROM dbo.ANH_XQUANG_LAN_KHAM WITH(HOLDLOCK) WHERE MaLS=@maLS AND MaAnh=@maAnh",new{maLS,maAnh},tx)??throw DAL_QUYENNGHIEPVU.TuChoi();
        if(Technician(a)&&!Services(db,tx,c,a).Any(s=>s.MaDVThucHien==row.MaDVThucHien))throw DAL_QUYENNGHIEPVU.TuChoi();
        if(row.TrangThai!="Đang sử dụng")throw new LoiAnhXquangException("AnhDaAn","Ảnh đã được ẩn; metadata vẫn được giữ trong lịch sử.");
        return row;
    });
    /// <summary>
    /// Chuyển ảnh sang Đã ẩn khi lượt còn mở và người dùng là quản trị, bác sĩ phụ trách hoặc người tải có quyền.
    /// </summary>
    /// <remarks>Giữ bản ghi và tệp; cập nhật trạng thái và nhật ký trong cùng giao dịch.</remarks>
    public bool AnAnh(string maLS,string maAnh,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var(c,a)=Context(db,tx,maLS,user,true);
        if(!Editable(c))throw new LoiAnhXquangException("LuotDaKhoa","Lịch sử khám chỉ xem; không ẩn ảnh sau khi hoàn tất lượt.");
        var row=new DAL_ANHXQUANG(tx).LayAnh(maLS,maAnh,a);
        if(!a.LaAdmin&&!Owner(c,a)&&row.MaNVUpload!=a.MaNV)throw DAL_QUYENNGHIEPVU.TuChoi();
        db.Execute("UPDATE dbo.ANH_XQUANG_LAN_KHAM SET TrangThai=N'Đã ẩn',NgayAn=SYSDATETIME() WHERE MaAnh=@maAnh",new{maAnh},tx);
        Audit(db,tx,a,"XR_HIDE",row,"Thành công");return true;
    });
    /// <summary>
    /// Ghi các sự kiện mở ảnh, lỗi tệp hoặc tải trùng được cho phép sau khi xác thực lại phạm vi lần khám.
    /// </summary>
    public void GhiSuKien(ET_ANHXQUANGLANKHAM image,string action,string result,NguCanhNghiepVu user)
    {
        if(action is not("XR_VIEW" or "XR_FILE_MISSING" or "XR_FILE_INVALID" or "XR_UPLOAD_DUPLICATE"))throw new ArgumentException("Sự kiện ảnh không hợp lệ.");
        ThucHien((db,tx)=>{var(_,a)=Context(db,tx,image.MaLS,user,false);Audit(db,tx,a,action,image,result);return true;});
    }
    /// <summary>
    /// Đọc toàn bộ metadata dưới quyền quản trị và khóa dùng chung của kho để phục vụ đối chiếu.
    /// </summary>
    public ET_ANHXQUANGLANKHAM[] LayToanBoMetadata(NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var a=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);if(!a.LaAdmin)throw DAL_QUYENNGHIEPVU.TuChoi();
        LockStorage(db,tx);return db.Query<ET_ANHXQUANGLANKHAM>("SELECT * FROM dbo.ANH_XQUANG_LAN_KHAM WITH(HOLDLOCK) ORDER BY MaAnh",transaction:tx).ToArray();
    });
    /// <summary>
    /// Yêu cầu quyền quản trị và khóa ứng dụng độc quyền theo giao dịch trước khi kiểm tra kho.
    /// </summary>
    /// <remarks>Dùng cùng tài nguyên khóa với thao tác ảnh thông thường để tránh đối chiếu giữa lúc ảnh đang được lưu.</remarks>
    public void KhoaKhoDocQuyen(NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        if(!DAL_QUYENNGHIEPVU.XacThuc(db,tx,user).LaAdmin)throw DAL_QUYENNGHIEPVU.TuChoi();
        var code=db.ExecuteScalar<int>("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'RHM:Xray:Storage',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; SELECT @r",transaction:tx);
        if(code<0)throw new LoiAnhXquangException("KhoDangBan","Kho ảnh đang có thao tác. Thử kiểm lại khi tải ảnh hoàn tất.");return true;
    });
    /// <summary>
    /// Ghi sự kiện ảnh bằng mã liên kết và trạng thái; không đưa nội dung ảnh hoặc thông tin bệnh án vào payload.
    /// </summary>
    private static void Audit(IDbConnection db,IDbTransaction? tx,NguCanhNghiepVu a,string action,ET_ANHXQUANGLANKHAM row,string result)
        =>db.Execute("""
            INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,ThoiGianUtc,MaPhien,MaSuKien,PhanHe,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuSau)
            VALUES(@id,@MaTK,SYSDATETIME(),SYSUTCDATETIME(),TRY_CONVERT(uniqueidentifier,SESSION_CONTEXT(N'RHM.MaPhien')),NEWID(),N'Ảnh X-quang',@action,N'ANH_XQUANG_LAN_KHAM',@MaAnh,@result,@detail)
            """,new{id="NK"+Guid.NewGuid().ToString("N"),a.MaTK,action,row.MaAnh,result,detail=JsonSerializer.Serialize(new{row.MaAnh,row.MaLS,row.MaDVThucHien,TrangThai=action=="XR_HIDE"?"Đã ẩn":row.TrangThai})},tx);

    /// <summary>
    /// Giữ khóa ứng dụng dùng chung tới cuối giao dịch; chờ thao tác kiểm kho hoặc bảo trì đang giữ khóa độc quyền.
    /// </summary>
    private static void LockStorage(IDbConnection db,IDbTransaction? tx)
    {
        var result=db.ExecuteScalar<int>("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'RHM:Xray:Storage',@LockMode='Shared',@LockOwner='Transaction',@LockTimeout=15000; SELECT @r",transaction:tx);
        if(result<0)throw new LoiAnhXquangException("KhoDangSaoLuu","Kho ảnh đang được sao lưu hoặc bảo trì. Vui lòng thử lại sau.");
    }
    /// <summary>
    /// Khóa dữ liệu lần khám và xác thực quyền xem hoặc ghi theo bác sĩ phụ trách, kỹ thuật viên được giao và vai trò.
    /// </summary>
    /// <param name="db">Kết nối SQL đang thực hiện thao tác ảnh.</param>
    /// <param name="tx">Giao dịch giữ khóa kho và dữ liệu lần khám.</param>
    /// <param name="maLS">Mã lần khám cần xác thực phạm vi.</param>
    /// <param name="user">Định danh người dùng để DAL xác thực lại.</param>
    /// <param name="write">true để giữ khóa cập nhật và yêu cầu phạm vi ghi ảnh.</param>
    /// <returns>Thông tin liên kết lần khám và ngữ cảnh người dùng đã xác thực.</returns>
    private static (VisitImage,NguCanhNghiepVu) Context(IDbConnection db,IDbTransaction? tx,string maLS,NguCanhNghiepVu user,bool write)
    {
        DAL_QUYENNGHIEPVU.KiemTraMa(maLS);LockStorage(db,tx);var a=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);
        var hint=write?"UPDLOCK,HOLDLOCK":"HOLDLOCK";
        var c=db.QuerySingleOrDefault<VisitImage>($"""
            SELECT l.MaLS,l.MaBS,p.MaBS BacSiPhieu,p.MaHS,h.MaBN,b.HoTenBN,l.TrangThai,p.TrangThai TrangThaiPhieu
            FROM dbo.LICH_SU_KHAM_BENH l WITH({hint}) JOIN dbo.PHIEU_KHAM_BENH p WITH({hint}) ON p.MaPhieu=l.MaPhieu
            JOIN dbo.HO_SO_BENH_AN h ON h.MaHS=p.MaHS JOIN dbo.BENH_NHAN b ON b.MaBN=h.MaBN WHERE l.MaLS=@maLS
            """,new{maLS},tx)??throw DAL_QUYENNGHIEPVU.TuChoi();
        var assigned=Technician(a)&&Services(db,tx,c,a).Count>0;
        var priorDoctor=a.CoQuyen(QuyenLamViec.Kham)&&c.TrangThai=="Đã xác nhận"&&db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.PHIEU_KHAM_BENH WITH(HOLDLOCK) WHERE MaHS=@MaHS AND MaBS=@MaNV AND TrangThai<>N'Hủy'",new{c.MaHS,a.MaNV},tx)>0;
        var reception=!Technician(a)&&a.CoQuyen(QuyenLamViec.TiepNhan)&&c.TrangThai=="Đã xác nhận";
        if(!a.LaAdmin&&(!a.CoQuyen(QuyenLamViec.XemHoSo)||!(Owner(c,a)||priorDoctor||reception||assigned)))throw DAL_QUYENNGHIEPVU.TuChoi();
        if(write&&!a.LaAdmin&&!Owner(c,a)&&!assigned)throw DAL_QUYENNGHIEPVU.TuChoi();
        return(c,a);
    }
    /// <summary>
    /// Đọc các dịch vụ X-quang của lượt, giới hạn theo phân công khi người dùng không phải quản trị hoặc bác sĩ phụ trách.
    /// </summary>
    private static IReadOnlyList<DichVuAnhXquang> Services(IDbConnection db,IDbTransaction? tx,VisitImage c,NguCanhNghiepVu a)
        =>db.Query<DichVuAnhXquang>($"""
            SELECT s.MaDVThucHien,d.TenDV,s.MaNVThucHien FROM dbo.DICH_VU_THUC_HIEN s WITH(HOLDLOCK)
            JOIN dbo.DICH_VU d ON d.MaDV=s.MaDV WHERE s.MaLS=@MaLS AND d.MaCK=N'CK004'
             AND(@all=1 OR {AssignedScope}) ORDER BY s.ThoiGianThucHien,s.MaDVThucHien
            """,new{c.MaLS,all=a.LaAdmin||Owner(c,a),a.MaNV},tx).ToArray();
    /// <summary>
    /// Nhận diện vai trò cập nhật kết quả được giao khi người dùng không có quyền khám và không phải quản trị.
    /// </summary>
    private static bool Technician(NguCanhNghiepVu a)=>!a.LaAdmin&&!a.CoQuyen(QuyenLamViec.Kham)&&a.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao);
    /// <summary>
    /// Chỉ nhận bác sĩ phụ trách khi mã bác sĩ trên lần khám và phiếu tiếp nhận cùng khớp người dùng có quyền khám.
    /// </summary>
    private static bool Owner(VisitImage c,NguCanhNghiepVu a)=>a.CoQuyen(QuyenLamViec.Kham)&&c.MaBS==a.MaNV&&c.BacSiPhieu==a.MaNV;
    /// <summary>
    /// Cho phép sửa ảnh khi lần khám còn Nháp và phiếu vẫn Chờ khám hoặc Đang khám.
    /// </summary>
    private static bool Editable(VisitImage c)=>c.TrangThai=="Nháp"&&c.TrangThaiPhieu is "Đang khám" or "Chờ khám";
    /// <summary>
    /// Dữ liệu liên kết lần khám, phiếu tiếp nhận và bệnh nhân dùng xác định phạm vi, trạng thái thao tác ảnh.
    /// </summary>
    private sealed class VisitImage
    {
        public string MaLS {get;set;}="";public string MaBS {get;set;}="";public string BacSiPhieu {get;set;}="";
        public string MaHS {get;set;}="";public string MaBN {get;set;}="";public string HoTenBN {get;set;}="";
        public string TrangThai {get;set;}="";public string TrangThaiPhieu {get;set;}="";
    }
}
