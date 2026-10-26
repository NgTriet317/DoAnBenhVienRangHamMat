using System.Data;
using BUS;
using Dapper;
using Microsoft.Data.SqlClient;

namespace RangHamMat.ClinicalChecks;

internal static class PricingConcurrencyChecks
{
    internal static int Run()
    {
        using var db = MigrationChecks.Open();
        if (!db.Database.StartsWith("RHM_ClinicalWorkflow_Test_P08Concurrency_", StringComparison.Ordinal) ||
            db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.BENH_NHAN WHERE MaBN LIKE N'CW%'") != 0)
            throw new InvalidOperationException("Bảng tính đồng thời cần database P08Concurrency riêng, chưa có fixture.");
        using var f = new Fixture(); var warehouse = ThanhToanChecks.Prepare(f); var actor = f.User("C");
        var counts = f.Scalar<string>("SELECT CONCAT((SELECT COUNT(*) FROM dbo.HOA_DON),':',(SELECT COUNT(*) FROM dbo.PHIEU_XUAT),':',(SELECT COUNT(*) FROM dbo.THANH_TOAN),':',(SELECT SUM(SoLuongTon) FROM dbo.VW_TON_KHO))");
        f.Transaction.Commit();
        using var start = new Barrier(2);
        var tasks = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
        {
            if (!start.SignalAndWait(TimeSpan.FromSeconds(10))) throw new Exception("Worker chưa sẵn sàng.");
            return new BUS_THANHTOANTRONGOI().LapBangTinh(f.Id("LSA"), warehouse, actor);
        })).ToArray();
        var results = Task.WhenAll(tasks).GetAwaiter().GetResult();
        if (results.Any(r => r.TongTien != 150000) || results[0].DauVanTay != results[1].DauVanTay) throw new Exception("Hai bảng tính cùng nguồn khác nhau.");
        Console.WriteLine("PASS: Hai kết nối xem cùng lượt có cùng tổng và token, không tạo bộ tài chính");
        using (var tx = db.BeginTransaction(IsolationLevel.ReadCommitted))
        {
            var fee = new BUS_THANHTOANTRONGOI(tx).LapBangTinh(f.Id("LSA"), warehouse, actor);
            foreach (var (sql, id) in new[] {
                ("UPDATE dbo.THUOC SET DonGiaBan=DonGiaBan+1 WHERE MaThuoc=@id", fee.Thuoc[0].MaThuoc),
                ("UPDATE dbo.LO_HANG_HOA SET SoLo=CONCAT(SoLo,N' FIXTURE') WHERE MaLo=@id", fee.Thuoc[0].CacLo[0].MaLo) })
            {
                var held = Task.Run(() => { using var other = MigrationChecks.Open(); try { other.Execute("SET LOCK_TIMEOUT 1000; " + sql, new { id }); return false; } catch (SqlException ex) when (ex.Number == 1222) { return true; } }).GetAwaiter().GetResult();
                if (!held) throw new Exception("Mất khóa nguồn bảng tính trong caller ReadCommitted.");
            }
            tx.Rollback();
        }
        Console.WriteLine("PASS: Caller ReadCommitted giữ giá thuốc và lô nguồn đến rollback");
        var after = db.ExecuteScalar<string>("SELECT CONCAT((SELECT COUNT(*) FROM dbo.HOA_DON),':',(SELECT COUNT(*) FROM dbo.PHIEU_XUAT),':',(SELECT COUNT(*) FROM dbo.THANH_TOAN),':',(SELECT SUM(SoLuongTon) FROM dbo.VW_TON_KHO))");
        if (counts != after) throw new Exception("Xem đồng thời đã ghi tài chính hoặc đổi tồn.");
        Console.WriteLine("PASS: Số hóa đơn/xuất/thu và tồn không đổi sau xem đồng thời");
        return 0;
    }
}
