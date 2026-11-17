param(
    [Parameter(Mandatory = $true)][string]$RepositoryRoot,
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repository = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$app = Join-Path $repository 'RangHamMat.Wpf'
if (-not (Test-Path (Join-Path $app 'RangHamMat.Wpf.csproj'))) {
    throw "Thiếu RangHamMat.Wpf.csproj. RepositoryRoot không hợp lệ: $repository"
}
foreach ($project in @('DAL', 'BUS', 'ET')) {
    if (-not (Test-Path (Join-Path $repository "$project/$project.csproj"))) {
        throw "Không tìm thấy project $project."
    }
}

$files = @(
    'RangHamMat.Wpf/Models/HospitalModels.cs',
    'RangHamMat.Wpf/Repositories/IHospitalRepository.cs',
    'RangHamMat.Wpf/Repositories/MockHospitalRepository.cs',
    'RangHamMat.Wpf/Repositories/SqlHospitalRepository.cs',
    'RangHamMat.Wpf/Services/GroqAiService.cs',
    'RangHamMat.Wpf/Presentation/PhieuHuongDan.AiPrint.cs',
    'RangHamMat.Wpf/Views/TroLyAI.xaml',
    'RangHamMat.Wpf/Views/TroLyAI.xaml.cs',
    'RangHamMat.Wpf/ViewModels/ReceptionistViewModel.cs',
    'RangHamMat.Wpf/Views/ReceptionistView.xaml',
    'RangHamMat.Wpf/Views/ReceptionistView.xaml.cs',
    'RangHamMat.Wpf/AI/Prompts/LeTanKiosk.md',
    'RangHamMat.Wpf/AI/Config/ReceptionistSettings.json',
    'RangHamMat.Wpf/AI/AiIntakeReview.cs',
    'DAL/DAL_AI_LETAN.cs',
    'BUS/BUS_AI_LETAN.cs',
    'ET/ET_AI_LETAN.cs'
)
foreach ($file in $files) {
    $src = Join-Path $sourceRoot $file
    $dst = Join-Path $repository $file
    if (-not (Test-Path -LiteralPath $src)) { throw "Thiếu file: $src" }
    $dir = Split-Path -Parent $dst
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -Path $dir -ItemType Directory -Force | Out-Null }
    if ([IO.Path]::GetFullPath($src) -ne [IO.Path]::GetFullPath($dst)) {
        Copy-Item -LiteralPath $src -Destination $dst -Force
    }
}

# Phải thay đường dẫn AI cũ thành module mới: nút TroLyAI giữ nguyên permission.
$mainCs = Join-Path $app 'MainWindow.xaml.cs'
$source = [IO.File]::ReadAllText($mainCs)
if ($source.Contains('"TroLyAI" => new TroLyAI(),')) {
    $source = $source.Replace('"TroLyAI" => new TroLyAI(),', '"TroLyAI" => new ReceptionistView(),')
}
if (-not $source.Contains('"TroLyAI" => new ReceptionistView(),')) {
    throw 'Không tìm được điểm điều hướng TroLyAI. Cần kiểm tra MainWindow.xaml.cs của nhánh hiện tại.'
}
# Với máy đã cài bản Demo cũ: bỏ nhánh điều hướng và ô trùng.
$source = [regex]::Replace($source, '(?m)^\s*"ReceptionistAI"\s*=>\s*new ReceptionistView\(\),\r?\n', "`r`n")
[IO.File]::WriteAllText($mainCs, $source, [Text.UTF8Encoding]::new($false))

$mainXaml = Join-Path $app 'MainWindow.xaml'
$source = [IO.File]::ReadAllText($mainXaml)
$source = [regex]::Replace($source, '(?s)\s*<Button\s+Tag="ReceptionistAI".*?</Button>', '')
$source = $source.Replace('AutomationProperties.Name="Trợ lý AI tiếp nhận" ToolTip="Mở chatbot AI trong WPF"',
                          'AutomationProperties.Name="Lễ tân AI – Tạo phiếu khám" ToolTip="Tạo phiếu chính thức qua SQL Server"')
$source = $source.Replace('<TextBlock Text="Trợ lý AI tiếp nhận" TextAlignment="Center"',
                          '<TextBlock Text="Lễ tân AI – Tạo phiếu khám" TextAlignment="Center"')
[IO.File]::WriteAllText($mainXaml, $source, [Text.UTF8Encoding]::new($false))

# Xóa service QR của bản Demo cũ để không còn tùy chọn QR trong module.
$oldQrService = Join-Path $app 'Services/SlipPrintAndQrService.cs'
if (Test-Path -LiteralPath $oldQrService) { Remove-Item -LiteralPath $oldQrService -Force }

# Không cần thêm QRCoder. Chỉ gỡ package nếu không còn mã nguồn nào dùng.
$csproj = Join-Path $app 'RangHamMat.Wpf.csproj'
$code = [IO.File]::ReadAllText($csproj)
$usesQr = @(Get-ChildItem -Path $app -Filter '*.cs' -Recurse -File |
    Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' } |
    Select-String -Pattern '^\s*using\s+QRCoder\s*;' -Quiet) -contains $true
if (-not $usesQr) {
    $code = [regex]::Replace($code, '(?m)^\s*<PackageReference\s+Include="QRCoder"\s+Version="[^"]+"\s*/>\s*\r?\n', "`r`n")
    [IO.File]::WriteAllText($csproj, $code, [Text.UTF8Encoding]::new($false))
}

Write-Host 'Đã cài Lễ tân AI (SQL thật). Nút TroLyAI cũ nay mở ReceptionistView.' -ForegroundColor Green
Write-Host 'Mặc định UseMockRepository=false. Cần tài khoản WPF có quyền tiếp nhận và SQL đúng phiên bản.'
Write-Host 'Không thêm hay sửa endpoint kiosk trình duyệt /api/ai/intake/receipts.'
Write-Host 'Không commit GROQ_API_KEY vào repo.'
if (-not $SkipBuild) {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'Không tìm thấy .NET 8 SDK. Cài .NET 8 SDK / Visual Studio 2022 trước khi build.'
    }
    Push-Location $repository
    try {
        dotnet restore .\BenhVienRangHamMat.sln
        if ($LASTEXITCODE -ne 0) { throw 'dotnet restore thất bại.' }
        dotnet build .\RangHamMat.Wpf\RangHamMat.Wpf.csproj -c Debug --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'dotnet build thất bại.' }
    }
    finally { Pop-Location }
}
