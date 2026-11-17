param(
    [string]$MsiPath = (Join-Path $PSScriptRoot '../RHM/Release/RHM.msi'),
    [string]$PublishPath
)

# Chỉ đọc bảng MSI; không chạy cài đặt/gỡ cài đặt và không đổi registry của sản phẩm.
$ErrorActionPreference = 'Stop'
$msiAbsolutePath = (Resolve-Path -LiteralPath $MsiPath).Path
$installer = New-Object -ComObject WindowsInstaller.Installer
$database = $installer.GetType().InvokeMember('OpenDatabase', 'InvokeMethod', $null, $installer, @($msiAbsolutePath, 0))

function Read-MsiTable([string]$Table, [string[]]$Columns) {
    $columnSql = ($Columns | ForEach-Object { '`' + $_ + '`' }) -join ','
    $query = 'SELECT ' + $columnSql + ' FROM `' + $Table + '`'
    $view = $database.GetType().InvokeMember('OpenView', 'InvokeMethod', $null, $database, @($query))
    try {
        [void]$view.GetType().InvokeMember('Execute', 'InvokeMethod', $null, $view, $null)
        while ($record = $view.GetType().InvokeMember('Fetch', 'InvokeMethod', $null, $view, $null)) {
            $row = [ordered]@{}
            for ($columnIndex = 0; $columnIndex -lt $Columns.Count; $columnIndex++) {
                $row[$Columns[$columnIndex]] = $record.GetType().InvokeMember('StringData', 'GetProperty', $null, $record, @($columnIndex + 1))
            }
            [pscustomobject]$row
            [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($record)
        }
    }
    finally {
        [void]$view.GetType().InvokeMember('Close', 'InvokeMethod', $null, $view, $null)
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($view)
    }
}

function Assert-Package([bool]$Condition, [string]$Description, [switch]$Quiet) {
    if (-not $Condition) { throw $Description }
    $script:packageCheckCount++
    if (-not $Quiet) { Write-Output "PASS: $Description" }
}

$script:packageCheckCount = 0
try {
    $registryRows = @(Read-MsiTable 'Registry' @('Registry', 'Root', 'Key', 'Name', 'Value', 'Component_'))
    $setupRows = @($registryRows | Where-Object { $_.Root -eq '1' -and $_.Key -eq 'Software\RangHamMat\CaiDat' })
    $flag = @($setupRows | Where-Object { $_.Name -eq 'CanCauHinhDatabase' })
    Assert-Package ($flag.Count -eq 1 -and $flag[0].Value -eq '#1') 'MSI dat DWORD CanCauHinhDatabase=1 trong HKCU'
    Assert-Package (@($setupRows | Where-Object { $_.Name -eq '-' -or $_.Name -eq '*' }).Count -gt 0) 'MSI xoa nhanh CaiDat khi go ung dung'

    $components = @(Read-MsiTable 'Component' @('Component', 'Attributes', 'Condition', 'KeyPath', 'Directory_'))
    $flagComponent = @($components | Where-Object { $_.Component -eq $flag[0].Component_ })
    Assert-Package ($flagComponent.Count -eq 1 -and ([int]$flagComponent[0].Attributes -band 16) -eq 0 -and [string]::IsNullOrEmpty($flagComponent[0].Condition)) 'Co khong Permanent va khong bi gioi han bang dieu kien cai dat'

    $properties = @(Read-MsiTable 'Property' @('Property', 'Value'))
    $allUsersOption = @($properties | Where-Object { $_.Property -eq 'FolderForm_AllUsersVisible' })
    Assert-Package ($allUsersOption.Count -eq 1 -and $allUsersOption[0].Value -eq '0') 'Bo cai chi hien lua chon cai cho tai khoan Windows hien tai'

    $files = @(Read-MsiTable 'File' @('FileName', 'FileSize', 'Component_', 'Version'))
    $componentMap = @{}
    foreach ($component in $components) { $componentMap[$component.Component] = $component }
    $directoryMap = @{}
    foreach ($directory in @(Read-MsiTable 'Directory' @('Directory', 'Directory_Parent', 'DefaultDir'))) {
        $directoryMap[$directory.Directory] = $directory
    }
    $mainExe = @($files | Where-Object { ($_.FileName -split '\|')[-1] -eq 'RangHamMat.Wpf.exe' })
    Assert-Package ($mainExe.Count -eq 1) 'MSI chua dung mot executable WPF'
    $applicationDirectory = $componentMap[$mainExe[0].Component_].Directory_
    $installedFiles = @{}
    foreach ($file in $files) {
        $parts = @((($file.FileName -split '\|')[-1]))
        $directoryId = $componentMap[$file.Component_].Directory_
        $depth = 0
        while ($directoryId -ne $applicationDirectory) {
            if (-not $directoryId -or -not $directoryMap.ContainsKey($directoryId) -or ++$depth -gt 64) {
                throw "File nam ngoai thu muc ung dung: $($file.FileName)"
            }
            $directory = $directoryMap[$directoryId]
            $folderName = (($directory.DefaultDir -split ':')[0] -split '\|')[-1]
            if ($folderName -ne '.') { $parts = @($folderName) + $parts }
            $directoryId = $directory.Directory_Parent
        }
        $relativePath = $parts -join '/'
        Assert-Package (-not $installedFiles.ContainsKey($relativePath)) "Khong trung duong dan cai dat: $relativePath" -Quiet
        $installedFiles[$relativePath] = $file
    }
    foreach ($requiredFile in @(
        'RangHamMat.Wpf.exe', 'RangHamMat.Wpf.dll', 'RangHamMat.Wpf.deps.json', 'RangHamMat.Wpf.runtimeconfig.json',
        'DAL.dll', 'BUS.dll', 'ET.dll', 'Dapper.dll', 'System.Configuration.ConfigurationManager.dll',
        'Microsoft.Data.SqlClient.dll', 'WebView2Loader.dll', 'appsettings.json',
        'coreclr.dll', 'hostfxr.dll', 'System.Private.CoreLib.dll', 'wpfgfx_cor3.dll',
        'KioskServer/RangHamMat.Kiosk.Api.exe', 'KioskServer/coreclr.dll', 'KioskServer/hostfxr.dll',
        'KioskServer/RangHamMat.Kiosk.Api.dll', 'KioskServer/RangHamMat.Kiosk.Api.runtimeconfig.json',
        'KioskServer/DAL.dll', 'KioskServer/appsettings.json'
    )) {
        Assert-Package ($installedFiles.ContainsKey($requiredFile) -and [int]$installedFiles[$requiredFile].FileSize -gt 0) "MSI chua dung vi tri: $requiredFile"
    }
    Assert-Package (@($installedFiles.Keys | Where-Object { $_ -like '*AiSecrets.local.json' }).Count -eq 0) 'Khong dong goi file khoa AI cuc bo'
    foreach ($versionedFile in @('RangHamMat.Wpf.dll', 'KioskServer/RangHamMat.Kiosk.Api.dll')) {
        Assert-Package ([version]$installedFiles[$versionedFile].Version -ge [version]'1.0.1.0') "Version file duoc tang de nang cap: $versionedFile"
    }
    if ($PublishPath) {
        $publishRoot = (Resolve-Path -LiteralPath $PublishPath).Path.TrimEnd('\', '/')
        $publishFiles = @(Get-ChildItem -LiteralPath $publishRoot -Recurse -File)
        foreach ($publishFile in $publishFiles) {
            $relativePath = $publishFile.FullName.Substring($publishRoot.Length + 1).Replace('\', '/')
            Assert-Package ($installedFiles.ContainsKey($relativePath) -and [long]$installedFiles[$relativePath].FileSize -eq $publishFile.Length) "Khop file publish: $relativePath" -Quiet
        }
        Write-Output "PASS: MSI khop duong dan va kich thuoc cua $($publishFiles.Count) file publish."
    }
    Write-Output "RESULT: $script:packageCheckCount package checks passed; khong thuc hien cai/go ung dung that."
}
finally {
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($database)
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer)
}
