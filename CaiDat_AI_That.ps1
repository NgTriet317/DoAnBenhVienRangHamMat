param([Parameter(Mandatory=$true)][string]$ProjectRoot)
$ErrorActionPreference='Stop'
$source=Split-Path -Parent $MyInvocation.MyCommand.Path
$project=Join-Path $ProjectRoot 'RangHamMat.Kiosk.Api\RangHamMat.Kiosk.Api.csproj'
if(-not (Test-Path $project)){throw "Khong tim thay API Kiosk cu tai: $project. Hay cai Kiosk MVP truoc."}
$files=@(
    'RangHamMat.Kiosk.Api\Program.cs',
    'RangHamMat.Kiosk.Api\AiDialogueEngine.cs',
    'RangHamMat.Kiosk.Api\wwwroot\index.html',
    'Chay_AI_That.ps1'
)
foreach($relative in $files){
    $src=Join-Path $source $relative
    if(-not (Test-Path $src)){throw "Bo cai thieu file: $relative"}
}
$targetProgram=Join-Path $ProjectRoot 'RangHamMat.Kiosk.Api\Program.cs'
$original=Get-Content -Raw -Encoding UTF8 $targetProgram
if($original -notmatch 'AI_PHIEN_TIEP_NHAN' -or $original -notmatch '/api/tra-loi'){
    throw 'Program.cs khong giong Kiosk API da cai; dung cai de khong ghi de code khac.'
}
$backup=Join-Path $ProjectRoot ('Backup_Kiosk_ChatAI_'+(Get-Date -Format 'yyyyMMdd_HHmmss'))
New-Item -Path $backup -ItemType Directory -Force | Out-Null
foreach($relative in $files){
    $src=Join-Path $source $relative
    $dest=Join-Path $ProjectRoot $relative
    if(Test-Path $dest){
        $backupFile=Join-Path $backup $relative
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backupFile) | Out-Null
        Copy-Item $dest $backupFile -Force
    }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
    Copy-Item $src $dest -Force
    Write-Host "OK: $relative"
}
Write-Host "DA CAI XONG. Backup: $backup" -ForegroundColor Green
Write-Host 'Luu y: Chay them SQL/14_AI_ChatThat.sql trong SSMS. Khong can sua WPF hoac SQL V2.2.'
