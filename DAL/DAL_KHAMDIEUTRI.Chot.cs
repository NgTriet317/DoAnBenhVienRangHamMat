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
    /// Chốt lần khám từ bản tổng kết đã xem, phối hợp xác nhận đơn thuốc và lập hóa đơn để giữ nhất quán các chứng từ. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch. Dùng application lock sở hữu bởi giao dịch; từ chối khi không lấy được khóa trong thời gian chờ. Đối chiếu dấu vân tay trước khi ghi để phát hiện bản dữ liệu đã bị thay đổi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>db.ExecuteScalar&lt;int&gt;</c>, <c>KhamError</c>, <c>OwnedVisit</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="dauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
    /// <param name="dauVanTayTongKet">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapKham dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapKham ChotKhamVaLapHoaDon(string maLS,string dauVanTay,string dauVanTayTongKet,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        // Same order as the prescription writer: global Rx lock, then visit lock.
        if(db.ExecuteScalar<int>("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'RHM:DonThuoc:Write',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; SELECT @r",transaction:tx)<0)
            throw KhamError("DonThuoc","Đơn đang được xử lý. Thử lại sau.");
        var(actor,visit)=OwnedVisit(db,tx,maLS,user,false);
        var original=ClinicalSnapshot(db,tx,visit);
        if(visit.TrangThai=="Đã xác nhận")return original;
        if(!original.DuocSua)throw DAL_QUYENNGHIEPVU.TuChoi();
        RequireFingerprint(dauVanTay,original.DauVanTay);
        var summary=ReadSummary(db,tx,visit);RequireFingerprint(dauVanTayTongKet,summary.DauVanTay);
        if(!summary.DaThucHien)throw KhamError("ThucHien","Xác nhận thực hiện khám/điều trị trước khi chốt.");
        db.Execute("SAVE TRANSACTION RhmFinalizeRx",transaction:tx);
        try
        {
            var rx=new DAL_DONTHUOC(tx);
            var drafts=db.Query<string>("SELECT MaDT FROM dbo.DON_THUOC WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@maLS AND TrangThai=N'Nháp' ORDER BY MaDT",new{maLS},tx).ToArray();
            foreach(var id in drafts)
            {
                var draft=rx.LayBanNhapDon(id,actor);
                rx.XacNhanDon(id,draft.DauVanTay,actor);
            }
            return new DAL_KHAMDIEUTRI(tx).HoanTatKham(maLS,dauVanTay,actor);
        }
        catch{if(db.ExecuteScalar<int>("SELECT XACT_STATE()",transaction:tx)==1)db.Execute("ROLLBACK TRANSACTION RhmFinalizeRx",transaction:tx);throw;}
    });
}
