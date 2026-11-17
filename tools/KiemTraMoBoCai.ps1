param([Parameter(Mandatory = $true)][string]$ApplicationFolder)

# Mo exe tu ban MSI giai nen, kiem man hinh SQL, dong bang nut X; khong bam Luu/dang nhap.
$ErrorActionPreference = 'Stop'
$applicationRoot = (Resolve-Path -LiteralPath $ApplicationFolder).Path
$exePath = Join-Path $applicationRoot 'RangHamMat.Wpf.exe'
$confirmed = [Microsoft.Win32.Registry]::GetValue('HKEY_CURRENT_USER\Software\RangHamMat\CaiDat', 'CanCauHinhDatabase', $null)
if ($confirmed -is [int] -and $confirmed -eq 0) {
    throw 'Tai khoan nay da xac nhan SQL. Khong tu doi registry san pham de chay kiem thu.'
}
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName = $exePath
$startInfo.WorkingDirectory = $applicationRoot
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.WindowStyle = 'Hidden'
$emptyRuntime = Join-Path $applicationRoot 'NoSystemDotnet'
[void][IO.Directory]::CreateDirectory($emptyRuntime)
$startInfo.EnvironmentVariables['PATH'] = [Environment]::GetFolderPath('System')
$startInfo.EnvironmentVariables['DOTNET_ROOT'] = $emptyRuntime
$startInfo.EnvironmentVariables['DOTNET_ROOT_X64'] = $emptyRuntime
$startInfo.EnvironmentVariables['DOTNET_MULTILEVEL_LOOKUP'] = '0'
$child = New-Object System.Diagnostics.Process
$child.StartInfo = $startInfo
$started = $false
try {
    if (-not $child.Start()) { throw 'Khong mo duoc exe trong bo cai.' }
    $started = $true
    $processCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $child.Id)
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    $window = $null
    while ([DateTime]::UtcNow -lt $deadline -and -not $child.HasExited) {
        $candidate = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, $processCondition)
        if ($candidate -and $candidate.Current.Name -like '*SQL Server') { $window = $candidate; break }
        Start-Sleep -Milliseconds 200
    }
    if (-not $window) { throw 'Ung dung khong hien man hinh cau hinh SQL Server.' }
    Start-Sleep -Milliseconds 500
    if ($child.HasExited -or -not $window.Current.IsEnabled) { throw 'Ung dung da dong hoac hien hop thoai loi khi nap cau hinh.' }
    $saveCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'btnLuu')
    $save = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $saveCondition)
    if (-not $save -or -not $save.Current.IsEnabled) { throw 'Man hinh SQL chua nap thanh cong.' }
    Write-Output 'PASS: Exe trong MSI mo duoc man hinh SQL Server va nut Luu, khong bao thieu DAL/ConfigurationManager.'
    $window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()
    if (-not $child.WaitForExit(5000) -or $child.ExitCode -ne 0) { throw 'Ung dung khong thoat binh thuong khi huy cau hinh.' }
    Write-Output 'PASS: Dong cau hinh thoat sach, khong luu profile hay ket noi SQL.'
}
finally {
    if ($started -and -not $child.HasExited) { $child.Kill(); [void]$child.WaitForExit(5000) }
    $child.Dispose()
}
