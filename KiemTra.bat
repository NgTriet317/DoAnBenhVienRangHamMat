@echo off
cd /d "%~dp0"
dotnet build BenhVienRangHamMat.sln -c Release -m:1
if errorlevel 1 exit /b 1
dotnet run --project RangHamMat.Checks -c Release
if errorlevel 1 exit /b 1
echo Build and checks completed.
pause
