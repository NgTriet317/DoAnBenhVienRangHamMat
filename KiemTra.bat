@echo off
rem Giu bien moi truong cua bo kiem trong pham vi script, khong doi shell dang goi.
setlocal
rem Chay bo kiem tu duong dan cua file bat; chuyen nguyen cac tham so nhu -ChiCauTruc hoac -KiemTraSql.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\KiemTraBanGiao.ps1" %*
rem Tra ma thoat cua PowerShell de Visual Studio/CI/nguoi goi nhan dung ket qua thanh cong hay that bai.
exit /b %errorlevel%
