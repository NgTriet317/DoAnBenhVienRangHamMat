param([Parameter(Mandatory = $true)][string]$ApplicationFolder)

# Kiểm đúng payload; không kết nối SQL hoặc thay cấu hình sản phẩm.
$ErrorActionPreference = 'Stop'
$applicationRoot = (Resolve-Path -LiteralPath $ApplicationFolder).Path.TrimEnd('\', '/')
$sourceRoot = Split-Path $PSScriptRoot -Parent
$files = @(Get-ChildItem -LiteralPath $applicationRoot -Recurse -File)
$allowedDlls = @('Dicom.Native.dll', 'KioskServer/Dicom.Native.dll')
if (@($files | Where-Object {
    $_.Extension -eq '.dll' -and $_.FullName.Substring($applicationRoot.Length + 1).Replace('\', '/') -notin $allowedDlls
}).Count -gt 0) {
    throw 'Ban phat hanh chua gon: van con DLL roi trong payload.'
}
foreach ($relativePath in $allowedDlls) {
    if ((Get-Item -LiteralPath (Join-Path $applicationRoot $relativePath)).Length -eq 0) {
        throw "Thieu codec DICOM mixed-mode: $relativePath"
    }
}
if ((Get-FileHash -LiteralPath (Join-Path $applicationRoot $allowedDlls[0])).Hash -ne
    (Get-FileHash -LiteralPath (Join-Path $applicationRoot $allowedDlls[1])).Hash) {
    throw 'Codec DICOM cua WPF va Kiosk khac nhau.'
}
foreach ($relativePath in @('RangHamMat.Wpf.exe', 'KioskServer/RangHamMat.Kiosk.Api.exe')) {
    $executable = Get-Item -LiteralPath (Join-Path $applicationRoot $relativePath)
    if ($executable.Length -lt 20MB) { throw "Executable chua kem runtime: $relativePath" }
}
$resources = @{
    'appsettings.json' = 'RangHamMat.Wpf/appsettings.json'
    'Database/QLBenhVienRangHamMat.sql' = 'Database/QLBenhVienRangHamMat.sql'
    'KioskServer/appsettings.json' = 'RangHamMat.Kiosk.Api/appsettings.json'
    'KioskServer/wwwroot/index.html' = 'RangHamMat.Kiosk.Api/wwwroot/index.html'
}
foreach ($resource in @(Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'Database/AnhXquang') -File)) {
    $folder = if ($resource.Extension -eq '.jpg') { 'AnhXquang' } else { 'NguonAnhXquang' }
    if ($resource.Extension -in @('.jpg', '.json', '.md')) {
        $resources["DuLieu/$folder/$($resource.Name)"] = "Database/AnhXquang/$($resource.Name)"
    }
}
foreach ($resource in @(Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'RangHamMat.Wpf/AI') -Recurse -File)) {
    if ($resource.Name -eq 'AiSecrets.local.json') { continue }
    if (($resource.Directory.Name -eq 'Prompts' -and $resource.Extension -eq '.md') -or
        ($resource.Directory.Name -eq 'Config' -and $resource.Extension -eq '.json')) {
        $relativePath = $resource.FullName.Substring((Join-Path $sourceRoot 'RangHamMat.Wpf').Length + 1).Replace('\', '/')
        $resources[$relativePath] = "RangHamMat.Wpf/$relativePath"
    }
}
foreach ($relativePath in $resources.Keys) {
    $publishedHash = (Get-FileHash -LiteralPath (Join-Path $applicationRoot $relativePath) -Algorithm SHA256).Hash
    $sourceHash = (Get-FileHash -LiteralPath (Join-Path $sourceRoot $resources[$relativePath]) -Algorithm SHA256).Hash
    if ($publishedHash -ne $sourceHash) { throw "Tai nguyen thieu/khac nguon: $relativePath" }
}
if (@($files | Where-Object { $_.Name -eq 'AiSecrets.local.json' -or $_.Extension -eq '.pdb' }).Count -gt 0) {
    throw 'Ban phat hanh con khoa AI cuc bo hoac PDB roi.'
}
if ($files.Count -gt 50) { throw "Payload con qua nhieu file: $($files.Count)" }
Write-Output "PASS: $($resources.Count) tai nguyen SQL/cau hinh/AI/anh/wwwroot khop SHA256 nguon."
Write-Output "PASS: Hai executable kem runtime; chi giu codec DICOM mixed-mode, khong co PDB roi/khoa AI."
Write-Output "RESULT: $($files.Count) file, $(@(Get-ChildItem -LiteralPath $applicationRoot -Directory).Count) thu muc goc, $([math]::Round(($files | Measure-Object Length -Sum).Sum / 1MB, 2)) MiB."
