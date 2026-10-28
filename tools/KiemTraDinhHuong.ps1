$ErrorActionPreference='Stop'
$view=Get-Content "$PSScriptRoot/../RangHamMat.Wpf/Views/KhamLamSang.xaml" -Raw
if($view.Contains('ConditionLocationEditor')){throw 'Form vẫn còn vị trí răng'}
if(!$view.Contains('ConditionRecommendation')){throw 'Chưa có đề xuất kế hoạch tự động'}
Write-Output 'PASS: form bỏ vị trí, có đề xuất kế hoạch.'
