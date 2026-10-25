using RangHamMat.ClinicalChecks;

try
{
    if (args.Contains("--migration")) return MigrationChecks.Run();
    if (args.Contains("--migration-upgrade")) return MigrationLifecycleChecks.Run(false);
    if (args.Contains("--migration-rollback")) return MigrationLifecycleChecks.Run(true);
    var passed = 0;
    var failed = 0;
    var cases=args.Contains("--configuration") ? CauHinhChecks.Cases() : args.Contains("--new-data") ? DuLieuMoiChecks.Cases() : QuyenVaTiepNhanChecks.Cases().Concat(PhamViGiaoDienChecks.Cases()).Concat(CauHinhChecks.Cases()).Concat(DuLieuMoiChecks.Cases());
    foreach (var (name, run) in cases)
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
    Console.WriteLine("Các ca SQL không thay thế nghiệm thu đầy đủ AT01–AT34; luồng tiếp nhận/khám/thu đủ và giao diện mới còn ở P04–P12.");
    return failed == 0 ? 0 : 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine("FAIL: " + ex);
    return 1;
}
