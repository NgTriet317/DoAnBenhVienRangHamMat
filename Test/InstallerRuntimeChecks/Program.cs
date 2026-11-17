using System.Text.Json;
using RangHamMat.Services;

// Build win-x64 self-contained, chép riêng exe/DLL/deps/runtimeconfig cạnh exe của MSI,
// rồi chạy chính apphost kiểm tra. Không chép DLL phụ thuộc vào bản giải nén.
// Tiến trình API con phải khởi động bằng chính apphost/runtime đã được đóng gói.
try
{
    var emptyRuntime = Path.Combine(AppContext.BaseDirectory, "NoSystemDotnet");
    Directory.CreateDirectory(emptyRuntime);
    Environment.SetEnvironmentVariable("PATH", Environment.GetFolderPath(Environment.SpecialFolder.System));
    Environment.SetEnvironmentVariable("DOTNET_ROOT", emptyRuntime);
    Environment.SetEnvironmentVariable("DOTNET_ROOT_X64", emptyRuntime);
    Environment.SetEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0");
    Environment.SetEnvironmentVariable("RHM_CONNECTION_STRING",
        "Server=127.0.0.1,1;Database=RHM_Installer_NoSql;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=1");
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
    var endpoint = await KioskApiHost.Instance.StartAsync(null, null, timeout.Token);
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    using var response = await client.GetAsync(endpoint + "/health", timeout.Token);
    response.EnsureSuccessStatusCode();
    using var health = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
    if (health.RootElement.GetProperty("status").GetString() != "ok")
        throw new Exception("Backend khong tra health=ok.");
    Console.WriteLine("PASS: KioskApiHost mo backend trong MSI khi PATH khong co dotnet va DOTNET_ROOT khong hop le.");
    Console.WriteLine("PASS: Backend tra /health=ok, khong ket noi SQL.");
    return 0;
}
catch (Exception error)
{
    Console.WriteLine("FAIL: " + error.GetBaseException().Message);
    return 1;
}
finally
{
    KioskApiHost.Instance.Stop();
}
