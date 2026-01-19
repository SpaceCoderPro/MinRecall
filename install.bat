@echo off
REM MinRecall Installation Script

echo ========================================
echo MinRecall Installation
echo ========================================
echo.

REM Check for Administrator privileges
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running with Administrator privileges...
) else (
    echo ERROR: This script requires Administrator privileges.
    echo Please right-click and run as Administrator.
    pause
    exit /b 1
)

echo.
echo Step 1: Stopping existing services (if any)...
sc stop "MinRecall Capture Service" >nul 2>&1
sc stop "MinRecall Optimizer Service" >nul 2>&1
timeout /t 2 /nobreak >nul

echo Step 2: Removing old services (if any)...
sc delete "MinRecall Capture Service" >nul 2>&1
sc delete "MinRecall Optimizer Service" >nul 2>&1
timeout /t 1 /nobreak >nul

echo Step 3: Installing MinRecall Capture Service...
sc create "MinRecall Capture Service" binPath= "\"%CD%\src\MinRecall.CaptureService\bin\Release\net8.0-windows\MinRecall.CaptureService.exe\"" start= auto DisplayName= "MinRecall Capture Service"
if %errorLevel% == 0 (
    echo Successfully installed Capture Service
) else (
    echo Failed to install Capture Service
)

echo Step 4: Installing MinRecall Optimizer Service...
sc create "MinRecall Optimizer Service" binPath= "\"%CD%\src\MinRecall.Optimizer\bin\Release\net8.0-windows\MinRecall.Optimizer.exe\"" start= auto DisplayName= "MinRecall Optimizer Service"
if %errorLevel% == 0 (
    echo Successfully installed Optimizer Service
) else (
    echo Failed to install Optimizer Service
)

echo.
echo Step 5: Starting services...
sc start "MinRecall Capture Service"
sc start "MinRecall Optimizer Service"

echo.
echo Step 6: Creating desktop shortcut...
set ShortcutPath=%USERPROFILE%\Desktop\MinRecall.lnk
powershell -command "$s=(New-Object -COM WScript.Shell).CreateShortcut('%ShortcutPath%');$s.TargetPath='%CD%\src\MinRecall.UI\bin\Release\net8.0-windows\MinRecall.UI.exe';$s.Save()"

echo.
echo ========================================
echo Installation Complete!
echo ========================================
echo.
echo Services are now running:
echo   - MinRecall Capture Service
echo   - MinRecall Optimizer Service
echo.
echo You can start the UI from the desktop shortcut or:
echo %CD%\src\MinRecall.UI\bin\Release\net8.0-windows\MinRecall.UI.exe
echo.
echo Press any key to exit...
pause >nul
