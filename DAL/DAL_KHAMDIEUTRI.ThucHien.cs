using System.Data;
using Dapper;
using ET;
namespace DAL;

public sealed partial class DAL_KHAMDIEUTRI
{
    public TongKetKham LayTongKetKham(string maLS,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {var(_,visit)=OwnedVisit(db,tx,maLS,user,false);return ReadSummary(db,tx,visit);});

    public TongKetKham XacNhanThucHien(string maLS,string dauVanTay,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var(actor,visit)=OwnedVisit(db,tx,maLS,user,true);
        var before=ReadSummary(db,tx,visit);
        if(before.DaThucHien)return before;
        RequireFingerprint(dauVanTay,before.DauVanTay);
        Required(visit.ChanDoan,"ChanDoan","Lưu chẩn đoán trước khi xác nhận thực hiện.");
        if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.KE_HOACH_DIEU_TRI WITH(HOLDLOCK) WHERE MaLS=@maLS AND TrangThai=N'Nháp'",new{maLS},tx)>0)
            throw KhamError("KeHoach","Duyệt hoặc hủy kế hoạch nháp trước khi xác nhận thực hiện.");
        if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DICH_VU_THUC_HIEN WITH(HOLDLOCK) WHERE MaLS=@maLS AND TrangThai=N'Nháp'",new{maLS},tx)>0)
            throw KhamError("DichVu","Còn dịch vụ nháp cần đối chiếu trước thực hiện.");
        db.Execute("SAVE TRANSACTION RhmExecutePlan",transaction:tx);
        try
        {
            ChotDichVuDieuTri(db,tx,visit,ClinicalNow(db,tx));
            var saved=ReadSummary(db,tx,visit);
            ClinicalAudit(db,tx,actor,"Xác nhận đã thực hiện kế hoạch","LICH_SU_KHAM_BENH",maLS,before.DauVanTay,saved.DauVanTay);
            return saved;
        }
        catch{if(db.ExecuteScalar<int>("SELECT XACT_STATE()",transaction:tx)==1)db.Execute("ROLLBACK TRANSACTION RhmExecutePlan",transaction:tx);throw;}
    });

    // Consultation is also an actual service. A visit without a treatment plan still confirms the examination here.
    internal static bool DaThucHienXong(IDbConnection db,IDbTransaction? tx,string maLS)=>db.ExecuteScalar<bool>("""
        SELECT CONVERT(bit,CASE WHEN EXISTS(
            SELECT 1 FROM dbo.DICH_VU_THUC_HIEN s WITH(HOLDLOCK)
            JOIN dbo.LICH_SU_KHAM_BENH l WITH(HOLDLOCK) ON l.MaLS=s.MaLS
            JOIN dbo.PHIEU_KHAM_BENH p WITH(HOLDLOCK) ON p.MaPhieu=l.MaPhieu
            WHERE s.MaLS=@maLS AND s.MaDV=p.MaDV AND s.TrangThai=N'Đã xác nhận')
        AND NOT EXISTS(SELECT 1 FROM dbo.KE_HOACH_DIEU_TRI WITH(HOLDLOCK) WHERE MaLS=@maLS AND TrangThai NOT IN(N'Đã hoàn thành',N'Hủy'))
        AND NOT EXISTS(SELECT 1 FROM dbo.KE_HOACH_DIEU_TRI k WITH(HOLDLOCK)
            JOIN dbo.CHI_TIET_KE_HOACH c WITH(HOLDLOCK) ON c.MaKHDT=k.MaKHDT
            WHERE k.MaLS=@maLS AND k.TrangThai<>N'Hủy' AND c.TrangThai<>N'Hủy' AND
             (c.TrangThai<>N'Đã hoàn thành' OR c.SoLuongDuKien<>(SELECT COALESCE(SUM(s.SoLuong),0)
                FROM dbo.DICH_VU_THUC_HIEN s WITH(HOLDLOCK) WHERE s.MaCTKH=c.MaCTKH AND s.TrangThai=N'Đã xác nhận')))
        AND NOT EXISTS(SELECT 1 FROM dbo.DICH_VU_THUC_HIEN WITH(HOLDLOCK) WHERE MaLS=@maLS AND TrangThai=N'Nháp')
        THEN 1 ELSE 0 END)
        """,new{maLS},tx);

    private static TongKetKham ReadSummary(IDbConnection db,IDbTransaction? tx,VisitRead visit)
    {
        var services=db.Query<DichVuTongKetKham>("""
            SELECT s.MaDVThucHien,s.MaDV,d.TenDV,s.SoLuong,s.DonGia,s.ThanhTien,s.ThoiGianThucHien,s.KetQua
            FROM dbo.DICH_VU_THUC_HIEN s WITH(HOLDLOCK) JOIN dbo.DICH_VU d WITH(HOLDLOCK) ON d.MaDV=s.MaDV
            WHERE s.MaLS=@MaLS AND s.TrangThai=N'Đã xác nhận' ORDER BY s.ThoiGianThucHien,s.MaDVThucHien
            """,visit,tx).ToArray();
        var drugs=db.Query<ThuocTongKetKham>("""
            SELECT d.MaDT,c.MaCTDT,c.MaThuoc,COALESCE(c.TenThuocChot,h.TenHH) TenThuoc,
                COALESCE(c.HamLuongChot,t.HamLuong) HamLuong,COALESCE(c.DonViTinhChot,h.DonViTinh) DonViTinh,
                c.SoLuong,c.LieuDung,c.DuongDung,c.SoNgayDung,c.HuongDan,COALESCE(bill.DonGia,t.DonGiaBan) DonGia
            FROM dbo.DON_THUOC d WITH(HOLDLOCK) JOIN dbo.CHI_TIET_DON_THUOC c WITH(HOLDLOCK) ON c.MaDT=d.MaDT
            JOIN dbo.THUOC t WITH(HOLDLOCK) ON t.MaThuoc=c.MaThuoc JOIN dbo.HANG_HOA h WITH(HOLDLOCK) ON h.MaHH=t.MaHH
            OUTER APPLY(SELECT TOP(1) b.DonGia FROM dbo.CHI_TIET_HOA_DON b WITH(HOLDLOCK)
                JOIN dbo.HOA_DON hd WITH(HOLDLOCK) ON hd.MaHD=b.MaHD
                LEFT JOIN dbo.CHI_TIET_PHIEU_XUAT x WITH(HOLDLOCK) ON x.MaCTPX=b.MaCTPX
                WHERE hd.MaLS=@MaLS AND hd.TaoKhiHoanTat=1 AND(b.MaCTDT=c.MaCTDT OR x.MaCTDT=c.MaCTDT)
                ORDER BY b.MaCTHD) bill
            WHERE d.MaLS=@MaLS AND d.TrangThai=N'Đã xác nhận' ORDER BY d.NgayKe,d.MaDT,c.MaCTDT
            """,visit,tx).ToArray();
        var plans=db.Query<ET_KEHOACHDIEUTRI>("SELECT * FROM dbo.KE_HOACH_DIEU_TRI WITH(HOLDLOCK) WHERE MaLS=@MaLS ORDER BY MaKHDT",visit,tx).ToArray();
        var steps=db.Query<ET_CHITIETKEHOACH>("SELECT c.* FROM dbo.CHI_TIET_KE_HOACH c WITH(HOLDLOCK) JOIN dbo.KE_HOACH_DIEU_TRI k WITH(HOLDLOCK) ON k.MaKHDT=c.MaKHDT WHERE k.MaLS=@MaLS ORDER BY c.MaCTKH",visit,tx).ToArray();
        var drafts=db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DON_THUOC WITH(HOLDLOCK) WHERE MaLS=@MaLS AND TrangThai=N'Nháp'",visit,tx);
        return new(services,drugs,DaThucHienXong(db,tx,visit.MaLS),drafts,Fingerprint(new{Clinical=ClinicalSnapshot(db,tx,visit).DauVanTay,Plans=plans,Steps=steps,Services=services}));
    }
}
