param([string]$MsiPath = (Join-Path $PSScriptRoot '../RHM/Release-CauHinhDatabase/RHM.msi'))

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
        $view.GetType().InvokeMember('Execute', 'InvokeMethod', $null, $view, $null)
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
        $view.GetType().InvokeMember('Close', 'InvokeMethod', $null, $view, $null)
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($view)
    }
}

function Assert-Package([bool]$Condition, [string]$Description) {
    if (-not $Condition) { throw $Description }
    Write-Output "PASS: $Description"
}

try {
    $registryRows = @(Read-MsiTable 'Registry' @('Registry', 'Root', 'Key', 'Name', 'Value', 'Component_'))
    $setupRows = @($registryRows | Where-Object { $_.Root -eq '1' -and $_.Key -eq 'Software\RangHamMat\CaiDat' })
    $flag = @($setupRows | Where-Object { $_.Name -eq 'CanCauHinhDatabase' })
    Assert-Package ($flag.Count -eq 1 -and $flag[0].Value -eq '#1') 'MSI dat DWORD CanCauHinhDatabase=1 trong HKCU'
    Assert-Package (@($setupRows | Where-Object { $_.Name -eq '-' -or $_.Name -eq '*' }).Count -gt 0) 'MSI xoa nhanh CaiDat khi go ung dung'

    $components = @(Read-MsiTable 'Component' @('Component', 'Attributes', 'Condition', 'KeyPath'))
    $flagComponent = @($components | Where-Object { $_.Component -eq $flag[0].Component_ })
    Assert-Package ($flagComponent.Count -eq 1 -and ([int]$flagComponent[0].Attributes -band 16) -eq 0 -and [string]::IsNullOrEmpty($flagComponent[0].Condition)) 'Co khong Permanent va khong bi gioi han bang dieu kien cai dat'

    $properties = @(Read-MsiTable 'Property' @('Property', 'Value'))
    $allUsersOption = @($properties | Where-Object { $_.Property -eq 'FolderForm_AllUsersVisible' })
    Assert-Package ($allUsersOption.Count -eq 1 -and $allUsersOption[0].Value -eq '0') 'Bo cai chi hien lua chon cai cho tai khoan Windows hien tai'

    $files = @(Read-MsiTable 'File' @('FileName', 'FileSize'))
    foreach ($requiredFile in @('RangHamMat.Wpf.exe', 'RangHamMat.Wpf.dll', 'RangHamMat.Kiosk.Api.dll', 'appsettings.json')) {
        $matches = @($files | Where-Object { ($_.FileName -split '\|')[-1] -eq $requiredFile -and [int]$_.FileSize -gt 0 })
        Assert-Package ($matches.Count -gt 0) "MSI chua $requiredFile"
    }
    Write-Output 'RESULT: 8 package checks passed; khong thuc hien cai/go ung dung that.'
}
finally {
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($database)
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer)
}
