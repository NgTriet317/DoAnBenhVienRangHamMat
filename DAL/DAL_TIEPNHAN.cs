using System.Data;
using Dapper;
using ET;

namespace DAL;

public sealed class DAL_TIEPNHAN(IDbTransaction? transaction=null) : dbconnect(transaction)
{
    public IReadOnlyList<BacSiTiepNhan> LayBacSi(string maDV,NguCanhNghiepVu nguoiDung) => ThucHien((db,tx)=>
    {
        Authorize(db,tx,nguoiDung);
        return Candidates(db,tx,maDV,Now(db,tx));
    });

    public KetQuaTiepNhan TiepNhan(YeuCauTiepNhan yeuCau,NguCanhNghiepVu nguoiDung) => ThucHien((db,tx)=>
    {
        ArgumentNullException.ThrowIfNull(yeuCau);
        // Cổng ghi tiếp nhận có thứ tự khóa chung, trước cả đọc định danh; tránh nâng khóa chéo khi hai quầy cấp mã.
        Lock(db,tx,"RHM:TiepNhan:Write");
        var user=Authorize(db,tx,nguoiDung);
        var r=yeuCau with {MaBN=Code(yeuCau.MaBN,"MaBN"),MaDV=Code(yeuCau.MaDV,"MaDV"),MaBS=Code(yeuCau.MaBS,"MaBS"),MaPhong=Code(yeuCau.MaPhong,"MaPhong"),MaLich=string.IsNullOrWhiteSpace(yeuCau.MaLich)?null:Code(yeuCau.MaLich,"MaLich")};
        if(string.IsNullOrWhiteSpace(r.LyDoKham)) throw Error("LyDoKham","Nhập lý do đến khám.");
        var now=Now(db,tx);
        if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.BENH_NHAN WITH(UPDLOCK,HOLDLOCK) WHERE MaBN=@MaBN AND TrangThai=N'Đang theo dõi'",r,tx)!=1) throw Error("MaBN","Bệnh nhân không tồn tại hoặc đã ngừng theo dõi.");
        var histories=db.Query<ET_HOSOBENHAN>("SELECT * FROM dbo.HO_SO_BENH_AN WITH(UPDLOCK,HOLDLOCK) WHERE MaBN=@MaBN",r,tx).ToArray();
        if(histories.Length>1 || histories.Any(h=>h.TrangThai!="Đang theo dõi")) throw Error("MaBN","Hồ sơ bệnh án cần được đối chiếu trước khi tiếp nhận.");
        var history=histories.SingleOrDefault();
        if(r.MaLich is not null)
        {
            var appointment=db.QuerySingleOrDefault<ET_LICHKHAM>("SELECT * FROM dbo.LICH_KHAM WITH(UPDLOCK,HOLDLOCK) WHERE MaLich=@MaLich",r,tx);
            if(appointment is null || appointment.MaBN!=r.MaBN || appointment.MaBS!=r.MaBS || appointment.MaPhong!=r.MaPhong || (appointment.MaDV is not null && appointment.MaDV!=r.MaDV)) throw Error("MaLich","Lịch hẹn phải khớp bệnh nhân, dịch vụ, bác sĩ và phòng; chỉnh lịch tại chức năng lịch hẹn trước.");
            if(appointment.NgayGioKham.Date!=now.Date || appointment.TrangThai is not("Nháp" or "Đã xác nhận" or "Chờ khám")) throw Error("MaLich","Chỉ tiếp nhận lịch còn hiệu lực trong ngày hôm nay.");
            var old=db.QuerySingleOrDefault<ET_PHIEUKHAMBENH>("SELECT * FROM dbo.PHIEU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaLich=@MaLich AND TrangThai<>N'Hủy'",r,tx);
            if(old is not null)
            {
                if(old.MaHS!=history?.MaHS || old.MaBS!=r.MaBS || old.MaPhong!=r.MaPhong || old.MaDV!=r.MaDV) throw Error("MaLich","Lịch đã có phiếu tiếp nhận khác; hãy mở phiếu đã lưu.");
                return Result(r.MaBN,old);
            }
        }
        var doctor=Candidates(db,tx,r.MaDV,now).SingleOrDefault(d=>d.MaNV==r.MaBS)
            ?? throw Error("MaBS","Không có bác sĩ phù hợp đang làm việc trong thời điểm tiếp nhận.");
        if(doctor.ThieuLich)
        {
            if(!user.CoQuyen(QuyenLamViec.DieuPhoiNgoaiLich)) throw DAL_QUYENNGHIEPVU.TuChoi();
            if(string.IsNullOrWhiteSpace(r.LyDoDieuPhoiNgoaiLich) || r.LyDoDieuPhoiNgoaiLich.Trim().Length>500) throw Error("LyDoDieuPhoiNgoaiLich","Chưa có lịch làm việc để đối chiếu. Nhập lý do điều phối (tối đa 500 ký tự).");
        }
        else if(doctor.MaPhong!=r.MaPhong) throw Error("MaPhong","Phòng phải khớp lịch làm việc hiện tại của bác sĩ.");
        if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.PHONG WITH(HOLDLOCK) WHERE MaPhong=@MaPhong AND MaKhoa=@MaKhoa AND TrangThai=N'Đang hoạt động' AND LoaiPhong LIKE N'Phòng khám%'",new{r.MaPhong,doctor.MaKhoa},tx)!=1) throw Error("MaPhong","Chọn phòng khám đang hoạt động của chuyên khoa dịch vụ.");
        if(r.MaLich is null && history is not null)
        {
            var old=db.QuerySingleOrDefault<ET_PHIEUKHAMBENH>("""
                SELECT TOP(1) * FROM dbo.PHIEU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK)
                WHERE MaHS=@hs AND MaDV=@MaDV AND MaBS=@MaBS AND MaPhong=@MaPhong AND MaLich IS NULL
                  AND ThoiGianTiepNhan>=@day AND ThoiGianTiepNhan<DATEADD(day,1,@day)
                  AND TrangThai IN(N'Chờ khám',N'Đang khám',N'Chờ thanh toán',N'Chờ nhận thuốc') ORDER BY MaPhieu
                """,new{hs=history.MaHS,r.MaDV,r.MaBS,r.MaPhong,day=now.Date},tx);
            if(old is not null) return Result(r.MaBN,old);
        }
        if(history is null)
        {
            history=new ET_HOSOBENHAN {MaHS=TaoMa(db,tx,"HO_SO_BENH_AN","MaHS"),MaBN=r.MaBN,NgayLap=now,LyDoKhamBanDau=r.LyDoKham,TrangThai="Đang theo dõi"};
            KiemTra<ET_HOSOBENHAN>(db,tx,"HO_SO_BENH_AN",history,null,null);
            db.Execute("INSERT dbo.HO_SO_BENH_AN(MaHS,MaBN,NgayLap,LyDoKhamBanDau,GhiChu,TrangThai) VALUES(@MaHS,@MaBN,@NgayLap,@LyDoKhamBanDau,@GhiChu,@TrangThai)",history,tx);
        }
        Lock(db,tx,$"RHM:TiepNhan:Queue:{now:yyyyMMdd}:{r.MaPhong}");
        var order=checked(db.ExecuteScalar<int>("SELECT COALESCE(MAX(SoThuTu),0) FROM dbo.PHIEU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaPhong=@MaPhong AND ThoiGianTiepNhan>=@day AND ThoiGianTiepNhan<DATEADD(day,1,@day)",new{r.MaPhong,day=now.Date},tx)+1);
        var receipt=new ET_PHIEUKHAMBENH {MaPhieu=TaoMa(db,tx,"PHIEU_KHAM_BENH","MaPhieu"),MaHS=history.MaHS,MaLich=r.MaLich,MaBS=r.MaBS,MaPhong=r.MaPhong,MaDV=r.MaDV,MaNVTiepNhan=user.MaNV,SoThuTu=order,ThoiGianTiepNhan=now,LyDoKham=r.LyDoKham,TrangThai="Chờ khám",GhiChu=r.GhiChu};
        KiemTra<ET_PHIEUKHAMBENH>(db,tx,"PHIEU_KHAM_BENH",receipt,null,null);
        db.Execute("""
            INSERT dbo.PHIEU_KHAM_BENH(MaPhieu,MaHS,MaLich,MaPhong,MaBS,MaNVTiepNhan,MaDV,SoThuTu,ThoiGianTiepNhan,LyDoKham,TrangThai,GhiChu)
            VALUES(@MaPhieu,@MaHS,@MaLich,@MaPhong,@MaBS,@MaNVTiepNhan,@MaDV,@SoThuTu,@ThoiGianTiepNhan,@LyDoKham,@TrangThai,@GhiChu)
            """,receipt,tx);
        if(r.MaLich is not null) db.Execute("UPDATE dbo.LICH_KHAM SET TrangThai=N'Chờ khám' WHERE MaLich=@MaLich AND TrangThai IN(N'Nháp',N'Đã xác nhận')",r,tx);
        db.Execute("""
            INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuSau)
            VALUES(@nk,@MaTK,@now,@action,N'PHIEU_KHAM_BENH',@id,N'Thành công',@detail)
            """,new{nk=TaoMa(db,tx,"NHAT_KY_THAO_TAC","MaNK"),user.MaTK,now,action=doctor.ThieuLich?"Tiếp nhận ngoài lịch":"Tiếp nhận khám",id=receipt.MaPhieu,detail=System.Text.Json.JsonSerializer.Serialize(new{r.MaBN,history.MaHS,receipt.MaPhieu,r.MaBS,r.MaDV,r.MaPhong,receipt.SoThuTu,LyDoDieuPhoi=doctor.ThieuLich?r.LyDoDieuPhoiNgoaiLich:null})},tx);
        return Result(r.MaBN,receipt);
    });

