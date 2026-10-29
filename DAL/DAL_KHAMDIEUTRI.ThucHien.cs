using System.Data;
using Dapper;
using ET;
namespace DAL;

/// <summary>
/// Tầng truy cập dữ liệu khám điều trị, thực hiện truy vấn và kiểm tra trong phạm vi kết nối/giao dịch của dbconnect.
/// </summary>
public sealed partial class DAL_KHAMDIEUTRI
{
    /// <summary>
    /// Đọc các dịch vụ và thuốc thuộc lần khám để tổng kết trước khi chốt; giữ thông tin nguồn và dấu vân tay đối chiếu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>OwnedVisit</c>, <c>ReadSummary</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu TongKetKham dùng cho bước xử lý tiếp theo.</returns>
    public TongKetKham LayTongKetKham(string maLS,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {var(_,visit)=OwnedVisit(db,tx,maLS,user,false);return ReadSummary(db,tx,visit);});

    /// <summary>
    /// Xác nhận các dịch vụ đã thực hiện sau khi đối chiếu dữ liệu hiện tại và quyền của người thao tác. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch. Đối chiếu dấu vân tay trước khi ghi để phát hiện bản dữ liệu đã bị thay đổi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>OwnedVisit</c>, <c>ReadSummary</c>, <c>RequireFingerprint</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="dauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu TongKetKham dùng cho bước xử lý tiếp theo.</returns>
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

    /// <summary>
    /// Đọc nguồn dịch vụ/thuốc và các điều kiện hiện tại để tạo tổng kết lần khám. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>db.Query&lt;DichVuTongKetKham&gt;</c>, <c>db.Query&lt;ThuocTongKetKham&gt;</c>, <c>db.Query&lt;ET_KEHOACHDIEUTRI&gt;</c>, <c>db.Query&lt;ET_CHITIETKEHOACH&gt;</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="visit">Giá trị lần khám dùng khi thực hiện thao tác; kiểu VisitRead.</param>
    /// <returns>Kết quả kiểu TongKetKham dùng cho bước xử lý tiếp theo.</returns>
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
                c.SoLuong,c.LieuDung,c.DuongDung,c.SoNgayDung,c.HuongDan,COALESCE(bill.DonGia,t.DonGiaBan) DonGia,d.TrangThai
            FROM dbo.DON_THUOC d WITH(HOLDLOCK) JOIN dbo.CHI_TIET_DON_THUOC c WITH(HOLDLOCK) ON c.MaDT=d.MaDT
            JOIN dbo.THUOC t WITH(HOLDLOCK) ON t.MaThuoc=c.MaThuoc JOIN dbo.HANG_HOA h WITH(HOLDLOCK) ON h.MaHH=t.MaHH
            OUTER APPLY(SELECT TOP(1) b.DonGia FROM dbo.CHI_TIET_HOA_DON b WITH(HOLDLOCK)
                JOIN dbo.HOA_DON hd WITH(HOLDLOCK) ON hd.MaHD=b.MaHD
                LEFT JOIN dbo.CHI_TIET_PHIEU_XUAT x WITH(HOLDLOCK) ON x.MaCTPX=b.MaCTPX
                WHERE hd.MaLS=@MaLS AND hd.TaoKhiHoanTat=1 AND(b.MaCTDT=c.MaCTDT OR x.MaCTDT=c.MaCTDT)
                ORDER BY b.MaCTHD) bill
            WHERE d.MaLS=@MaLS AND d.TrangThai IN(N'Nháp',N'Đã xác nhận')
                AND NOT EXISTS(SELECT 1 FROM dbo.DON_THUOC pending WITH(HOLDLOCK) WHERE pending.MaDTThayThe=d.MaDT AND pending.TrangThai=N'Nháp')
            ORDER BY d.NgayKe,d.MaDT,c.MaCTDT
            """,visit,tx).ToArray();
        var plans=db.Query<ET_KEHOACHDIEUTRI>("SELECT * FROM dbo.KE_HOACH_DIEU_TRI WITH(HOLDLOCK) WHERE MaLS=@MaLS ORDER BY MaKHDT",visit,tx).ToArray();
        var steps=db.Query<ET_CHITIETKEHOACH>("SELECT c.* FROM dbo.CHI_TIET_KE_HOACH c WITH(HOLDLOCK) JOIN dbo.KE_HOACH_DIEU_TRI k WITH(HOLDLOCK) ON k.MaKHDT=c.MaKHDT WHERE k.MaLS=@MaLS ORDER BY c.MaCTKH",visit,tx).ToArray();
        var drafts=db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DON_THUOC WITH(HOLDLOCK) WHERE MaLS=@MaLS AND TrangThai=N'Nháp'",visit,tx);
        var orders=db.Query<ET_DONTHUOC>("SELECT * FROM dbo.DON_THUOC WITH(HOLDLOCK) WHERE MaLS=@MaLS ORDER BY MaDT",visit,tx).ToArray();
        var orderLines=db.Query<ET_CHITIETDONTHUOC>("SELECT c.* FROM dbo.CHI_TIET_DON_THUOC c WITH(HOLDLOCK) JOIN dbo.DON_THUOC d WITH(HOLDLOCK) ON d.MaDT=c.MaDT WHERE d.MaLS=@MaLS ORDER BY c.MaCTDT",visit,tx).ToArray();
        return new(services,drugs,DaThucHienXong(db,tx,visit.MaLS),drafts,Fingerprint(new{Clinical=ClinicalSnapshot(db,tx,visit).DauVanTay,Plans=plans,Steps=steps,Services=services,Orders=orders,OrderLines=orderLines,Drugs=drugs}));
    }
}
