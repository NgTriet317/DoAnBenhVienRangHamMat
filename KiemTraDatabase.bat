@echo off
cd /d "%~dp0"
dotnet run --project RangHamMat.PersistenceChecks -c Release -p:NuGetAudit=false
if errorlevel 1 exit /b 1
dotnet run --project RangHamMat.FinanceChecks -c Release -p:NuGetAudit=false
if errorlevel 1 exit /b 1
dotnet run --project RangHamMat.WpfSmoke -c Release -p:NuGetAudit=false
if errorlevel 1 exit /b 1
echo SQL and WPF checks passed. Test records were rolled back.
pause
