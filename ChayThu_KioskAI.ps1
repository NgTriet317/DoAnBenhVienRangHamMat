param(
 [string]$SqlServer='.',
 [string]$Database='QLBenhVienRangHamMat',
 [string]$OllamaModel=''
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$env:RHM_CONNECTION_STRING="Server=$SqlServer;Database=$Database;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
$env:RHM_OLLAMA_MODEL=$OllamaModel
$api=Join-Path $root 'RangHamMat.Kiosk.Api\RangHamMat.Kiosk.Api.csproj'
if(-not (Test-Path $api)) { throw 'Chay file trong thu muc goc du an sau khi cai dat.' }
Write-Host 'Kiosk AI: http://127.0.0.1:5188'
Write-Host 'Nhan Ctrl+C de dung API.'
& dotnet run --project $api
