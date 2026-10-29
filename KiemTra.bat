@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\KiemTraBanGiao.ps1" %*
exit /b %errorlevel%
