<#
.SYNOPSIS
Nhập ảnh tham khảo có nguồn cho đúng bốn lượt khám giả lập đã có dịch vụ chụp.
.DESCRIPTION
Mặc định chỉ kiểm tra tệp và liên kết. -ApDung sao chép tệp rồi nhập metadata
và nhật ký trong một giao dịch Serializable; không sửa lượt khám/chứng từ.
Chạy lại không thêm bản sao. Không tắt trigger hoặc giả ngày chụp.
#>
param(
    [string]$MayChu='.',
    [string]$Database='QLBenhVienRangHamMat',
    [string]$ThuMucKho,
    [switch]$ApDung
)
$ErrorActionPreference='Stop'
if($Database -notmatch '^(QLBenhVienRangHamMat|RHM_Hospital_Test_[A-Za-z0-9_]+)$'){throw 'Database ngoài phạm vi đồ án/kiểm thử.'}
$root=Split-Path $PSScriptRoot -Parent
$source=Join-Path $root 'Database/AnhXquang'
$manifest=Get-Content -LiteralPath (Join-Path $source 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if(@($manifest.Images).Count -ne 4){throw 'Bộ ảnh phải có đúng bốn liên kết đã đối chiếu.'}
if(!$ThuMucKho){$ThuMucKho=Join-Path $root 'PhatHanh/DuLieu/AnhXquang'}
$store=[IO.Path]::GetFullPath($ThuMucKho)
for($directory=[IO.DirectoryInfo]::new($store);$null -ne $directory;$directory=$directory.Parent){
    if($directory.Exists -and ($directory.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Kho không được đi qua thư mục liên kết.'}
}
$ids=@{};$visits=@{}
foreach($image in $manifest.Images){
    if($image.MaAnh -notmatch '^[0-9a-f]{32}$' -or $image.TenTepLuu -cne ($image.MaAnh+'.jpg')){throw 'Khóa tệp ảnh không hợp lệ.'}
    if($ids.ContainsKey($image.MaAnh) -or $visits.ContainsKey($image.MaLS)){throw 'Liên kết ảnh bị trùng trong manifest.'}
    $ids[$image.MaAnh]=$true;$visits[$image.MaLS]=$true
    $file=Join-Path $source $image.TenTepLuu
    if((Get-Item -LiteralPath $file).Length -ne $image.KichThuocBytes -or (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -cne $image.Sha256){throw ('Tệp nguồn sai hash/dung lượng: '+$image.MaLS)}
    if($image.GhiChu.Length -gt 500 -or $image.GhiChu -notlike '*tham khảo*' -or $image.MaNVUpload -cne 'NV001'){throw 'Thiếu nguồn gốc/người nhập ảnh tham khảo.'}
    $target=Join-Path $store $image.TenTepLuu
    if(Test-Path -LiteralPath $target){
        if((Get-Item -LiteralPath $target).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Tệp trong kho là liên kết.'}
        if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -cne $image.Sha256){throw ('Tệp trong kho khác nội dung; không ghi đè: '+$target)}
    }
}
Add-Type -AssemblyName System.Data
$builder=[System.Data.SqlClient.SqlConnectionStringBuilder]::new()
$builder['Data Source']=$MayChu;$builder['Initial Catalog']=$Database;$builder['Integrated Security']=$true
$builder['Encrypt']=$false;$builder['TrustServerCertificate']=$true;$builder['Connect Timeout']=5
$db=[System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
$transaction=$null;$created=[Collections.Generic.List[string]]::new();$commitAttempted=$false
function Invoke-Scalar([string]$sql,$image){
    $command=$db.CreateCommand();$command.Transaction=$transaction;$command.CommandTimeout=30;$command.CommandText=$sql
    try{
        if($null -ne $image){
            foreach($name in @('MaAnh','MaLS','MaBN','MaDVThucHien','MaDV','MaDanhMucTinhTrang','MaNVUpload','TenTepGoc','TenTepLuu','MimeType','Sha256','GhiChu')){
                $parameter=$command.Parameters.Add('@'+$name,[Data.SqlDbType]::NVarChar,1000);$parameter.Value=[string]$image.$name
            }
            $parameter=$command.Parameters.Add('@KichThuocBytes',[Data.SqlDbType]::BigInt);$parameter.Value=[long]$image.KichThuocBytes
        }
        return $command.ExecuteScalar()
    }finally{$command.Dispose()}
}
try{
    $db.Open();$transaction=$db.BeginTransaction([Data.IsolationLevel]::Serializable)
    $lock=Invoke-Scalar "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'RHM:Xray:Storage',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; SELECT @r;" $null
    if($lock -lt 0){throw 'Kho ảnh đang bận; chưa nhập dữ liệu.'}
    $pending=@()
    foreach($image in $manifest.Images){
        $valid=Invoke-Scalar @'
SELECT COUNT(*) FROM dbo.DICH_VU_THUC_HIEN s WITH(UPDLOCK,HOLDLOCK)
JOIN dbo.DICH_VU d ON d.MaDV=s.MaDV
JOIN dbo.LICH_SU_KHAM_BENH l WITH(UPDLOCK,HOLDLOCK) ON l.MaLS=s.MaLS
JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu
JOIN dbo.HO_SO_BENH_AN h ON h.MaHS=p.MaHS
WHERE s.MaDVThucHien=@MaDVThucHien AND s.MaLS=@MaLS AND s.MaDV=@MaDV
 AND h.MaBN=@MaBN AND d.MaCK=N'CK004' AND l.TrangThai=N'Đã xác nhận'
 AND EXISTS(SELECT 1 FROM dbo.TINH_TRANG_BENH t WHERE t.MaLS=l.MaLS AND t.MaDMTT=@MaDanhMucTinhTrang)
 AND EXISTS(SELECT 1 FROM dbo.NHAN_VIEN n JOIN dbo.TAI_KHOAN k ON k.MaNV=n.MaNV WHERE n.MaNV=@MaNVUpload AND k.MaTK=N'TK001');
'@ $image
        if($valid -ne 1){throw ('Bệnh nhân, tình trạng hoặc dịch vụ không khớp bộ dữ liệu đã đối chiếu: '+$image.MaLS)}
        $exists=Invoke-Scalar 'SELECT COUNT(*) FROM dbo.ANH_XQUANG_LAN_KHAM WITH(UPDLOCK,HOLDLOCK) WHERE MaAnh=@MaAnh OR DuongDanTuongDoi=@TenTepLuu;' $image
        if($exists -gt 0){
            $same=Invoke-Scalar @'
SELECT COUNT(*) FROM dbo.ANH_XQUANG_LAN_KHAM
WHERE MaAnh=@MaAnh AND MaLS=@MaLS AND MaDVThucHien=@MaDVThucHien
 AND TenTepLuu=@TenTepLuu AND DuongDanTuongDoi=@TenTepLuu
 AND Sha256=@Sha256 AND KichThuocBytes=@KichThuocBytes AND MimeType=@MimeType;
'@ $image
            if($exists -ne 1 -or $same -ne 1){throw ('Metadata đã có nhưng khác nội dung; không ghi đè: '+$image.MaLS)}
            Write-Output ($image.MaLS+': đã có metadata phù hợp, giữ nguyên trạng thái/ghi chú.')
        }else{
            $other=Invoke-Scalar "SELECT COUNT(*) FROM dbo.ANH_XQUANG_LAN_KHAM WHERE MaLS=@MaLS AND TrangThai=N'Đang sử dụng';" $image
            if($other -gt 0){throw ('Lượt đã có ảnh khác; không tự gắn ảnh tham khảo: '+$image.MaLS)}
            $pending+=,$image
            Write-Output ($image.MaLS+': liên kết hợp lệ, sẽ thêm ảnh tham khảo.')
        }
    }
    if(!$ApDung){$transaction.Rollback();Write-Output ('Chỉ kiểm tra: '+$pending.Count+' ảnh cần bổ sung; không ghi SQL/tệp.');return}
    New-Item -ItemType Directory -Path $store -Force | Out-Null
    foreach($image in $manifest.Images){
        $target=Join-Path $store $image.TenTepLuu
        if(!(Test-Path -LiteralPath $target)){
            [IO.File]::Copy((Join-Path $source $image.TenTepLuu),$target,$false);$created.Add($target)
        }
        if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -cne $image.Sha256){throw 'Kiểm tra hash sau chép thất bại.'}
    }
    foreach($image in $pending){
        $null=Invoke-Scalar @'
INSERT dbo.ANH_XQUANG_LAN_KHAM(MaAnh,MaLS,MaDVThucHien,TenTepGoc,TenTepLuu,DuongDanTuongDoi,MimeType,KichThuocBytes,Sha256,GhiChu,MaNVUpload)
VALUES(@MaAnh,@MaLS,@MaDVThucHien,@TenTepGoc,@TenTepLuu,@TenTepLuu,@MimeType,@KichThuocBytes,@Sha256,@GhiChu,@MaNVUpload);
INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,ThoiGianUtc,MaSuKien,PhanHe,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuSau)
VALUES(CONVERT(nvarchar(50),NEWID()),N'TK001',SYSDATETIME(),SYSUTCDATETIME(),NEWID(),N'Ảnh X-quang',N'XR_REFERENCE_IMPORT',N'ANH_XQUANG_LAN_KHAM',@MaAnh,N'Thành công',@GhiChu);
SELECT 1;
'@ $image
    }
    $commitAttempted=$true;$transaction.Commit()
    Write-Output ('Đã thêm '+$pending.Count+' ảnh và nhật ký; kho: '+$store)
}catch{
    if($transaction -and !$commitAttempted){
        try{$transaction.Rollback()}catch{}
        foreach($file in $created){if(Test-Path -LiteralPath $file){Remove-Item -LiteralPath $file -Force}}
    }
    throw
}finally{
    if($transaction){$transaction.Dispose()};$db.Dispose()
}
