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
    if($tableCount -ne 52 -or !$sql.Contains('RHM.ClinicalWorkflow.Version')){throw 'SQL chính phải có52 bảng/version2.'}
    $settings=Get-Content -LiteralPath 'RangHamMat.Wpf/appsettings.json' -Raw|ConvertFrom-Json
    if([string]::IsNullOrWhiteSpace($settings.ConnectionStrings.RangHamMat)){throw 'Thiếu cấu hình kết nối.'}
    Write-Output 'PASS: bốn project, Views XAML/code-behind, SQL52 bảng/version2 và cấu hình.'
    if(!$ChiCauTruc){
        foreach($configuration in @('Debug','Release')){
            & dotnet build 'BenhVienRangHamMat.sln' -c $configuration
            if($LASTEXITCODE -ne 0){throw "Build $configuration thất bại."}
        }
    }
    if($KiemTraSql){
        if(!(Get-Command sqlcmd -ErrorAction SilentlyContinue)){throw 'Cần cài sqlcmd hoặc chạy Verify trong SSMS.'}
        & sqlcmd -S $MayChu -d $Database -E -C -b -f65001 -i 'Database/Migrations/20261024_03_Verify_ClinicalWorkflow.sql'
        if($LASTEXITCODE -ne 0){throw 'Database chưa khớp schema quy trình khám2.'}
        & sqlcmd -S $MayChu -d $Database -E -C -b -f65001 -Q "SET NOCOUNT ON; IF (SELECT COUNT(*) FROM sys.triggers WHERE name IN(N'TR_CW_MauKH_BaoVe',N'TR_CW_CauHinh_BaoVe',N'TR_CW_BanGiao_BaoVe') AND is_disabled=0 AND OBJECT_DEFINITION(object_id) LIKE N'%QUẢN TRỊ%')<>3 THROW 52130,N'Chạy migration20261028_05_AdminFullAccess để đồng bộ quyền Admin.',1;"
        if($LASTEXITCODE -ne 0){throw 'Database chưa đồng bộ quyền Admin; chạy migration05.'}
        Write-Output 'PASS: database khớp schema2; kiểm tra SQL chỉ đọc.'
    }
} finally {Pop-Location}
