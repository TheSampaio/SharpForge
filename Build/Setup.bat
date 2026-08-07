@echo off
setlocal

title Sharp Forge Runtime Setup
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Setup.ps1"

if errorlevel 1 (
    echo.
    echo Setup failed. Review the error above and try again.
    pause
    exit /b 1
)

echo.
echo Setup completed successfully. Build Sharp Forge to copy the runtime to the output directory.
pause
