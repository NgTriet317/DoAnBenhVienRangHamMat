using System.Data;
using Dapper;
using ET;
namespace DAL;

public sealed partial class DAL_KHAMDIEUTRI
{
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
