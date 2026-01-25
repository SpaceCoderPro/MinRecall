# How to Run MinRecall UI with Debug Output

## Quick Start - See What's Happening

### Windows Users (Run from Command Prompt)

1. Open **Command Prompt** (cmd.exe) - **NOT PowerShell**

2. Navigate to the build output:
   ```cmd
   cd C:\path\to\MinRecall\src\MinRecall.UI\bin\Release\net8.0-windows10.0.19041.0
   ```

3. Run the executable:
   ```cmd
   MinRecall.UI.exe
   ```

4. **Watch the console!** You'll see detailed output showing exactly what's happening:

```
==========================================================
MinRecall Application Starting
Time: 2024-01-25 08:24:15
OS: Microsoft Windows NT 10.0.19041.0
.NET Runtime: 8.0.0
==========================================================

[DEBUG] Building Avalonia app...
[DEBUG] Configuring Avalonia AppBuilder...
[DEBUG] Avalonia AppBuilder configured successfully
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
[DEBUG] Creating SearchViewModel...
[DEBUG] Creating HeatmapViewModel...
[DEBUG] Creating ActivityViewModel...
[DEBUG] Creating SettingsViewModel...
[DEBUG] All ViewModels initialized successfully
[DEBUG] MainViewModel constructor completed
[DEBUG] MainViewModel created and set as DataContext
[DEBUG] Caching navigation controls...
[DEBUG] Navigation controls cached
[DEBUG] Navigating to timeline view...
[DEBUG] NavigateTo(timeline) starting...
[DEBUG] Calling _viewModel.NavigateTo(timeline)...
[DEBUG] _viewModel.NavigateTo(timeline) completed
[DEBUG] Creating view for timeline...
[DEBUG] View created for timeline
[DEBUG] NavigateTo(timeline) completed
[DEBUG] MainWindow constructor completed successfully
[DEBUG] MainWindow created and assigned successfully
[DEBUG] OnFrameworkInitializationCompleted completed successfully
```

## What You'll Learn

The debug output tells you **exactly** what's happening:

### ✅ Success Indicators
- `[DEBUG]` messages show progress through initialization
- Each ViewModel is created successfully
- Database is created and connected
- Window is shown

### ❌ Failure Indicators
If something fails, you'll see:
```
[ERROR] Failed to create DatabaseManager:
  Type: System.Data.SQLite.SQLiteException
  Message: unable to open database file
  StackTrace:
   at Microsoft.Data.Sqlite.SqliteConnection.Open()
   at MinRecall.Core.Database.DatabaseManager.EnsureDatabaseCreated()
   at MinRecall.Core.Database.DatabaseManager..ctor(String dbPath)
   at MinRecall.UI.Services.DatabaseService..ctor(String dbPath)
   ...
```

## Common Issues You Might See

### Issue 1: Database Permission Error
```
[ERROR] Failed to create DatabaseManager:
  Message: Access to path 'C:\Users\...\AppData\Roaming\MinRecall' is denied
```
**Solution:** Run as Administrator or check folder permissions

### Issue 2: Missing SQLite DLL
```
[ERROR] DatabaseManager constructor failed:
  Type: System.DllNotFoundException
  Message: Unable to load DLL 'e_sqlite3': The specified module could not be found
```
**Solution:** SQLite native library is missing - rebuild or copy from NuGet packages

### Issue 3: XAML Parsing Error
```
[ERROR] App.Initialize() failed:
  Type: Avalonia.Markup.Xaml.XamlLoadException
  Message: Unable to resolve type MainWindow from namespace MinRecall.UI.Views
```
**Solution:** XAML file has syntax errors - check Views/*.axaml files

### Issue 4: No Screenshots Showing
```
[DEBUG] Timeline data loading initiated
[DEBUG] TimelineViewModel constructor completed
(Window shows but is empty)
```
**This is NORMAL!** The database is created but empty. You need to run the CaptureService to capture screenshots first.

## Error Log Files

If the app crashes, full error details are automatically saved:

**Location:**
```
%LOCALAPPDATA%\MinRecall\Logs\error_YYYYMMDD_HHMMSS.log
```

**Example:**
```
C:\Users\YourName\AppData\Local\MinRecall\Logs\error_20240125_082415.log
```

**Contents:**
```
[2024-01-25 08:24:15] Fatal Error starting application

Exception Type: System.InvalidOperationException
Message: MainWindow failed to create

Stack Trace:
   at MinRecall.UI.App.OnFrameworkInitializationCompleted()
   at Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime.Start()
   ...
```

## Build from Source

If you need to rebuild:

```bash
# Clone repository
git clone https://github.com/yourusername/MinRecall.git
cd MinRecall

# Build Release configuration
dotnet build --configuration Release

# Output will be in:
# src/MinRecall.UI/bin/Release/net8.0-windows10.0.19041.0/
```

## What Was Fixed

This debug logging was added to fix the "RecallApp.exe launches but shows nothing" issue:

1. ✅ Changed `OutputType` from `WinExe` to `Exe` - console now visible
2. ✅ Added debug logging to every initialization step
3. ✅ Added error handling with full stack traces
4. ✅ Fixed compilation errors in ViewModels
5. ✅ Made errors non-fatal where possible (window shows even if data fails)
6. ✅ Error logs saved to files automatically

## Success!

You should now see:
- ✅ Console window with debug output
- ✅ Detailed initialization progress
- ✅ Clear error messages if something fails
- ✅ Window appears (even if empty because no screenshots captured yet)
- ✅ Error log files for troubleshooting

**No more silent crashes!** You'll always know what's happening.

## For Production

Once everything is working, you can hide the console by changing:
```xml
<!-- In MinRecall.UI.csproj -->
<OutputType>WinExe</OutputType>
```

The logging will still work (saved to files), but the console won't show.
