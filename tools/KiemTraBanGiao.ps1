param(
    [switch]$ChiCauTruc,
    [switch]$KiemTraSql,
    [string]$MayChu='.',
    [string]$Database='QLBenhVienRangHamMat'
)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Push-Location -LiteralPath $projectRoot
try {
    $expected=@('BUS','DAL','ET','RangHamMat.Wpf')
    $actual=@(Get-ChildItem -LiteralPath $projectRoot -Recurse -Filter '*.csproj' -File |
        Where-Object {$_.FullName -notmatch '[\\/](bin|obj|\.git)[\\/]'} |
        ForEach-Object {$_.BaseName} | Sort-Object)
    if(($actual -join ',') -ne (($expected|Sort-Object) -join ',')){throw "Project phải đúng bốn tầng: $($actual -join ', ')"}
    $views=Join-Path $projectRoot 'RangHamMat.Wpf/Views'
    $extra=@(Get-ChildItem -LiteralPath $views -Recurse -File -Filter '*.cs'|Where-Object {$_.Name -notlike '*.xaml.cs'})
    if($extra.Count){throw "Views còn C# phụ trợ: $($extra.Name -join ', ')"}
    foreach($view in Get-ChildItem -LiteralPath $views -Filter '*.xaml' -File){
        [xml]$markup=Get-Content -LiteralPath $view.FullName -Raw
        if($markup.DocumentElement.GetAttribute('Class','http://schemas.microsoft.com/winfx/2006/xaml') -and
            !(Test-Path -LiteralPath ($view.FullName+'.cs'))){throw "Thiếu code-behind: $($view.Name)"}
    }
    $sql=Get-Content -LiteralPath 'Database/QLBenhVienRangHamMat.sql' -Raw
    $tableCount=[regex]::Matches($sql,'CREATE TABLE \[dbo\]\.\[\w+\]').Count
    if($tableCount -ne 53 -or !$sql.Contains('RHM.HospitalWorkflow.Version') -or !$sql.Contains('RHM.PrescriptionFlow.Version')){throw 'SQL chính phải có53 bảng/HospitalWorkflow3/PrescriptionFlow1.'}
    $settings=Get-Content -LiteralPath 'RangHamMat.Wpf/appsettings.json' -Raw|ConvertFrom-Json
    if([string]::IsNullOrWhiteSpace($settings.ConnectionStrings.RangHamMat)){throw 'Thiếu cấu hình kết nối.'}
    Write-Output 'PASS: bốn project, Views XAML/code-behind, SQL53 bảng/HospitalWorkflow3/PrescriptionFlow1 và cấu hình.'
    if(!$ChiCauTruc){
        foreach($configuration in @('Debug','Release')){
            & dotnet build 'BenhVienRangHamMat.sln' -c $configuration
            if($LASTEXITCODE -ne 0){throw "Build $configuration thất bại."}
        }
    }
    if($KiemTraSql){
        if(!(Get-Command sqlcmd -ErrorAction SilentlyContinue)){throw 'Cần sqlcmd để kiểm tra SQL Server.'}
        & sqlcmd -S $MayChu -d $Database -E -C -b -f65001 -Q "IF OBJECT_ID(N'dbo.usp_RHM_KiemTraSchema',N'P') IS NULL OR OBJECT_ID(N'dbo.usp_RHM_KiemTraKeThuoc',N'P') IS NULL THROW 52272,N'Chạy toàn bộ Database/QLBenhVienRangHamMat.sql để đồng bộ.',1; EXEC dbo.usp_RHM_KiemTraSchema; EXEC dbo.usp_RHM_KiemTraKeThuoc;"
        if($LASTEXITCODE -ne 0){throw 'Database chưa khớp QLBenhVienRangHamMat.sql hiện tại.'}
        Write-Output 'PASS: HospitalWorkflow3/PrescriptionFlow1, schema53 bảng và ràng buộc đúng; kiểm tra SQL chỉ đọc.'
    }
} finally {Pop-Location}
