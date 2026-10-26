using ET;
using Dapper;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace DAL;
public partial class DAL_DICHVUTHUCHIEN
{
    private const string VisitSql="""
        SELECT l.MaLS,h.MaBN,b.HoTenBN,p.MaHS,l.MaBS,l.ThoiGianKham,l.TrangThai,
          p.MaBS BacSiPhieu,p.TrangThai TrangThaiPhieu
        FROM dbo.LICH_SU_KHAM_BENH l WITH(HOLDLOCK)
        JOIN dbo.PHIEU_KHAM_BENH p WITH(HOLDLOCK) ON p.MaPhieu=l.MaPhieu
        JOIN dbo.HO_SO_BENH_AN h WITH(HOLDLOCK) ON h.MaHS=p.MaHS
        JOIN dbo.BENH_NHAN b ON b.MaBN=h.MaBN
        """;
    private const string ReadScope="""
        ((@Doctor=1 AND (l.MaBS=@MaNV AND p.MaBS=@MaNV OR l.TrangThai=N'Đã xác nhận' AND EXISTS(
           SELECT 1 FROM dbo.PHIEU_KHAM_BENH ownp JOIN dbo.HO_SO_BENH_AN ownh ON ownh.MaHS=ownp.MaHS WHERE ownp.MaBS=@MaNV AND ownh.MaBN=h.MaBN)))
         OR (@Nurse=1 AND EXISTS(SELECT 1 FROM dbo.DICH_VU_THUC_HIEN a WHERE a.MaLS=l.MaLS AND a.MaNVThucHien=@MaNV))
         OR (@Cash=1 AND l.TrangThai=N'Đã xác nhận'))
        """;
    private static object Scope(NguCanhNghiepVu a,string? maLS=null,string? maBN=null)=>new{a.MaNV,maLS,maBN,Doctor=a.CoQuyen(QuyenLamViec.Kham),Nurse=a.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao),Cash=a.LaAdmin||a.CoQuyen(QuyenLamViec.ThuTien)};
    public IReadOnlyList<LuotThucHien> LayLuotThucHien(NguCanhNghiepVu user,string? maBN=null)=>ThucHien((db,tx)=>
    {
        if(maBN is not null)DAL_QUYENNGHIEPVU.KiemTraMa(maBN);
        var a=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);
        var sql="SELECT TOP(100) MaLS,MaBN,HoTenBN,MaHS,MaBS,ThoiGianKham,TrangThai FROM ("+VisitSql+" WHERE "+ReadScope+" AND (@maBN IS NULL OR h.MaBN=@maBN)) permitted ORDER BY ThoiGianKham DESC,MaLS";
        return (IReadOnlyList<LuotThucHien>)db.Query<LuotThucHien>(sql,Scope(a,maBN:maBN),tx).ToArray();
    });
    public KhongGianThucHien LayKhongGianThucHien(string maLS,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var (a,v)=ReadVisit(db,tx,maLS,user);var own=Own(a,v);var canCreate=own&&Draft(v);
        var rows=db.Query<ET_DICHVUTHUCHIEN>("SELECT * FROM dbo.DICH_VU_THUC_HIEN WITH(HOLDLOCK) WHERE MaLS=@maLS AND (@own=1 OR MaNVThucHien=@MaNV AND @nurse=1 OR TrangThai=N'Đã xác nhận' AND @nurse=0) ORDER BY ThoiGianThucHien,MaDVThucHien",new{maLS,own,a.MaNV,nurse=a.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao)&&!own},tx).Select(r=>Snapshot(db,tx,r,a,v)).ToArray();
        var steps=own?db.Query<BuocDuKienThucHien>("""
            SELECT c.MaCTKH,c.MaKHDT,k.MaLS MaLSSource,c.MaDV,d.TenDV,c.LanThu,c.NoiDungBuoc NoiDung,c.SoLuongDuKien,
              COALESCE(q.Done,0) DaXacNhan,COALESCE(q.Pending,0) DangNhap,
              c.SoLuongDuKien-COALESCE(q.Done,0)-COALESCE(q.Pending,0) ConLai,c.DonGiaDuKien
            FROM dbo.CHI_TIET_KE_HOACH c WITH(HOLDLOCK) JOIN dbo.KE_HOACH_DIEU_TRI k WITH(HOLDLOCK) ON k.MaKHDT=c.MaKHDT
            JOIN dbo.LICH_SU_KHAM_BENH l0 ON l0.MaLS=k.MaLS JOIN dbo.PHIEU_KHAM_BENH p0 ON p0.MaPhieu=l0.MaPhieu
            JOIN dbo.DICH_VU d ON d.MaDV=c.MaDV
            OUTER APPLY(SELECT SUM(CASE WHEN s.TrangThai=N'Đã xác nhận' THEN s.SoLuong ELSE 0 END) Done,
              SUM(CASE WHEN s.TrangThai=N'Nháp' THEN s.SoLuong ELSE 0 END) Pending FROM dbo.DICH_VU_THUC_HIEN s WITH(HOLDLOCK) WHERE s.MaCTKH=c.MaCTKH) q
            WHERE p0.MaHS=@MaHS AND k.TrangThai IN(N'Đã xác nhận',N'Đang thực hiện',N'Đã hoàn thành') AND c.TrangThai<>N'Hủy'
            ORDER BY k.NgayLap,c.LanThu,c.MaCTKH
            """,v,tx).ToArray():Array.Empty<BuocDuKienThucHien>();
        var catalog=own?db.Query<ET_DICHVU>("SELECT * FROM dbo.DICH_VU WHERE TrangThai=N'Đang hoạt động' ORDER BY TenDV,MaDV",transaction:tx).ToArray():Array.Empty<ET_DICHVU>();
        var people=canCreate?db.Query<NguoiThucHienDichVu>("SELECT n.MaNV,n.HoTen FROM dbo.NHAN_VIEN n WHERE n.TrangThai=N'Đang làm việc' AND n.MaChucVu IN(N'CV002',N'CV003') AND EXISTS(SELECT 1 FROM dbo.TAI_KHOAN t WHERE t.MaNV=n.MaNV AND t.TrangThai=N'Đang hoạt động') ORDER BY n.HoTen,n.MaNV",transaction:tx).ToArray():Array.Empty<NguoiThucHienDichVu>();
        if(a.LaAdmin)Audit(db,tx,a,"Tra cứu dịch vụ",maLS,null,null);
        return new KhongGianThucHien(new(v.MaLS,v.MaBN,v.HoTenBN,v.MaHS,v.MaBS,v.ThoiGianKham,v.TrangThai),canCreate,steps,rows,catalog,people);
    });
    public BanNhapThucHien LayThucHien(string id,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {var (a,v,r)=ReadActual(db,tx,id,user);var result=Snapshot(db,tx,r,a,v);if(a.LaAdmin)Audit(db,tx,a,"Tra cứu dịch vụ",id,null,null);return result;});

    public BanNhapThucHien LuuThucHien(YeuCauThucHienDichVu request,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        ArgumentNullException.ThrowIfNull(request);WriteLock(db,tx);
        var (a,v)=ReadVisit(db,tx,request.MaLS,user);
        ET_DICHVUTHUCHIEN? old=null;BanNhapThucHien? previous=null;
        if(request.MaDVThucHien is not null)
        {
            old=ReadRow(db,tx,request.MaDVThucHien);
            if(old.MaLS!=v.MaLS)throw DAL_QUYENNGHIEPVU.TuChoi();
            previous=Snapshot(db,tx,old,a,v);EnsureDraft(db,tx,old,v);CheckHash(request.DauVanTay,previous.DauVanTay);
            if(!previous.DuocSua)throw DAL_QUYENNGHIEPVU.TuChoi();
            if(request.MaCTKH!=old.MaCTKH||request.MaDV!=old.MaDV)throw Error("MaCTKH","Nguồn đã lưu được giữ nguyên; hủy nháp và lập dòng đúng nguồn.");
        }
        if(!Draft(v))throw DAL_QUYENNGHIEPVU.TuChoi();
        var own=Own(a,v);
        if(!own && (old is null||!a.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao)||old.MaNVThucHien!=a.MaNV))throw DAL_QUYENNGHIEPVU.TuChoi();
        var reason=request.MaCTKH is not null?null:string.IsNullOrWhiteSpace(request.LyDoPhatSinh)?previous?.LyDoPhatSinh:request.LyDoPhatSinh.Trim();
        if(!own && (request.MaNVThucHien!=old!.MaNVThucHien||request.SoLuong!=old.SoLuong||request.DonGia!=old.DonGia||reason!=previous!.LyDoPhatSinh))throw DAL_QUYENNGHIEPVU.TuChoi();
        var row=new ET_DICHVUTHUCHIEN {MaDVThucHien=old?.MaDVThucHien??"",MaLS=v.MaLS,MaCTKH=request.MaCTKH,MaDV=request.MaDV,MaNVThucHien=request.MaNVThucHien,ThoiGianThucHien=request.ThoiGianThucHien,SoLuong=request.SoLuong,DonGia=request.DonGia,KetQua=request.KetQua,TrangThai="Nháp"};
        Validate(db,tx,row,v,reason);KiemTra(db,tx,"DICH_VU_THUC_HIEN",row,old,null);
        if(old is null)
        {
            row.MaDVThucHien=TaoMa(db,tx,"DICH_VU_THUC_HIEN","MaDVThucHien");
            db.Execute("INSERT dbo.DICH_VU_THUC_HIEN(MaDVThucHien,MaLS,MaCTKH,MaDV,MaNVThucHien,ThoiGianThucHien,SoLuong,DonGia,KetQua,TrangThai) VALUES(@MaDVThucHien,@MaLS,@MaCTKH,@MaDV,@MaNVThucHien,@ThoiGianThucHien,@SoLuong,@DonGia,@KetQua,@TrangThai)",row,tx);
        }
        else db.Execute("UPDATE dbo.DICH_VU_THUC_HIEN SET MaNVThucHien=@MaNVThucHien,ThoiGianThucHien=@ThoiGianThucHien,SoLuong=@SoLuong,DonGia=@DonGia,KetQua=@KetQua WHERE MaDVThucHien=@MaDVThucHien",row,tx);
        Audit(db,tx,a,"Lưu dịch vụ thực hiện",row.MaDVThucHien,previous?.DauVanTay,JsonSerializer.Serialize(new{LyDoPhatSinh=reason}));
        return Snapshot(db,tx,ReadRow(db,tx,row.MaDVThucHien),a,v);
    });
    public BanNhapThucHien XacNhanThucHien(string id,string dauVanTay,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        WriteLock(db,tx);var(a,v,r)=ReadActual(db,tx,id,user);
        if(!Own(a,v))throw DAL_QUYENNGHIEPVU.TuChoi();
        var before=Snapshot(db,tx,r,a,v);if(r.TrangThai=="Đã xác nhận")return before;
        EnsureDraft(db,tx,r,v);CheckHash(dauVanTay,before.DauVanTay);Validate(db,tx,r,v,before.LyDoPhatSinh);
        if(string.IsNullOrWhiteSpace(r.KetQua))throw Error("KetQua","Nhập kết quả thực hiện trước khi xác nhận.");
        db.Execute("UPDATE dbo.DICH_VU_THUC_HIEN SET TrangThai=N'Đã xác nhận' WHERE MaDVThucHien=@id",new{id},tx);
        Audit(db,tx,a,"Xác nhận dịch vụ",id,before.DauVanTay,null);
        return Snapshot(db,tx,ReadRow(db,tx,id),a,v);
    });
    public BanNhapThucHien HuyThucHien(string id,string dauVanTay,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        WriteLock(db,tx);var(a,v,r)=ReadActual(db,tx,id,user);if(!Own(a,v))throw DAL_QUYENNGHIEPVU.TuChoi();
        var before=Snapshot(db,tx,r,a,v);EnsureDraft(db,tx,r,v);CheckHash(dauVanTay,before.DauVanTay);
        db.Execute("UPDATE dbo.DICH_VU_THUC_HIEN SET TrangThai=N'Hủy' WHERE MaDVThucHien=@id",new{id},tx);Audit(db,tx,a,"Hủy dịch vụ nháp",id,before.DauVanTay,null);
        return Snapshot(db,tx,ReadRow(db,tx,id),a,v);
    });
    private void Validate(IDbConnection db,IDbTransaction? tx,ET_DICHVUTHUCHIEN r,Visit v,string? reason)
    {
        if(r.SoLuong<=0||r.DonGia<0)throw Error("SoLuong","Lượng phải dương và giá không âm.");
        var now=db.ExecuteScalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())",transaction:tx);
        if(r.ThoiGianThucHien<v.ThoiGianKham||r.ThoiGianThucHien>now)throw Error("ThoiGianThucHien","Giờ thực hiện phải từ lúc bắt đầu lượt đến hiện tại.");
        if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DICH_VU WITH(HOLDLOCK) WHERE MaDV=@MaDV AND TrangThai=N'Đang hoạt động'",r,tx)!=1)throw Error("MaDV","Dịch vụ không hoạt động.");
        if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.NHAN_VIEN n WITH(HOLDLOCK) WHERE MaNV=@MaNVThucHien AND TrangThai=N'Đang làm việc' AND MaChucVu IN(N'CV002',N'CV003') AND EXISTS(SELECT 1 FROM dbo.TAI_KHOAN t WITH(HOLDLOCK) WHERE t.MaNV=n.MaNV AND t.TrangThai=N'Đang hoạt động')",r,tx)!=1)throw Error("MaNVThucHien","Chọn bác sĩ/điều dưỡng có tài khoản đang hoạt động.");
        if(r.MaCTKH is null)
        {if(string.IsNullOrWhiteSpace(reason)||reason.Length>1000)throw Error("LyDoPhatSinh","Dịch vụ ngoài kế hoạch cần lý do, tối đa 1000 ký tự.");return;}
        var source=db.QuerySingleOrDefault<Step>("""
            SELECT c.MaDV,c.SoLuongDuKien,c.TrangThai,k.TrangThai PlanStatus,p.MaHS
            FROM dbo.CHI_TIET_KE_HOACH c WITH(UPDLOCK,HOLDLOCK)
            JOIN dbo.KE_HOACH_DIEU_TRI k WITH(HOLDLOCK) ON k.MaKHDT=c.MaKHDT
            JOIN dbo.LICH_SU_KHAM_BENH l WITH(HOLDLOCK) ON l.MaLS=k.MaLS JOIN dbo.PHIEU_KHAM_BENH p WITH(HOLDLOCK) ON p.MaPhieu=l.MaPhieu
            WHERE c.MaCTKH=@MaCTKH
            """,r,tx);
        if(source is null||source.MaHS!=v.MaHS||source.MaDV!=r.MaDV||source.TrangThai=="Hủy"||source.PlanStatus is not("Đã xác nhận" or "Đang thực hiện" or "Đã hoàn thành"))throw Error("MaCTKH","Chọn bước đã duyệt, đúng dịch vụ và cùng hồ sơ bệnh án.");
        var used=db.ExecuteScalar<decimal>("SELECT COALESCE(SUM(SoLuong),0) FROM dbo.DICH_VU_THUC_HIEN WITH(UPDLOCK,HOLDLOCK) WHERE MaCTKH=@MaCTKH AND MaDVThucHien<>@MaDVThucHien AND TrangThai IN(N'Nháp',N'Đã xác nhận')",r,tx);
        if(used+r.SoLuong>source.SoLuongDuKien)throw Error("SoLuong","Lượng vượt phần còn lại của bước (gồm dòng nháp đang được giao).");
    }
    private static (NguCanhNghiepVu,Visit) ReadVisit(IDbConnection db,IDbTransaction? tx,string maLS,NguCanhNghiepVu user)
    {
        DAL_QUYENNGHIEPVU.KiemTraMa(maLS);Lock(db,tx,"RHM:Kham:LanKham:"+maLS);
        var a=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);var v=db.QuerySingleOrDefault<Visit>(VisitSql+" WHERE l.MaLS=@maLS AND "+ReadScope,Scope(a,maLS),tx)??throw DAL_QUYENNGHIEPVU.TuChoi();return(a,v);
    }
    private static (NguCanhNghiepVu,Visit,ET_DICHVUTHUCHIEN) ReadActual(IDbConnection db,IDbTransaction? tx,string id,NguCanhNghiepVu user)
    {
        DAL_QUYENNGHIEPVU.KiemTraMa(id);
        // Mã cha chỉ định tuyến; kiểm tra lại dòng, quyền và nguồn sau khóa lượt.
        var parent=db.QuerySingleOrDefault<string>("SELECT MaLS FROM dbo.DICH_VU_THUC_HIEN WITH(READUNCOMMITTED) WHERE MaDVThucHien=@id",new{id},tx)??throw DAL_QUYENNGHIEPVU.TuChoi();
        var(a,v)=ReadVisit(db,tx,parent,user);var r=ReadRow(db,tx,id);
        if(r.MaLS!=v.MaLS||a.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao)&&!Own(a,v)&&r.MaNVThucHien!=a.MaNV||!Own(a,v)&&!a.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao)&&r.TrangThai!="Đã xác nhận")throw DAL_QUYENNGHIEPVU.TuChoi();return(a,v,r);
    }
    private static ET_DICHVUTHUCHIEN ReadRow(IDbConnection db,IDbTransaction? tx,string id)=>db.QuerySingleOrDefault<ET_DICHVUTHUCHIEN>("SELECT * FROM dbo.DICH_VU_THUC_HIEN WITH(HOLDLOCK) WHERE MaDVThucHien=@id",new{id},tx)??throw DAL_QUYENNGHIEPVU.TuChoi();
    private static bool Own(NguCanhNghiepVu a,Visit v)=>a.CoQuyen(QuyenLamViec.Kham)&&v.MaBS==a.MaNV&&v.BacSiPhieu==a.MaNV;
    private static bool Draft(Visit v)=>v.TrangThai=="Nháp"&&v.TrangThaiPhieu is "Chờ khám" or "Đang khám";
    private static bool Billed(IDbConnection db,IDbTransaction? tx,string id)=>db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.CHI_TIET_HOA_DON c WITH(HOLDLOCK) JOIN dbo.HOA_DON h WITH(HOLDLOCK) ON h.MaHD=c.MaHD WHERE c.MaDVThucHien=@id AND h.TrangThai<>N'Hủy'",new{id},tx)>0;
    private static void EnsureDraft(IDbConnection db,IDbTransaction? tx,ET_DICHVUTHUCHIEN r,Visit v)
    {if(r.TrangThai!="Nháp"||!Draft(v)||Billed(db,tx,r.MaDVThucHien))throw Error("TrangThai","Chỉ sửa/xác nhận nháp của lượt đang khám, chưa tính phí.");}
    private static BanNhapThucHien Snapshot(IDbConnection db,IDbTransaction? tx,ET_DICHVUTHUCHIEN r,NguCanhNghiepVu a,Visit v)
    {
        // Hậu tố mã do TaoMa cấp tăng theo số; sắp chuỗi sẽ đảo thứ tự ở biên 99/100 hoặc 999/1000.
        var reason=db.QueryFirstOrDefault<string>("SELECT TOP(1) JSON_VALUE(CASE WHEN ISJSON(DuLieuSau)=1 THEN DuLieuSau ELSE N'{}' END,'$.LyDoPhatSinh') FROM dbo.NHAT_KY_THAO_TAC WITH(HOLDLOCK) WHERE LoaiDoiTuong=N'DICH_VU_THUC_HIEN' AND MaDoiTuong=@id AND HanhDong=N'Lưu dịch vụ thực hiện' ORDER BY TRY_CONVERT(bigint,SUBSTRING(MaNK,PATINDEX('%[0-9]%',MaNK+'0'),50)) DESC,ThoiGian DESC,MaNK DESC",new{id=r.MaDVThucHien},tx);
        var billed=Billed(db,tx,r.MaDVThucHien);var draft=r.TrangThai=="Nháp"&&Draft(v)&&!billed;var own=Own(a,v);
        if(!a.LaAdmin&&!a.CoQuyen(QuyenLamViec.Kham)&&!a.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao)){r.KetQua=null;reason=null;}
        var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{Row=r,Reason=reason}))));
        var name=db.QuerySingleOrDefault<string>("SELECT TenDV FROM dbo.DICH_VU WHERE MaDV=@MaDV",r,tx);
        var person=db.QuerySingleOrDefault<string>("SELECT HoTen FROM dbo.NHAN_VIEN WHERE MaNV=@MaNVThucHien",r,tx);
        return new(r,hash,draft&&(own||a.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao)&&r.MaNVThucHien==a.MaNV),draft&&own,billed,reason,name,person);
    }
    private static void CheckHash(string? actual,string expected){if(actual!=expected)throw new XungDotDuLieuKhamException();}
    private static LoiKhamException Error(string field,string message)=>new(field,message);
    private static void WriteLock(IDbConnection db,IDbTransaction? tx)=>Lock(db,tx,"RHM:ThucHien:Write");
    private static void Lock(IDbConnection db,IDbTransaction? tx,string key)
    {if(db.ExecuteScalar<int>("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; SELECT @r",new{key},tx)<0)throw new InvalidOperationException("Dịch vụ đang được xử lý. Thử lại sau.");}
    private static void Audit(IDbConnection db,IDbTransaction? tx,NguCanhNghiepVu a,string action,string id,string? before,string? after)=>db.Execute("INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuTruoc,DuLieuSau) VALUES(@nk,@MaTK,SYSDATETIME(),@action,N'DICH_VU_THUC_HIEN',@id,N'Thành công',@before,@after)",new{nk=TaoMa(db,tx,"NHAT_KY_THAO_TAC","MaNK"),a.MaTK,action,id,before,after},tx);
    private sealed class Visit
    {public string MaLS{get;set;}="";public string MaBN{get;set;}="";public string HoTenBN{get;set;}="";public string MaHS{get;set;}="";public string MaBS{get;set;}="";public string BacSiPhieu{get;set;}="";public DateTime ThoiGianKham{get;set;}public string TrangThai{get;set;}="";public string TrangThaiPhieu{get;set;}="";}
    private sealed class Step
    {public string MaDV{get;set;}="";public decimal SoLuongDuKien{get;set;}public string TrangThai{get;set;}="";public string PlanStatus{get;set;}="";public string MaHS{get;set;}="";}
}
