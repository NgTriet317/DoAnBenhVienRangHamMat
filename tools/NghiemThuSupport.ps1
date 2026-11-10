# Helper cho bộ nghiệm thu, không ghi dữ liệu SQL hay sửa file nguồn.
function Get-P6Outcome {
 param([string]$Log,[int]$ExitCode,[ValidateSet('Summary','Lines','Marker')][string]$Mode='Summary',[int]$MinimumPassed=1,[string[]]$RequiredPatterns=@())
 $body=[IO.File]::ReadAllText($Log)
 $reasons=[Collections.Generic.List[string]]::new();$passedCount=$null;$failedCount=0
 if($ExitCode -ne 0){$reasons.Add("Child exit=$ExitCode")}
 if($body -match '(?m)^\s*(?:(?:powershell|powershell\.exe)\s*:\s*)?FAIL(?:[:\s]|$)'){$reasons.Add('Log chứa FAIL')}
 if($Mode -eq 'Summary'){
  $summaries=[regex]::Matches($body,'(?m)^RESULT:\s*(\d+)\s+passed\s*[,;]\s*(\d+)\s+failed\s*$')
  if(!$summaries.Count){$reasons.Add('Thiếu summary kiểm thử')}
  else {$last=$summaries[$summaries.Count-1];$passedCount=[int]$last.Groups[1].Value;foreach($summary in $summaries){$failedCount+=[int]$summary.Groups[2].Value};if($failedCount){$reasons.Add("Có$failedCount kiểm thất bại trong các summary")};if($passedCount -lt $MinimumPassed){$reasons.Add("Chỉ có$passedCount kiểm, cần ít nhất$MinimumPassed")}}
 } elseif($Mode -eq 'Lines'){
  $passedCount=[regex]::Matches($body,'(?m)^PASS(?:[:\s]|$)').Count
  if($passedCount -lt $MinimumPassed){$reasons.Add("Chỉ có$passedCount dòng PASS, cần ít nhất$MinimumPassed")}
 } elseif(!$RequiredPatterns.Count){$reasons.Add('Operation cần marker chứng minh kết quả')}
 foreach($pattern in $RequiredPatterns){if(![regex]::IsMatch($body,$pattern,[Text.RegularExpressions.RegexOptions]::Multiline)){$reasons.Add('Thiếu marker: '+$pattern)}}
 [pscustomobject]@{Status=if($reasons.Count){'FAIL'}else{'PASS'};Passed=$passedCount;Failed=$failedCount;Reason=$reasons -join '; '}
}
function Invoke-P6Tool {
 param([string]$Name,[string]$Tool,[string[]]$ToolArgs=@(),[string]$Directory,[string]$Mode='Summary',[int]$MinimumPassed=1,[string[]]$RequiredPatterns=@())
 if($Name -notmatch '^[A-Za-z0-9_-]+$'){throw 'Tên bước không hợp lệ.'}
 $log=Join-Path $Directory ($Name+'.log');if(Test-Path -LiteralPath $log){throw 'Không ghi đè log nghiệm thu.'}
 $started=[DateTime]::UtcNow;$oldPreference=$ErrorActionPreference
 try {$ErrorActionPreference='Continue';& powershell -NoProfile -ExecutionPolicy Bypass -File $Tool @ToolArgs *> $log;$childExit=$LASTEXITCODE}
 finally {$ErrorActionPreference=$oldPreference}
 $outcome=Get-P6Outcome -Log $log -ExitCode $childExit -Mode $Mode -MinimumPassed $MinimumPassed -RequiredPatterns $RequiredPatterns
 [pscustomobject]@{Name=$Name;Status=$outcome.Status;ExitCode=$childExit;Passed=$outcome.Passed;Failed=$outcome.Failed;Reason=$outcome.Reason;Log=$log;StartedUtc=$started;DurationSeconds=[Math]::Round(([DateTime]::UtcNow-$started).TotalSeconds,2)}
}
function Export-P6Baseline {
 param([string]$Root,[string]$Destination)
 $sha='1cafa7d2096a3e954d381e473520cba8bab4b9dd'
 $process=[Diagnostics.Process]::new();$process.StartInfo=[Diagnostics.ProcessStartInfo]::new()
 $process.StartInfo.FileName='git';$process.StartInfo.Arguments='cat-file blob '+$sha+':Database/QLBenhVienRangHamMat.sql'
 $process.StartInfo.WorkingDirectory=$Root;$process.StartInfo.UseShellExecute=$false;$process.StartInfo.CreateNoWindow=$true
 $process.StartInfo.RedirectStandardOutput=$true;$process.StartInfo.RedirectStandardError=$true
 $stream=[IO.FileStream]::new($Destination,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
 try {
  if(!$process.Start()){throw 'Không chạy được git đọc baseline.'}
  $errors=$process.StandardError.ReadToEndAsync();$process.StandardOutput.BaseStream.CopyTo($stream);$process.WaitForExit()
  if($process.ExitCode -ne 0){throw ('Không lấy được SQL gốc: '+$errors.Result)}
 } finally {$stream.Dispose();$process.Dispose()}
 $raw=[IO.File]::ReadAllBytes($Destination);$crlf=[Text.Encoding]::UTF8.GetString($raw).Replace("`r`n","`n").Replace("`n","`r`n")
 $hash=[Security.Cryptography.SHA256]::Create()
 try {$normalized=([BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes($crlf)))).Replace('-','')}
 finally {$hash.Dispose()}
 if($normalized -ne '1EEB1EF8232485C86B18700BB12A81149268D9B7E495E64F6E6E06541BA56BCB'){throw 'SQL baseline không khớp nguồn CRLF đã khóa trước P1.'}
 [pscustomobject]@{Commit=$sha;Path=$Destination;BlobSha256=(Get-FileHash -LiteralPath $Destination -Algorithm SHA256).Hash;CrlfSha256=$normalized;Bytes=$raw.Length}
}
function Get-P6SourceManifest {
 param([string]$Root)
 $files=@(foreach($folder in @('ET','DAL','BUS','RangHamMat.Wpf','Database','tools')){Get-ChildItem -LiteralPath (Join-Path $Root $folder) -File -Recurse | Where-Object {$_.FullName -notmatch '[\\/](obj|bin)[\\/]' -and $_.Name -match '\.(cs|xaml|csproj|json|sql|ps1|cs\.txt)$'}})
 $files+=Get-Item -LiteralPath (Join-Path $Root 'BenhVienRangHamMat.sln')
 @($files|Sort-Object FullName|ForEach-Object {[pscustomobject]@{Path=$_.FullName.Substring($Root.Length+1).Replace('\','/');Bytes=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
}
function Write-P6Json {
 param([string]$Path,[object]$Value)
 [IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
}
function Compare-P6StorageProof {
 param([string]$First,[string]$Second)
 $reasons=[Collections.Generic.List[string]]::new()
 try {$a=[IO.File]::ReadAllText($First)|ConvertFrom-Json;$b=[IO.File]::ReadAllText($Second)|ConvertFrom-Json}
 catch {return [pscustomobject]@{Status='FAIL';Reason='Không đọc được hai proof JSON';Scope='SharedStorageOnly'}}
 foreach($proof in @($a,$b)){
  if($proof.Protocol -ne 'RHM.Xray.SharedStorageProbe.v1' -or !$proof.Machine -or !$proof.SqlServer -or $proof.Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$'){$reasons.Add('Sai protocol/nguồn fixture/hostname')}
  if($proof.StorageRoot -notmatch '^\\\\[^\\]+\\[^\\]+' -or $proof.StorageRoot.StartsWith('\\?\') -or $proof.StorageRoot.StartsWith('\\.\')){$reasons.Add('Kho không phải UNC thông thường')}
  if($proof.MetadataSha256 -notmatch '^[0-9A-Fa-f]{64}$'){$reasons.Add('Thiếu fingerprint metadata')}
  if($proof.Images -isnot [int] -and $proof.Images -isnot [long]){$reasons.Add('Sai kiểu số ảnh')}
  if($proof.HiddenImages -isnot [int] -and $proof.HiddenImages -isnot [long]){$reasons.Add('Sai kiểu số ảnh ẩn')}
  if($proof.OpenedImages -isnot [int] -and $proof.OpenedImages -isnot [long]){$reasons.Add('Sai kiểu số ảnh đã mở')}
  if($proof.Images -le 0 -or $proof.HiddenImages -lt 1 -or $proof.OpenedImages -ne ($proof.Images-$proof.HiddenImages)){$reasons.Add('Chưa xác minh đủ ảnh đang dùng và ảnh ẩn')}
  if($proof.AllHashesVerified -isnot [bool] -or $proof.AllHashesVerified -ne $true -or $proof.UsedApplicationPermissions -isnot [bool] -or $proof.UsedApplicationPermissions -ne $true){$reasons.Add('Thiếu xác minh hash/quyền BUS')}
 }
 if([string]::Equals($a.Machine,$b.Machine,[StringComparison]::OrdinalIgnoreCase)){$reasons.Add('Hai proof cùng hostname; không phải hai máy')}
 foreach($field in @('SqlServer','Database','StorageRoot','MetadataSha256','Images','HiddenImages','OpenedImages')){
  if(![string]::Equals([string]$a.$field,[string]$b.$field,[StringComparison]::OrdinalIgnoreCase)){$reasons.Add('Hai proof khác '+$field)}
 }
 [pscustomobject]@{Status=if($reasons.Count){'FAIL'}else{'PASS_SHARED_STORAGE'};Reason=$reasons -join '; ';Scope='SharedStorageOnly';FirstMachine=$a.Machine;SecondMachine=$b.Machine;RemoteSqlRestoreAccepted=$false}
}
