<#
.SYNOPSIS
Kiểm trạng thái hiển thị đơn thuốc và tính thống nhất của bộ lọc lịch sử.
.DESCRIPTION
Tạo project trong thư mục tạm để kiểm năm tổ hợp trạng thái bản ký/bàn giao.
KiemTraSql đọc cấu hình ứng dụng, lấy truy vấn lịch sử từ DAL và đối chiếu ba bộ lọc chỉ đọc.
.PARAMETER KiemTraSql
Kết nối database trong appsettings.json để kiểm bộ lọc khớp TrangThaiHienThi.
#>
param([switch]$KiemTraSql)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$checkRoot = Join-Path ([IO.Path]::GetTempPath()) ('RhmPrescriptionStatus-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $checkRoot | Out-Null
$etProject = [Security.SecurityElement]::Escape((Join-Path $projectRoot 'DAL/DAL.csproj'))
@"
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><ProjectReference Include="$etProject" /></ItemGroup></Project>
"@ | Set-Content -LiteralPath (Join-Path $checkRoot 'Check.csproj') -Encoding UTF8
@'
using ET;
var cases = new[] {
    ("Đã xác nhận", (string)null, "Đã xác nhận"),
    ("Đã xác nhận", "Chờ giao", "Đã xác nhận"),
    ("Đã xác nhận", "Đã giao", "Đã giao thuốc"),
    ("Ngừng áp dụng", "Đã giao", "Ngừng áp dụng"),
    ("Nháp", "Đã giao", "Nháp")
};
foreach (var (signed, handover, expected) in cases) {
    var row = new DongLichSuDon { TrangThai = signed, TrangThaiBanGiao = handover };
    if (row.TrangThaiHienThi != expected) throw new Exception($"{signed}/{handover}: {row.TrangThaiHienThi} != {expected}");
    if (row.TrangThai != signed) throw new Exception("Trạng thái bản ký bị thay đổi.");
}
Console.WriteLine("PASS: 5 trường hợp trạng thái đơn thuốc, giữ nguyên trạng thái bản ký.");
'@ | Set-Content -LiteralPath (Join-Path $checkRoot 'Program.cs') -Encoding UTF8
& dotnet run --project (Join-Path $checkRoot 'Check.csproj') --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Kiểm tra trạng thái đơn thuốc thất bại.' }
if ($KiemTraSql) {
@'
using ET;
using DAL;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Text.RegularExpressions;
var root = args[0];
using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "RangHamMat.Wpf/appsettings.json")));
var connectionString = new SqlConnectionStringBuilder(settings.RootElement.GetProperty("ConnectionStrings").GetProperty("RangHamMat").GetString()) { ConnectTimeout = 5 };
using var db = new SqlConnection(connectionString.ConnectionString);
db.Open();
var code = File.ReadAllText(Path.Combine(root, "DAL/DAL_KHAMDIEUTRI.PhanTrang.cs"));
var source = Regex.Match(code, "LayTrangLichSuDon[^\\n]+?\"\"\"([\\s\\S]*?)\"\"\"").Groups[1].Value;
if (string.IsNullOrWhiteSpace(source)) throw new Exception("Không tìm thấy truy vấn lịch sử.");
var sql = "WITH patients AS (SELECT * FROM dbo.BENH_NHAN) " + source;
object Params(string status) => new { LaAdmin = true, doctor = false, MaNV = "", patient = (string)null, visit = (string)null, status, term = "", search = "%" };
var all = db.Query<DongLichSuDon>(sql, Params(null)).ToArray();
foreach (var status in new[] { "Đã xác nhận", "Đã giao thuốc", "Ngừng áp dụng" }) {
    var actual = db.Query<DongLichSuDon>(sql, Params(status)).Select(r => r.MaDT).OrderBy(x => x).ToArray();
    var expected = all.Where(r => r.TrangThaiHienThi == status).Select(r => r.MaDT).OrderBy(x => x).ToArray();
    if (!actual.SequenceEqual(expected)) throw new Exception("Bộ lọc không khớp trạng thái: " + status);
}
Console.WriteLine($"PASS: SQL lịch sử và 3 bộ lọc trên {all.Length} đơn (chỉ đọc).");
'@ | Set-Content -LiteralPath (Join-Path $checkRoot 'Program.cs') -Encoding UTF8
    & dotnet run --project (Join-Path $checkRoot 'Check.csproj') --verbosity quiet -- $projectRoot
    if ($LASTEXITCODE -ne 0) { throw 'Kiểm tra SQL đơn thuốc thất bại.' }
}
