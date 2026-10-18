@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Switch-ModVersion.ps1"
if errorlevel 1 echo Switching failed. See the message above.
echo.
pause
