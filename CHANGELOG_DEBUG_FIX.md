# Debug Fix - RecallApp.exe Silent Crash Issue

## Problem
RecallApp.exe was launching but showing nothing - the app was crashing silently without any visible error messages or diagnostic information.

## Root Causes

1. **OutputType was WinExe** - Windows GUI app that doesn't show console output
2. **No debug logging** - No way to see what was happening during initialization
3. **No error handling** - Exceptions in ViewModels caused silent crashes
4. **Code bugs** - ActivityViewModel and HeatmapViewModel had syntax errors

## Changes Made

### 1. Changed OutputType to Enable Console (MinRecall.UI.csproj)
```xml
<OutputType>Exe</OutputType>  <!-- Changed from WinExe -->
```
**Effect:** Now shows console window with debug output when running the app

### 2. Enhanced Program.cs Logging
- Added startup banner with system info
- Added debug logging for Avalonia app initialization
- Enhanced error logging with timestamps
- Added "Press any key to exit" for error visibility

### 3. Added Debug Logging to App.axaml.cs
- Logs App.Initialize() progress
- Logs OnFrameworkInitializationCompleted progress
- Logs MainWindow creation
- Catches and logs all exceptions with full stack traces

### 4. Added Comprehensive Error Handling to MainWindow.axaml.cs
- Try-catch around entire constructor
- Separate error handling for ViewModel creation (non-fatal)
- Separate error handling for control caching (non-fatal)
- Separate error handling for navigation (non-fatal)
- **Window will show even if data fails to load**

### 5. Added Debug Logging to MainViewModel.cs
- Logs each ViewModel creation step
- Shows exactly which ViewModel is being initialized
- Helps identify which ViewModel is causing issues

### 6. Added Debug Logging to TimelineViewModel.cs
- Logs DatabaseService.Instance access
- Logs database path
- Logs data loading progress
- Non-fatal error handling for data loading

### 7. Added Debug Logging to DatabaseService.cs
- Logs database path resolution
- Logs DatabaseManager creation
- Shows exact database file location

### 8. Added Debug Logging to DatabaseManager.cs (Core)
- Logs database directory creation
- Logs SQLite connection opening
- Logs schema creation SQL execution
- Full error reporting with exception details

### 9. Fixed Code Bugs

**ActivityViewModel.cs - Line 84:**
```csharp
// BEFORE (BROKEN):
                });
            });  // Extra closing brace!
        }

// AFTER (FIXED):
                });
            }  // Removed extra closing brace
        }
```

**HeatmapViewModel.cs - Lines 112-118:**
```csharp
// BEFORE (BROKEN):
        var applications = new[]
        {
            foreach (var data in hourlyData)
            {
                data.Percentage = (data.Activity / (double)maxActivity) * 100;
            }
        }

// AFTER (FIXED):
        // Calculate percentages
        if (maxActivity > 0)
        {
            foreach (var data in hourlyData)
            {
                data.Percentage = (data.Activity / (double)maxActivity) * 100;
            }
        }
```

**HeatmapViewModel.cs - Line 138:**
```csharp
// BEFORE (BROKEN):
                Color = color,  // Variable 'color' doesn't exist!

// AFTER (FIXED):
                Color = colors[colorIndex % colors.Length],
```

## Debug Output Flow

When you run `MinRecall.UI.exe` from command prompt, you'll see:

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
[DEBUG] DatabaseService initializing with path: C:\Users\...\AppData\Roaming\MinRecall\minrecall.db
[DEBUG] DatabaseService constructor - creating DatabaseManager for: ...
[DEBUG] DatabaseManager constructor - path: ...
[DEBUG] Calling EnsureDatabaseCreated...
[DEBUG] EnsureDatabaseCreated starting...
[DEBUG] Database directory: ...
[DEBUG] Creating directory: ...
[DEBUG] Directory created successfully
[DEBUG] Opening SQLite connection to: ...
[DEBUG] SQLite connection opened successfully
[DEBUG] Executing database schema creation SQL...
[DEBUG] Database schema created/verified successfully
[DEBUG] DatabaseManager created successfully
[DEBUG] DatabaseService.Instance obtained, path: ...
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

## Error Example

If something fails, you'll see clear error messages:

```
[ERROR] Failed to create DatabaseManager:
  Type: System.IO.IOException
  Message: Could not write to directory C:\Users\...\AppData\Roaming\MinRecall
  StackTrace:
   at System.IO.Directory.CreateDirectory(String path)
   at MinRecall.Core.Database.DatabaseManager.EnsureDatabaseCreated()
   ...
   
Error details written to: C:\Users\...\AppData\Local\MinRecall\Logs\error_20240115_103045.log

Press any key to exit...
```

## How to Use

1. **Build the project:**
   ```bash
   dotnet build --configuration Release
   ```

2. **Navigate to output directory:**
   ```cmd
   cd src\MinRecall.UI\bin\Release\net8.0-windows10.0.19041.0
   ```

3. **Run from Command Prompt:**
   ```cmd
   MinRecall.UI.exe
   ```

4. **Watch console output** - You'll see exactly where it succeeds or fails

5. **If it crashes** - Check the error log file location shown in console

## Reverting to WinExe (Production)

Once the issue is diagnosed and fixed, you can change back to WinExe for production:

1. Edit `src/MinRecall.UI/MinRecall.UI.csproj`
2. Change `<OutputType>Exe</OutputType>` to `<OutputType>WinExe</OutputType>`
3. Rebuild

The debug logging will still work (written to log files), but the console window won't show.

## Success Criteria

✅ App launches and shows console with debug output
✅ Each initialization step is logged
✅ Errors are caught and displayed clearly
✅ Error logs are saved to files
✅ Window shows even if data loading fails
✅ Code compiles without errors
✅ Clear diagnostic information for troubleshooting
