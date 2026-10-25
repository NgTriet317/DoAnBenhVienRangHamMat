using RangHamMat.ClinicalChecks;

try
{
    var passed = 0;
    var failed = 0;
    foreach (var (name, run) in QuyenVaTiepNhanChecks.Cases().Concat(PhamViGiaoDienChecks.Cases()))
    {
        try
        {
            using var fixture = new Fixture();
            run(fixture);
            Console.WriteLine("PASS: " + name);
            passed++;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL: {name}: {ex.Message}");
            failed++;
        }
    }
    Console.WriteLine($"ClinicalChecks: {passed} PASS, {failed} FAIL; toàn bộ fixture rollback.");
    Console.WriteLine("Chưa nghiệm thu: tiếp nhận mới, migration, kế hoạch, kê đơn, thanh toán trọn gói và WPF.");
    return failed == 0 ? 0 : 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine("FAIL: " + ex);
    return 1;
}
