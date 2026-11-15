using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;

if (args.Length != 3 || !System.Text.RegularExpressions.Regex.IsMatch(args[1], "^RHM_Hospital_Test_[A-Za-z0-9_]+$"))
    throw new ArgumentException("Usage: server RHM_Hospital_Test_database backend.dll");
var connection = new SqlConnectionStringBuilder { DataSource=args[0], InitialCatalog=args[1], IntegratedSecurity=true, TrustServerCertificate=true };
await using var db = new SqlConnection(connection.ConnectionString);
await db.OpenAsync();
int receiptsBefore = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.PHIEU_KHAM_BENH");
int patientsBefore = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.BENH_NHAN");
var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
int port = ((IPEndPoint)listener.LocalEndpoint).Port;
listener.Stop();
var start = new ProcessStartInfo("dotnet") { UseShellExecute=false, CreateNoWindow=true, WorkingDirectory=Path.GetDirectoryName(Path.GetFullPath(args[2]))! };
start.ArgumentList.Add(Path.GetFullPath(args[2]));
start.Environment["RHM_CONNECTION_STRING"] = connection.ConnectionString;
start.Environment["RHM_KIOSK_EMBEDDED_PORT"] = port.ToString();
using var process = Process.Start(start) ?? throw new Exception("Cannot start backend");
try
{
    using var http = new HttpClient { BaseAddress=new Uri($"http://127.0.0.1:{port}"), Timeout=TimeSpan.FromSeconds(5) };
    bool healthy = false;
    for (int attempt=0; attempt<80 && !process.HasExited; attempt++)
    {
        try { using var health=await http.GetAsync("/health"); if (health.IsSuccessStatusCode) { healthy=true; break; } }
        catch (HttpRequestException) { }
        await Task.Delay(250);
    }
    Check(healthy, "Backend starts from current build");
    using var page = await http.GetAsync("/");
    Check(page.IsSuccessStatusCode && (await page.Content.ReadAsStringAsync()).Contains("html", StringComparison.OrdinalIgnoreCase), "Kiosk static page included");
    using var sqlHealth=await http.GetAsync("/health/db");
    Check(sqlHealth.IsSuccessStatusCode, "Backend uses the selected test database");
    using var blocked=await http.PostAsJsonAsync("/api/ai/intake/receipts", new {});
    Check(blocked.StatusCode==HttpStatusCode.Gone, "AI cannot create official receipts");
    Guid session=Guid.NewGuid();
    object Draft(Guid id, bool confirmed=true, bool staff=false, string phone="0912345678") => new {
        SessionId=id, PatientConfirmed=confirmed, RequiresStaff=staff,
        Patient=new { FullName="Nguyễn Kiểm Thử", DateOfBirth="2000-01-01", Phone=phone, PreviousVisit="no" },
        Visit=new { Reason="Kiểm tra tích hợp", ExamType="service" }
    };
    using var rejected=await http.PostAsJsonAsync("/api/ai/intake/drafts", Draft(Guid.NewGuid(), confirmed:false));
    Check(rejected.StatusCode==HttpStatusCode.BadRequest, "Unconfirmed intake rejected");
    using var invalid=await http.PostAsJsonAsync("/api/ai/intake/drafts", Draft(Guid.NewGuid(), phone:"123"));
    Check(invalid.StatusCode==HttpStatusCode.BadRequest, "Invalid phone rejected");
    for (int attempt=0; attempt<2; attempt++)
    {
        using var response=await http.PostAsJsonAsync("/api/ai/intake/drafts", Draft(session));
        Check(response.IsSuccessStatusCode, "Confirmed draft saved/retried");
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Check(json.RootElement.GetProperty("status").GetString()=="saved" && json.RootElement.GetProperty("maPhien").GetGuid()==session, "Draft response identifies actual saved session");
    }
    Check(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.AI_PHIEN_TIEP_NHAN WHERE MaPhien=@session AND TrangThai=N'ChoNhanVien' AND MaPhieu IS NULL", new {session})==1, "Retry creates one staff draft");
    Check(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.AI_TIEP_NHAN_NHAP WHERE MaPhien=@session AND LoaiKham=N'DichVu'", new {session})==1, "Exam type linked to staff review");
    Guid urgent=Guid.NewGuid();
    using var urgentResponse=await http.PostAsJsonAsync("/api/ai/intake/drafts", Draft(urgent, staff:true));
    Check(urgentResponse.IsSuccessStatusCode && await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.AI_PHIEN_TIEP_NHAN WHERE MaPhien=@urgent AND TrangThai=N'CanHoTro'", new {urgent})==1, "Urgent draft remains for staff");
    using var crossOrigin=new HttpRequestMessage(HttpMethod.Post, "/api/ai/intake/drafts") { Content=JsonContent.Create(Draft(Guid.NewGuid())) };
    crossOrigin.Headers.Add("Origin", "http://example.invalid");
    using var forbidden=await http.SendAsync(crossOrigin);
    Check(forbidden.StatusCode==HttpStatusCode.Forbidden, "Cross-origin submission rejected");
    Check(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.PHIEU_KHAM_BENH")==receiptsBefore && await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.BENH_NHAN")==patientsBefore, "Drafts do not create patients or official receipts");
    Console.WriteLine("PASS: API integration checks");
}
finally
{
    if (!process.HasExited) { process.Kill(entireProcessTree:true); await process.WaitForExitAsync(); }
}
static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException("FAIL: "+message);
    Console.WriteLine("PASS: "+message);
}
