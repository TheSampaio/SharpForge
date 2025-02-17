ECHO OFF
TITLE Setup
CLS

:: Define variables
set "URL=https://github.com/xinntao/Real-ESRGAN/releases/download/v0.2.5.0/realesrgan-ncnn-vulkan-20220424-windows.zip"
set "ZIP_FILE=realesrgan.zip"
set "SUB_DIR=realesrgan"
set "DEBUG_PATH=bin\Debug\net8.0-windows\%SUB_DIR%"
set "RELEASE_PATH=bin\Release\net8.0-windows\%SUB_DIR%"

:: Navigate to project root
CD ../
CD SharpForge

:: Create necessary directories
MKDIR "%DEBUG_PATH%"
MKDIR "%RELEASE_PATH%"

:: Download and extract Real-ESRGAN in both Debug and Release
call :DownloadAndExtract "%DEBUG_PATH%"
call :DownloadAndExtract "%RELEASE_PATH%"

echo Setup complete!
PAUSE
exit /b 0

:: Function to download and extract
:DownloadAndExtract
set "TARGET_PATH=%~1"

:: Download Real-ESRGAN
echo Downloading Real-ESRGAN to %TARGET_PATH%...
curl -L -o "%TARGET_PATH%\%ZIP_FILE%" "%URL%"

:: Verify download
if not exist "%TARGET_PATH%\%ZIP_FILE%" (
    echo Download failed in %TARGET_PATH%!
    exit /b 1
)

:: Extract ZIP using PowerShell
echo Extracting files...
powershell -command "Expand-Archive -Path '%TARGET_PATH%\%ZIP_FILE%' -DestinationPath '%TARGET_PATH%' -Force"

:: Verify extraction
if exist "%TARGET_PATH%\*" (
    echo Extraction completed in %TARGET_PATH%!
) else (
    echo Extraction failed in %TARGET_PATH%!
    exit /b 1
)

:: Clean up ZIP file
del "%TARGET_PATH%\%ZIP_FILE%"
exit /b 0