    private static IReadOnlyList<BacSiTiepNhan> Candidates(IDbConnection db,IDbTransaction? tx,string service,DateTime now)
    {
        Code(service,"MaDV");
        var specialty=db.QuerySingleOrDefault<string>("SELECT MaCK FROM dbo.DICH_VU WITH(HOLDLOCK) WHERE MaDV=@service AND TrangThai=N'Đang hoạt động'",new{service},tx);
        if(string.IsNullOrWhiteSpace(specialty)) throw Error("MaDV","Dịch vụ cần đang hoạt động và có chuyên khoa để phân bác sĩ.");
        return db.Query<BacSiTiepNhan>("""
            WITH shifts AS(
              SELECT l.*,DATEADD(second,DATEDIFF(second,CAST('00:00:00' AS time),c.GioBatDau),CAST(l.NgayLam AS datetime2(0))) Begins,
                DATEADD(day,CASE WHEN c.GioKetThuc<=c.GioBatDau THEN 1 ELSE 0 END,DATEADD(second,DATEDIFF(second,CAST('00:00:00' AS time),c.GioKetThuc),CAST(l.NgayLam AS datetime2(0)))) Ends,c.TrangThai CaStatus
              FROM dbo.LICH_LAM_VIEC l WITH(HOLDLOCK) JOIN dbo.CA_LAM c WITH(HOLDLOCK) ON c.MaCa=l.MaCa
              WHERE l.NgayLam BETWEEN DATEADD(day,-1,CAST(@now AS date)) AND CAST(@now AS date))
            SELECT n.MaNV,n.HoTen,n.MaCK,ck.MaKhoa,s.MaPhong,p.TenPhong,s.MaCa,
              CONVERT(bit,CASE WHEN s.MaLLV IS NULL THEN 1 ELSE 0 END) ThieuLich,
              (SELECT COUNT(*) FROM dbo.PHIEU_KHAM_BENH q WHERE q.MaBS=n.MaNV AND q.TrangThai IN(N'Chờ khám',N'Đang khám') AND q.ThoiGianTiepNhan>=CAST(@now AS date) AND q.ThoiGianTiepNhan<DATEADD(day,1,CAST(@now AS date))) SoDangCho
            FROM dbo.NHAN_VIEN n WITH(HOLDLOCK) JOIN dbo.CHUYEN_KHOA ck WITH(HOLDLOCK) ON ck.MaCK=n.MaCK
            OUTER APPLY(SELECT TOP(1) * FROM shifts s WHERE s.MaNV=n.MaNV AND @now>=s.Begins AND @now<s.Ends
              AND s.TrangThai=N'Đã xác nhận' AND s.TrangThaiHienDien NOT IN(N'Vắng mặt',N'Nghỉ phép') AND s.CaStatus=N'Đang hoạt động' ORDER BY s.MaLLV) s
            LEFT JOIN dbo.PHONG p WITH(HOLDLOCK) ON p.MaPhong=s.MaPhong AND p.TrangThai=N'Đang hoạt động' AND p.MaKhoa=ck.MaKhoa AND p.LoaiPhong LIKE N'Phòng khám%'
            WHERE n.MaCK=@specialty AND n.MaChucVu=N'CV002' AND n.TrangThai=N'Đang làm việc'
              AND EXISTS(SELECT 1 FROM dbo.TAI_KHOAN t WITH(HOLDLOCK) WHERE t.MaNV=n.MaNV AND t.TrangThai=N'Đang hoạt động')
              AND NOT EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI d WITH(HOLDLOCK) WHERE d.MaNV=n.MaNV AND d.TrangThaiDuyet=N'Đã duyệt' AND CAST(@now AS date) BETWEEN d.TuNgay AND d.DenNgay)
              AND ((s.MaLLV IS NOT NULL AND p.MaPhong IS NOT NULL) OR NOT EXISTS(SELECT 1 FROM shifts d WHERE d.MaNV=n.MaNV AND(d.NgayLam=CAST(@now AS date) OR @now>=d.Begins AND @now<d.Ends)))
            ORDER BY n.HoTen,n.MaNV
            """,new{specialty,now},tx).ToArray();
    }
    private static NguCanhNghiepVu Authorize(IDbConnection db,IDbTransaction? tx,NguCanhNghiepVu user)
    {var current=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);if(!current.CoQuyen(QuyenLamViec.TiepNhan))throw DAL_QUYENNGHIEPVU.TuChoi();return current;}
    private static DateTime Now(IDbConnection db,IDbTransaction? tx)=>db.ExecuteScalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())",transaction:tx);
    private static string Code(string? code,string field)=>!string.IsNullOrWhiteSpace(code)&&code.Trim().Length<=50?code.Trim():throw Error(field,"Chọn "+field+" hợp lệ trước khi tiếp nhận.");
    private static LoiTiepNhanException Error(string field,string message)=>new(field,message);
    private static KetQuaTiepNhan Result(string patient,ET_PHIEUKHAMBENH receipt)=>new(patient,receipt.MaHS,receipt.MaPhieu,receipt.MaBS!,receipt.SoThuTu,receipt.TrangThai);
    private static void Lock(IDbConnection db,IDbTransaction? tx,string resource)
    {
        var result=db.ExecuteScalar<int>("DECLARE @result int; EXEC @result=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; SELECT @result",new{resource},tx);
        if(result<0)throw new InvalidOperationException("Quầy khác đang tiếp nhận. Vui lòng thử lại.");
    }
}
