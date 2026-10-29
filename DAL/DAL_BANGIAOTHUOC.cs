using System.Data;
using Dapper;
using System.Text.Json;
using ET;
namespace DAL;

/// <summary>
/// Tầng truy cập dữ liệu bàn giao thuốc, thực hiện truy vấn và kiểm tra trong phạm vi kết nối/giao dịch của dbconnect.
/// </summary>
public sealed class DAL_BANGIAOTHUOC : dbconnect
{
    /// <summary>
    /// Giao dịch được giữ để các thao tác liên quan dùng cùng một phạm vi commit/rollback.
    /// </summary>
    private readonly IDbTransaction? callerTransaction;
    /// <summary>
    /// Khởi tạo bàn giao thuốc. Chuyển giao dịch tùy chọn cho lớp cơ sở; nơi cấp giao dịch chịu trách nhiệm kết thúc giao dịch đó.
    /// </summary>
    /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    public DAL_BANGIAOTHUOC(IDbTransaction? transaction=null):base(transaction)=>callerTransaction=transaction;
    /// <summary>
    /// Đọc thông tin bàn giao tương ứng với đơn thuốc được chỉ định.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>db.QuerySingleOrDefault&lt;ET_BANGIAOTHUOC&gt;</c>.</remarks>
    /// <param name="maDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Đối tượng kiểu ET_BANGIAOTHUOC được tìm hoặc tính toán; có thể null theo nhánh xử lý trong hàm.</returns>
    public ET_BANGIAOTHUOC? LayTheoDon(string maDT,NguCanhNghiepVu nguoiDung) => ThucHien((db,tx) =>
    {
        new DAL_KHAMDIEUTRI(tx).LayDonThuoc(maDT,nguoiDung);
        return db.QuerySingleOrDefault<ET_BANGIAOTHUOC>("SELECT * FROM dbo.BAN_GIAO_THUOC WHERE MaDT=@maDT",new{maDT},tx);
    });
    /// <summary>
    /// Đọc danh sách hàng đợi theo phạm vi dữ liệu và quyền của người dùng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>RequirePharmacy</c>, <c>Error</c>, <c>filter.TuKhoa.Trim</c>.</remarks>
    /// <param name="filter">Giá trị bộ lọc dùng khi thực hiện thao tác; kiểu BoLocBanGiao.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu HangDoiBanGiao dùng cho bước xử lý tiếp theo.</returns>
    public HangDoiBanGiao LayHangDoi(BoLocBanGiao filter,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        RequirePharmacy(db,tx,user);
        if(filter is null || filter.Trang<1 || filter.Trang>1000000 || filter.KichThuocTrang is <1 or >100 ||
            filter.TrangThai is not("Chờ giao" or "Đã giao" or "Tất cả") || filter.TuKhoa?.Length>100 || filter.MaBN?.Length>50 || filter.MaLS?.Length>50)
            throw Error("BoLoc","Bộ lọc không hợp lệ; mỗi trang tối đa 100 đơn.");
        var term=string.IsNullOrWhiteSpace(filter.TuKhoa)?null:"%"+filter.TuKhoa.Trim().Replace("[","[[]").Replace("%","[%]").Replace("_","[_]")+"%";
        var where=Eligible+" AND(@MaBN IS NULL OR bn.MaBN=@MaBN) AND(@MaLS IS NULL OR d.MaLS=@MaLS) AND(@TrangThai=N'Tất cả' OR b.TrangThai=@TrangThai) AND(@term IS NULL OR bn.HoTenBN COLLATE Latin1_General_CI_AI LIKE @term OR bn.MaBN LIKE @term OR d.MaDT LIKE @term OR b.MaHD LIKE @term)";
        var args=new{filter.MaBN,filter.MaLS,filter.TrangThai,term,offset=(long)(filter.Trang-1)*filter.KichThuocTrang,take=filter.KichThuocTrang};
        var total=db.ExecuteScalar<int>("SELECT COUNT(*) "+Joins+" WHERE "+where,args,tx);
        var rows=db.Query<HangBanGiao>(Columns+Joins+" WHERE "+where+" ORDER BY h.NgayLap,d.MaDT OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY",args,tx).ToArray();
        return new HangDoiBanGiao(rows,total,filter.Trang,filter.KichThuocTrang);
    });
    /// <summary>
    /// Đọc dữ liệu chi tiết của mục được chọn để hiển thị hoặc tiếp tục xử lý.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>RequirePharmacy</c>, <c>DAL_QUYENNGHIEPVU.KiemTraMa</c>, <c>db.ExecuteScalar&lt;string?&gt;</c>.</remarks>
    /// <param name="maDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu ChiTietBanGiao dùng cho bước xử lý tiếp theo.</returns>
    public ChiTietBanGiao LayChiTiet(string maDT,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var actor=RequirePharmacy(db,tx,user);DAL_QUYENNGHIEPVU.KiemTraMa(maDT);
        // Đọc định tuyến ngắn trước khi khóa; không giữ S trên nguồn rồi chờ canonical lock.
        var visit=db.ExecuteScalar<string?>("SELECT MaLS FROM dbo.DON_THUOC WITH(READCOMMITTEDLOCK) WHERE MaDT=@maDT",new{maDT},tx);
        if(visit is null)throw Error("MaDT","Không có đơn đủ điều kiện bàn giao.");
        Lock(db,tx,"RHM:Kham:LanKham:"+visit);
        var row=ReadValid(db,tx,maDT);
        if(row.MaLS!=visit)throw Error("MaDT","Nguồn đơn đã đổi; tải lại trước khi xem/giao.");
        return new ChiTietBanGiao(row,new DAL_DONTHUOC(tx).LayBanInDon(maDT,actor));
    });
    /// <summary>
    /// Kiểm tra chứng từ và nội dung đối chiếu trước khi xác nhận bàn giao thuốc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Error</c>, <c>ThucHien</c>, <c>RequirePharmacy</c>, <c>DAL_QUYENNGHIEPVU.KiemTraMa</c>.</remarks>
    /// <param name="maDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu KetQuaBanGiao dùng cho bước xử lý tiếp theo.</returns>
    public KetQuaBanGiao XacNhanGiao(string maDT,NguCanhNghiepVu user)
    {
        if(callerTransaction is null || callerTransaction.IsolationLevel!=IsolationLevel.Serializable)throw Error("BanGiao","Cần giao dịch Serializable của đầu mối bàn giao.");
        return ThucHien((db,tx)=>
        {
            var actor=RequirePharmacy(db,tx,user);DAL_QUYENNGHIEPVU.KiemTraMa(maDT);
            Lock(db,tx,"RHM:ThanhToan:Write");
            var visit=db.ExecuteScalar<string?>("SELECT MaLS FROM dbo.DON_THUOC WHERE MaDT=@maDT",new{maDT},tx);
            if(visit is null)throw Error("MaDT","Không có đơn đủ điều kiện bàn giao.");
            Lock(db,tx,"RHM:Kham:LanKham:"+visit);
            var row=ReadValid(db,tx,maDT);
            if(row.TrangThai=="Đã giao")return Result(row,true,AllGiven(db,tx,row.MaLS,row.MaHD));
            // Kiểm tra nội dung bản ký trước giao; không sửa đơn, giá, thu hay nguồn kho.
            new DAL_DONTHUOC(tx).LayBanInDon(maDT,actor);
            var changed=db.Execute("UPDATE dbo.BAN_GIAO_THUOC SET TrangThai=N'Đã giao',MaNVGiao=@MaNV,ThoiGianGiao=CONVERT(datetime2(0),SYSDATETIME()) WHERE MaDT=@maDT AND TrangThai=N'Chờ giao'",new{maDT,actor.MaNV},tx);
            if(changed!=1)throw Error("MaDT","Trạng thái bàn giao đã đổi; tải lại đơn.");
            row=ReadValid(db,tx,maDT);var complete=AllGiven(db,tx,row.MaLS,row.MaHD);
            if(complete)db.Execute("UPDATE p SET TrangThai=N'Hoàn tất' FROM dbo.PHIEU_KHAM_BENH p JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaPhieu=p.MaPhieu WHERE l.MaLS=@MaLS",row,tx);
            var result=Result(row,false,complete);var audit=TaoMa(db,tx,"NHAT_KY_THAO_TAC","MaNK");
            db.Execute("INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,DuLieuSau,KetQua) VALUES(@audit,@MaTK,SYSDATETIME(),N'Bàn giao thuốc',N'DON_THUOC',@maDT,@data,N'Thành công')",new{audit,actor.MaTK,maDT,data=JsonSerializer.Serialize(result)},tx);
            return result;
        });
    }
    /// <summary>
    /// Xác thực quyền bàn giao thuốc của người thực hiện.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DAL_QUYENNGHIEPVU.XacThuc</c>, <c>actor.CoQuyen</c>, <c>DAL_QUYENNGHIEPVU.TuChoi</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu NguCanhNghiepVu dùng cho bước xử lý tiếp theo.</returns>
    private static NguCanhNghiepVu RequirePharmacy(IDbConnection db,IDbTransaction? tx,NguCanhNghiepVu user)
    {var actor=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);if(!actor.CoQuyen(QuyenLamViec.GiaoThuoc))throw DAL_QUYENNGHIEPVU.TuChoi();return actor;}
    /// <summary>
    /// Đọc và kiểm tra bản dữ liệu bàn giao đủ điều kiện xử lý.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>db.QuerySingleOrDefault&lt;HangBanGiao&gt;</c>, <c>Error</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả kiểu HangBanGiao dùng cho bước xử lý tiếp theo.</returns>
    private static HangBanGiao ReadValid(IDbConnection db,IDbTransaction? tx,string id)
        =>db.QuerySingleOrDefault<HangBanGiao>(Columns+Joins+" WHERE b.MaDT=@id AND "+Eligible,new{id},tx)??throw Error("MaDT","Đơn chưa có bàn giao hợp lệ: cần bản ký, hóa đơn thu đủ và xuất đúng nguồn. Không xuất lại thuốc.");
    /// <summary>
    /// Kiểm tra toàn bộ đơn liên quan đã được bàn giao hay chưa.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>db.ExecuteScalar&lt;bool&gt;</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="maHD">Mã hóa đơn xác định đối tượng cần xử lý.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    private static bool AllGiven(IDbConnection db,IDbTransaction? tx,string maLS,string maHD)=>db.ExecuteScalar<bool>("SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.DON_THUOC WHERE MaLS=@maLS AND TrangThai=N'Đã xác nhận') AND NOT EXISTS(SELECT 1 FROM dbo.DON_THUOC d WHERE d.MaLS=@maLS AND d.TrangThai=N'Đã xác nhận' AND NOT EXISTS(SELECT 1 FROM dbo.BAN_GIAO_THUOC b WHERE b.MaDT=d.MaDT AND b.MaHD=@maHD AND b.TrangThai=N'Đã giao')) THEN 1 ELSE 0 END AS bit)",new{maLS,maHD},tx);
    /// <summary>
    /// Tạo kết quả trả về từ dữ liệu nghiệp vụ đã đọc hoặc xử lý.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Error</c>.</remarks>
    /// <param name="r">Giá trị r dùng khi thực hiện thao tác; kiểu HangBanGiao.</param>
    /// <param name="existing">Giá trị đã tồn tại dùng khi thực hiện thao tác; kiểu bool.</param>
    /// <param name="complete">Giá trị hoàn tất dùng khi thực hiện thao tác; kiểu bool.</param>
    /// <returns>Kết quả kiểu KetQuaBanGiao dùng cho bước xử lý tiếp theo.</returns>
    private static KetQuaBanGiao Result(HangBanGiao r,bool existing,bool complete)=>new(r.MaDT,r.MaLS,r.MaHD,r.MaPX,r.MaNVGiao??throw Error("MaDT","Thiếu người giao."),r.ThoiGianGiao??throw Error("MaDT","Thiếu giờ giao."),existing,complete);
    /// <summary>
    /// Tạo ngoại lệ nghiệp vụ với thông tin trường và thông báo được truyền vào.
    /// </summary>
    /// <param name="field">Giá trị trường dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="message">Nội dung thông báo được chuyển đến nơi gọi hoặc hiển thị cho người dùng.</param>
    /// <returns>Kết quả kiểu LoiKhamException dùng cho bước xử lý tiếp theo.</returns>
    private static LoiKhamException Error(string field,string message)=>new(field,message);
    /// <summary>
    /// Lấy khóa cho nguồn dữ liệu nghiệp vụ trong giao dịch hiện tại. Dùng application lock sở hữu bởi giao dịch; từ chối khi không lấy được khóa trong thời gian chờ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>db.ExecuteScalar&lt;int&gt;</c>, <c>Error</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="key">Giá trị khóa dùng khi thực hiện thao tác; kiểu string.</param>
    private static void Lock(IDbConnection db,IDbTransaction? tx,string key)
    {if(db.ExecuteScalar<int>("DECLARE @r int;EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;SELECT @r",new{key},tx)<0)throw Error("BanGiao","Đơn đang xử lý; thử lại đúng mã đơn.");}
    /// <summary>
    /// Các cột được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private const string Columns="SELECT d.MaDT,d.MaLS,bn.MaBN,bn.HoTenBN,bn.NgaySinh,d.NgayKe,b.MaHD,b.MaPX,b.TrangThai,b.MaNVGiao,n.HoTen TenNguoiGiao,b.ThoiGianGiao ";
    /// <summary>
    /// Liên kết được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private const string Joins="""
        FROM dbo.BAN_GIAO_THUOC b WITH(HOLDLOCK) JOIN dbo.DON_THUOC d WITH(HOLDLOCK) ON d.MaDT=b.MaDT
        JOIN dbo.HOA_DON h WITH(HOLDLOCK) ON h.MaHD=b.MaHD JOIN dbo.PHIEU_XUAT x WITH(HOLDLOCK) ON x.MaPX=b.MaPX
        JOIN dbo.LICH_SU_KHAM_BENH l WITH(HOLDLOCK) ON l.MaLS=d.MaLS
        JOIN dbo.PHIEU_KHAM_BENH p WITH(HOLDLOCK) ON p.MaPhieu=l.MaPhieu
        JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS JOIN dbo.BENH_NHAN bn ON bn.MaBN=hs.MaBN
        LEFT JOIN dbo.NHAN_VIEN n ON n.MaNV=b.MaNVGiao
        """;
    /// <summary>
    /// Đủ điều kiện được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private const string Eligible="""
        h.PhienBanQuyTrinh=2 AND h.TrangThai=N'Đã xác nhận' AND d.TrangThai=N'Đã xác nhận' AND l.TrangThai=N'Đã xác nhận'
        AND d.MaLS=h.MaLS AND x.MaLS=h.MaLS AND x.TrangThai=N'Đã ghi sổ'
        AND(h.SoTienPhaiThu=0 AND NOT EXISTS(SELECT 1 FROM dbo.THANH_TOAN t WITH(HOLDLOCK) WHERE t.MaHD=h.MaHD)
            OR h.SoTienPhaiThu>0 AND(SELECT COUNT(*) FROM dbo.THANH_TOAN t WITH(HOLDLOCK) WHERE t.MaHD=h.MaHD)=1
            AND EXISTS(SELECT 1 FROM dbo.THANH_TOAN t WITH(HOLDLOCK) WHERE t.MaHD=h.MaHD AND t.TrangThai=N'Đã xác nhận' AND t.LoaiGiaoDich=N'Thu' AND t.SoTien=h.SoTienPhaiThu))
        AND EXISTS(SELECT 1 FROM dbo.CHI_TIET_DON_THUOC c WITH(HOLDLOCK) WHERE c.MaDT=d.MaDT)
        AND EXISTS(SELECT 1 FROM dbo.CHI_TIET_PHIEU_XUAT c WITH(HOLDLOCK) WHERE c.MaPX=b.MaPX)
        AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_PHIEU_XUAT z WITH(HOLDLOCK) LEFT JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaCTDT=z.MaCTDT
            WHERE z.MaPX=b.MaPX AND(c.MaDT IS NULL OR c.MaDT<>d.MaDT OR NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON i WITH(HOLDLOCK) WHERE i.MaHD=h.MaHD AND i.MaCTPX=z.MaCTPX)))
        AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_DON_THUOC c WITH(HOLDLOCK) WHERE c.MaDT=d.MaDT AND c.SoLuong<>
            COALESCE((SELECT SUM(z.SoLuong) FROM dbo.CHI_TIET_PHIEU_XUAT z WITH(HOLDLOCK) WHERE z.MaPX=b.MaPX AND z.MaCTDT=c.MaCTDT),0))
        """;
}
