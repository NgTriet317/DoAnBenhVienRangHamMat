param(
    [string]$SqlServer='.',
    [string]$Database='QLBenhVienRangHamMat',
    [ValidateSet('groq','ollama')][string]$Provider='groq',
    [string]$Model=''
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$project=Join-Path $root 'RangHamMat.Kiosk.Api\RangHamMat.Kiosk.Api.csproj'
if(-not (Test-Path $project)) { throw 'Chay Chay_AI_That.ps1 tai thu muc goc du an sau khi cai.' }
$env:RHM_CONNECTION_STRING="Server=$SqlServer;Database=$Database;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
$env:RHM_AI_PROVIDER=$Provider
if(-not $Model){ $Model=if($Provider -eq 'groq'){'openai/gpt-oss-20b'}else{'qwen2.5:7b'} }
$env:RHM_AI_MODEL=$Model
if($Provider -eq 'groq') {
    if(-not $env:RHM_AI_API_KEY -and -not $env:GROQ_API_KEY){
        Write-Host 'Nhap Groq API key (KHONG luu vao file/code):'
        $secret=Read-Host 'GROQ_API_KEY' -AsSecureString
        $ptr=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
        try{ $env:RHM_AI_API_KEY=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
        finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)}
    }
    if(-not $env:RHM_AI_API_KEY -and -not $env:GROQ_API_KEY){throw 'Chua co API key Groq.'}
    Write-Host 'Che do cloud: ban khai duoc gui toi Groq de AI xu ly. CHI SU DUNG DU LIEU GIA.' -ForegroundColor Yellow
}else{
    Write-Host 'Che do Ollama: hay dam bao ollama serve va model da pull.' -ForegroundColor Yellow
}
Write-Host "Kiosk AI that: http://127.0.0.1:5188 - Provider: $Provider - Model: $Model"
Write-Host 'Kiem tra http://127.0.0.1:5188/health/ai va /health/db; Ctrl+C de dung.'
& dotnet run --project $project
