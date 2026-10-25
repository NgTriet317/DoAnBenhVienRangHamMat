using RangHamMat.ClinicalChecks;

try
{
    if (args.Contains("--migration")) return MigrationChecks.Run();
    if (args.Contains("--reception-concurrency")) return TiepNhanConcurrencyChecks.Run();
    if (args.Contains("--examination-concurrency")) return ExaminationConcurrencyChecks.Run();
    if (args.Contains("--migration-upgrade")) return MigrationLifecycleChecks.Run(false);
    if (args.Contains("--migration-rollback")) return MigrationLifecycleChecks.Run(true);
    var passed = 0;
    var failed = 0;
    var cases=args.Contains("--examination") ? KhamVaKeHoachChecks.Cases() : args.Contains("--reception") ? TiepNhanChecks.Cases() : args.Contains("--configuration") ? CauHinhChecks.Cases() : args.Contains("--new-data") ? DuLieuMoiChecks.Cases() : QuyenVaTiepNhanChecks.Cases().Concat(PhamViGiaoDienChecks.Cases()).Concat(CauHinhChecks.Cases()).Concat(DuLieuMoiChecks.Cases()).Concat(TiepNhanChecks.Cases()).Concat(KhamVaKeHoachChecks.Cases());
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
    Console.WriteLine("Các ca SQL không thay thế nghiệm thu đầy đủ AT01–AT34; dịch vụ thực hiện, kê đơn, thu đủ và giao diện hoàn chỉnh còn ở P06–P12.");
    return failed == 0 ? 0 : 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine("FAIL: " + ex);
    return 1;
}
