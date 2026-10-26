using BUS;
using Dapper;
using ET;
using System.Data;
using Microsoft.Data.SqlClient;
namespace RangHamMat.ClinicalChecks;
internal static class PrescriptionConcurrencyChecks
{
    private static T[] Pair<T>(Func<int,T> action)
    {using var start=new Barrier(2);Task<T> Send(int i)=>Task.Run(()=>{if(!start.SignalAndWait(TimeSpan.FromSeconds(10)))throw new Exception("Worker chưa sẵn sàng.");return action(i);});return Task.WhenAll(Send(1),Send(2)).GetAwaiter().GetResult();}
    internal static int Run()
    {
        using var db=MigrationChecks.Open();if(!db.Database.StartsWith("RHM_ClinicalWorkflow_Test_P07Concurrency_",StringComparison.Ordinal))throw new InvalidOperationException("Ký đơn đồng thời cần database P07Concurrency riêng.");if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.BENH_NHAN WHERE MaBN LIKE N'CW%'")!=0)throw new InvalidOperationException("Database thử đồng thời đã có fixture.");
        using var f=new Fixture();f.Execute("DELETE dbo.CHI_TIET_DON_THUOC WHERE MaDT=@id;DELETE dbo.DON_THUOC WHERE MaDT=@id",new{id=f.Id("DTA")});var actor=f.User("A");var local=new BUS_DONTHUOC(f.Transaction);var request=DonThuocChecks.Request(f);var stock=f.Scalar<decimal>("SELECT SUM(SoLuongTon) FROM dbo.VW_TON_KHO");var old=local.LuuDon(request,actor);old=local.XacNhanDon(old.Don.MaDT,old.DauVanTay,actor);DonThuocChecks.Complete(f,actor);var replace=request with {MaDTThayThe=old.Don.MaDT,LyDoThayThe="FIXTURE thay trước thu"};var next=local.LuuDon(replace,actor);f.Transaction.Commit();
        var results=Pair(_=>new BUS_DONTHUOC().XacNhanDon(next.Don.MaDT,next.DauVanTay,actor));
        if(results.Any(r=>r.Don.TrangThai!="Đã xác nhận")||db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE MaDoiTuong=@id AND HanhDong=N'Ký đơn thuốc'",new{id=next.Don.MaDT})!=1||db.ExecuteScalar<string>("SELECT TrangThai FROM dbo.DON_THUOC WHERE MaDT=@id",new{id=old.Don.MaDT})!="Ngừng áp dụng")throw new Exception("Ký thay đơn không idempotent/atomic.");
        Console.WriteLine("PASS: Hai kết nối ký thay đơn, một chữ ký/audit và đổi hiệu lực cũ cùng giao dịch");
        var service=new BUS_DONTHUOC();var pending=service.LuuDon(replace with {MaDTThayThe=next.Don.MaDT},actor);
        var edits=Pair(i=>{try{new BUS_DONTHUOC().LuuDon(DonThuocChecks.Update(pending) with {GhiChu="FIXTURE cửa sổ "+i},actor);return "saved";}catch(XungDotDuLieuKhamException){return "conflict";}});
        if(edits.Count(s=>s=="saved")!=1||edits.Count(s=>s=="conflict")!=1)throw new Exception("Ghi đè nháp đồng thời.");
        try{service.KiemTraDonChoThu(request.MaLS,actor);throw new Exception("Cho tính phí khi còn đơn thay nháp.");}catch(LoiKhamException){}
        pending=service.LayBanNhapDon(pending.Don.MaDT,actor);service.HuyDonNhap(pending.Don.MaDT,pending.DauVanTay,actor);
        var creates=Pair(_=>{try{return new BUS_DONTHUOC().LuuDon(replace with {MaDTThayThe=next.Don.MaDT},actor).Don.MaDT;}catch(LoiKhamException ex)when(ex.Truong=="MaDTThayThe"){return "pending";}});
        if(creates.Count(s=>s=="pending")!=1)throw new Exception("Hai nháp thay cùng nguồn.");
        Console.WriteLine("PASS: Hai cửa sổ sửa cùng nháp có một xung đột; hai yêu cầu thay cùng nguồn chỉ một nháp");
        var activePending=service.LayBanNhapDon(creates.Single(s=>s!="pending"),actor);
        using(var tx=db.BeginTransaction(IsolationLevel.ReadCommitted))
        {
            new BUS_DONTHUOC(tx).LuuDon(DonThuocChecks.Update(activePending) with {GhiChu="FIXTURE ReadCommitted"},actor);
            var held=Task.Run(()=>{using var other=MigrationChecks.Open();try{other.Execute("SET LOCK_TIMEOUT 1000; UPDATE dbo.HANG_HOA SET TrangThai=N'Ngừng hoạt động' WHERE MaHH=(SELECT MaHH FROM dbo.THUOC WHERE MaThuoc=@id)",new{id=request.CacDong[0].MaThuoc});return false;}catch(SqlException ex)when(ex.Number==1222){return true;}}).GetAwaiter().GetResult();tx.Rollback();if(!held)throw new Exception("Mất khóa thuốc nguồn trong ReadCommitted.");
        }
        Console.WriteLine("PASS: Caller ReadCommitted giữ điều kiện thuốc nguồn đến rollback");
        if(db.ExecuteScalar<decimal>("SELECT SUM(SoLuongTon) FROM dbo.VW_TON_KHO")!=stock)throw new Exception("Kê/ký đồng thời làm đổi tồn.");
        Console.WriteLine("PASS: Tồn không đổi sau kê/ký/thay đồng thời; fixture chữ ký chỉ ở database riêng");return 0;
    }
}
