using BUS;
using Dapper;
using ET;
using System.Data;
using Microsoft.Data.SqlClient;
namespace RangHamMat.ClinicalChecks;

internal static class ServiceConcurrencyChecks
{
    private static T[] Pair<T>(Func<int,T> action)
    {
        using var start=new Barrier(2);
        Task<T> Send(int i)=>Task.Run(()=>{if(!start.SignalAndWait(TimeSpan.FromSeconds(10)))throw new Exception("Worker chưa sẵn sàng.");return action(i);});
        return Task.WhenAll(Send(1),Send(2)).GetAwaiter().GetResult();
    }
    internal static int Run()
    {
        using var db=MigrationChecks.Open();
        if(!db.Database.StartsWith("RHM_ClinicalWorkflow_Test_P06Concurrency_",StringComparison.Ordinal))throw new InvalidOperationException("Ca ký đồng thời cần database thử riêng P06Concurrency; không dùng bản thử chính.");
        if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.BENH_NHAN WHERE MaBN LIKE N'CW%'")!=0)throw new InvalidOperationException("Database đồng thời đã có fixture; tạo bản trống mới.");
        using var f=new Fixture();var actorB=f.User("B");var principalB=Thread.CurrentPrincipal;var actor=f.User("A");var principal=Thread.CurrentPrincipal;
        var plan=DichVuThucHienChecks.Plan(f,actor,2);var request=DichVuThucHienChecks.Request(f,plan.CacBuoc[0].MaCTKH);
        var original=new BUS_DICHVUTHUCHIEN(f.Transaction).LuuThucHien(request,actor);
        f.Transaction.Commit();
        var confirmed=Pair(_=>new BUS_DICHVUTHUCHIEN().XacNhanThucHien(original.DichVu.MaDVThucHien,original.DauVanTay,actor));
        if(confirmed.Any(s=>s.DichVu.TrangThai!="Đã xác nhận")||db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE MaDoiTuong=@id AND HanhDong=N'Xác nhận dịch vụ'",new{id=original.DichVu.MaDVThucHien})!=1)throw new Exception("Xác nhận cùng dòng không idempotent.");
        Console.WriteLine("PASS: Hai kết nối xác nhận cùng dòng, một lần ký/audit/nguồn phí");
        var reservations=Pair(i=>
        {
            Thread.CurrentPrincipal=i==1?principal:principalB;
            try{return new BUS_DICHVUTHUCHIEN().LuuThucHien(request with {MaLS=f.Id(i==1?"LSA":"LSB")},i==1?actor:actorB).DichVu.MaDVThucHien;}
            catch(LoiKhamException ex)when(ex.Truong=="SoLuong"){return "limit";}
        });
        if(reservations.Count(s=>s=="limit")!=1||db.ExecuteScalar<decimal>("SELECT SUM(SoLuong) FROM dbo.DICH_VU_THUC_HIEN WHERE MaCTKH=@id AND TrangThai IN(N'Nháp',N'Đã xác nhận')",new{id=plan.CacBuoc[0].MaCTKH})!=2)throw new Exception("Hai buổi cùng chiếm phần lượng cuối.");
        Console.WriteLine("PASS: Hai bác sĩ/hai lượt cùng hồ sơ tranh phần lượng cuối, chỉ một lưu");
        using(var tx=db.BeginTransaction(IsolationLevel.ReadCommitted))
        {
            Thread.CurrentPrincipal=principal;
            new BUS_DICHVUTHUCHIEN(tx).LuuThucHien(request with {MaCTKH=null,LyDoPhatSinh="FIXTURE ReadCommitted"},actor);
            var held=Task.Run(()=>
            {
                using var other=MigrationChecks.Open();
                try{other.Execute("SET LOCK_TIMEOUT 1000; UPDATE dbo.DICH_VU SET TrangThai=N'Ngừng hoạt động' WHERE MaDV=@MaDV",request);return false;}
                catch(SqlException ex)when(ex.Number==1222){return true;}
            }).GetAwaiter().GetResult();
            tx.Rollback();if(!held)throw new Exception("ReadCommitted không giữ dịch vụ nguồn đến hết giao dịch.");
        }
        Console.WriteLine("PASS: Caller ReadCommitted giữ khóa nguồn dịch vụ đến rollback");
        Console.WriteLine("PASS: Fixture ký chỉ nằm trong database đồng thời riêng, không tắt trigger để dọn lịch sử");
        return 0;
    }
}
