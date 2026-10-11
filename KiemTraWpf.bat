@echo off
cd /d "%~dp0"
dotnet run --project RangHamMat.WpfSmoke -c Release
if errorlevel 1 exit /b 1
pause
