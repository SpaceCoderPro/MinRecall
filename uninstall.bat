@echo off
REM MinRecall Uninstallation Script

echo ========================================
echo MinRecall Uninstallation
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
echo WARNING: This will uninstall MinRecall.
echo Your data (screenshots and database) will be kept.
echo.
set /p confirm="Are you sure you want to uninstall? (y/N): "

if /i not "%confirm%"=="y" (
    echo Uninstallation cancelled.
    pause
    exit /b 0
)

echo.
echo Step 1: Stopping services...
sc stop "MinRecall Capture Service" >nul 2>&1
sc stop "MinRecall Optimizer Service" >nul 2>&1
timeout /t 2 /nobreak >nul

echo Step 2: Removing services...
sc delete "MinRecall Capture Service" >nul 2>&1
sc delete "MinRecall Optimizer Service" >nul 2>&1
timeout /t 1 /nobreak >nul

echo Step 3: Removing desktop shortcut...
if exist "%USERPROFILE%\Desktop\MinRecall.lnk" (
    del "%USERPROFILE%\Desktop\MinRecall.lnk"
    echo Removed desktop shortcut
)

echo.
echo ========================================
echo Uninstallation Complete!
echo ========================================
echo.
echo Services removed:
echo   - MinRecall Capture Service
echo   - MinRecall Optimizer Service
echo.
echo Your data is preserved at:
echo   %APPDATA%\MinRecall\
echo.
echo To completely remove MinRecall (including data), delete the folder above manually.
echo.
echo Press any key to exit...
pause >nul
