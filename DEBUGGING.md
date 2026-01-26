# MinRecall UI - Debugging Guide

## Problem: RecallApp.exe launches but shows nothing

This has been fixed by adding comprehensive debug logging throughout the application.

## How to See Debug Output

### Method 1: Run from Command Prompt (RECOMMENDED)

1. Open Command Prompt (cmd.exe) - **NOT PowerShell**
2. Navigate to the executable directory:
   ```cmd
   cd C:\path\to\MinRecall.UI\bin\Release\net8.0-windows10.0.19041.0
   ```
3. Run the executable:
   ```cmd
   MinRecall.UI.exe
   ```

You will now see detailed debug output like:
```
==========================================================
MinRecall Application Starting
Time: 2024-01-15 10:30:45
OS: Microsoft Windows NT 10.0.19041.0
.NET Runtime: 8.0.0
==========================================================

[DEBUG] Building Avalonia app...
[DEBUG] App.Initialize() starting...
[DEBUG] App.Initialize() completed successfully
[DEBUG] OnFrameworkInitializationCompleted starting...
[DEBUG] Creating MainWindow...
[DEBUG] MainWindow constructor starting...
[DEBUG] InitializeComponent completed
[DEBUG] Creating MainViewModel...
[DEBUG] MainViewModel constructor starting...
[DEBUG] InitializeViewModels starting...
[DEBUG] Creating TimelineViewModel...
[DEBUG] TimelineViewModel constructor starting...
[DEBUG] Getting DatabaseService.Instance...
[DEBUG] DatabaseService initializing with path: C:\Users\YourName\AppData\Roaming\MinRecall\minrecall.db
[DEBUG] DatabaseService constructor - creating DatabaseManager for: C:\Users\YourName\AppData\Roaming\MinRecall\minrecall.db
[DEBUG] DatabaseManager constructor - path: C:\Users\YourName\AppData\Roaming\MinRecall\minrecall.db
[DEBUG] Calling EnsureDatabaseCreated...
[DEBUG] EnsureDatabaseCreated starting...
[DEBUG] Database directory: C:\Users\YourName\AppData\Roaming\MinRecall
[DEBUG] Creating directory: C:\Users\YourName\AppData\Roaming\MinRecall
[DEBUG] Directory created successfully
[DEBUG] Opening SQLite connection to: C:\Users\YourName\AppData\Roaming\MinRecall\minrecall.db
[DEBUG] SQLite connection opened successfully
[DEBUG] Executing database schema creation SQL...
[DEBUG] Database schema created/verified successfully
[DEBUG] DatabaseManager created successfully
[DEBUG] DatabaseService.Instance obtained, path: C:\Users\YourName\AppData\Roaming\MinRecall\minrecall.db
[DEBUG] Loading timeline data...
[DEBUG] Timeline data loading initiated
[DEBUG] TimelineViewModel constructor completed
...
```

### Method 2: Check Error Log Files

If the app crashes, error details are automatically saved to:
```
%LOCALAPPDATA%\MinRecall\Logs\error_YYYYMMDD_HHMMSS.log
```

Example path:
```
C:\Users\YourName\AppData\Local\MinRecall\Logs\error_20240115_103045.log
```

### Method 3: Visual Studio Debugger

1. Open the solution in Visual Studio
2. Set MinRecall.UI as startup project
3. Press F5 to run with debugger attached
4. Check the Output window for debug messages

## Common Issues and Solutions

### Issue 1: Window doesn't show, crashes silently

**Symptom:** App exits immediately without showing window or error

**Debug:** Look for error messages in console or log file

**Common causes:**
- Database initialization failure
- Missing dependencies
- ViewModel constructor exception
- XAML parsing error

### Issue 2: Window shows but is empty/blank

**Symptom:** Window appears but content doesn't load

**Debug:** Console will show where the failure occurred:
```
[DEBUG] Creating view for timeline...
[ERROR] Failed to create view for timeline:
  Message: Could not find control 'TimelineList'
```

**Solution:** Check XAML control names and bindings

### Issue 3: Database errors

**Symptom:** Console shows database-related errors

**Common messages:**
```
[ERROR] Failed to create DatabaseManager:
  Type: System.Data.SQLite.SQLiteException
  Message: unable to open database file
```

**Solution:**
- Check if directory exists and has write permissions
- Verify SQLite native libraries are present
- Check database file path in console output

### Issue 4: Missing screenshots

**Symptom:** App runs but shows "0 screenshots"

**Cause:** CaptureService hasn't run yet to capture screenshots

**Solution:** 
1. Run MinRecall.CaptureService.exe to start capturing screenshots
2. Wait a few minutes for screenshots to be captured
3. Refresh the UI

## Changes Made for Better Debugging

1. **Changed OutputType from WinExe to Exe**
   - This shows a console window for debug output
   - Can be changed back to WinExe for production builds

2. **Added debug logging at every initialization step:**
   - Program.cs: Application startup
   - App.axaml.cs: Avalonia framework initialization
   - MainWindow.axaml.cs: Window creation
   - MainViewModel.cs: ViewModel initialization
   - DatabaseService.cs: Database connection
   - DatabaseManager.cs: Database schema creation

3. **Added error handling at every level:**
   - Try-catch blocks around all initialization code
   - Graceful degradation (show window even if data fails to load)
   - File-based error logging
   - Console error output with stack traces

4. **Fixed broken ViewModels:**
   - ActivityViewModel: Removed extra closing brace
   - HeatmapViewModel: Fixed malformed array syntax
   - HeatmapViewModel: Fixed undefined 'color' variable

## Next Steps

1. Run `MinRecall.UI.exe` from command prompt
2. Look at the console output to see where it's failing
3. Check the error log files if it crashes
4. Report the specific error message you see

The app should now show exactly what's going wrong instead of failing silently!
