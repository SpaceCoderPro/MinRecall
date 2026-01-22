# MinRecall UI Setup Guide

## Overview
The MinRecall UI is now fully connected to the database and all services. It displays real data from the screenshot capture system.

## How It Works

### Database Connection
The UI automatically connects to the SQLite database at:
```
%APPDATA%\MinRecall\minrecall.db
```

On Windows, this typically resolves to:
```
C:\Users\<YourUsername>\AppData\Roaming\MinRecall\minrecall.db
```

### Features Connected

✅ **Timeline View**
- Shows all screenshots for the selected date
- Displays timestamp, process name, window title
- Shows file size and dimensions
- Real-time data from the database

✅ **Search View**
- Full-text search using SQLite FTS5
- Searches through OCR text and window titles
- Debounced search (500ms delay)
- Shows search results with timestamps

✅ **Activity Heatmap**
- Hourly activity breakdown
- Top applications by usage time
- Visual representation of your day
- Based on ActivityLog table data

✅ **Activity Log**
- Detailed activity sessions
- Duration and screenshot count per session
- Filter by date
- Real database activity records

✅ **Settings View**
- Capture interval configuration
- Image quality settings
- Privacy blacklist management
- All settings will be saved to database

## Running the UI

### Option 1: Development Mode
```bash
cd src/MinRecall.UI
dotnet run
```

### Option 2: Release Build
```bash
dotnet build --configuration Release
cd src/MinRecall.UI/bin/Release/net8.0-windows10.0.19041.0
.\MinRecall.UI.exe
```

## What You'll See

### First Launch
If no screenshots have been captured yet, you'll see:
- Empty timeline (shows "Loaded 0 screenshots")
- No search results
- Empty activity heatmap
- Welcome message in the UI

### After Capture Service Runs
Once the CaptureService has been running and capturing screenshots:
1. **Timeline** will populate with screenshot entries
2. **Search** will find text from OCR processing
3. **Heatmap** will show your activity patterns
4. **Activity Log** will display your application usage

## Testing the UI

### 1. Generate Test Data
You can run the CaptureService to start capturing screenshots:
```bash
cd src/MinRecall.CaptureService
dotnet run
```

### 2. Run Both Services
For full functionality:
- **CaptureService**: Captures screenshots every N seconds
- **OptimizerService**: Processes screenshots and runs OCR
- **UI**: Displays all the data

### 3. Manual Database Check
To verify the database has data:
```bash
# Install sqlite3 if needed
sqlite3 %APPDATA%\MinRecall\minrecall.db

# Check screenshot count
SELECT COUNT(*) FROM Screenshots;

# Check latest screenshots
SELECT Timestamp, ProcessName, WindowTitle FROM Screenshots 
ORDER BY Timestamp DESC LIMIT 10;

# Check OCR data
SELECT COUNT(*) FROM OcrData;

# Check activity log
SELECT * FROM ActivityLog ORDER BY StartTime DESC LIMIT 5;
```

## Navigation

The UI has a modern sidebar navigation:
- 📅 **Timeline** - Browse screenshots chronologically
- 🔍 **Search** - Search through screenshot text
- 📊 **Activity Heatmap** - Visual activity patterns
- 📝 **Activity Log** - Detailed activity sessions
- ⚙️ **Settings** - Configure the application

## Data Refresh

The UI loads data when:
1. You open a view for the first time
2. You change the date filter
3. You perform a search
4. You navigate between views

Data is loaded asynchronously to keep the UI responsive.

## Troubleshooting

### UI Shows No Data
1. Check if database exists: `%APPDATA%\MinRecall\minrecall.db`
2. Verify CaptureService has been running
3. Check database has records (see SQL queries above)

### Window Doesn't Appear
1. Check for exceptions in the console
2. Verify all dependencies are installed
3. Try running with `dotnet run` to see error messages

### Search Not Working
1. Ensure OptimizerService has run OCR on screenshots
2. Check OcrData table has entries
3. Verify FTS5 index is populated

### Navigation Issues
1. Click on the sidebar items (📅 Timeline, 🔍 Search, etc.)
2. Watch for the highlight color change (#00d9ff)
3. Content should update in the main area

## Architecture

```
MinRecall.UI
├── Services/
│   └── DatabaseService.cs     ← Singleton database access
├── ViewModels/
│   ├── MainViewModel.cs       ← Navigation controller
│   ├── TimelineViewModel.cs   ← Real screenshot data
│   ├── SearchViewModel.cs     ← FTS5 search
│   ├── ActivityViewModel.cs   ← Activity log data
│   ├── HeatmapViewModel.cs    ← Activity heatmap
│   └── SettingsViewModel.cs   ← Settings management
└── Views/
    ├── MainWindow.axaml       ← Main shell
    ├── TimelineView.axaml     ← Timeline display
    ├── SearchView.axaml       ← Search interface
    ├── ActivityView.axaml     ← Activity log
    ├── HeatmapView.axaml      ← Heatmap visualization
    └── SettingsView.axaml     ← Settings panel
```

## Success Criteria ✅

- [x] Window displays when MinRecall.UI.exe runs
- [x] Connected to SQLite database
- [x] Shows real screenshot data in Timeline
- [x] Activity heatmap displays usage patterns
- [x] Search functionality works with OCR text
- [x] Results display with timestamps
- [x] All data connected to database
- [x] Navigation works between all views
- [x] No crashes or errors on startup

## Next Steps

1. **Start Capturing**: Run MinRecall.CaptureService
2. **Process Screenshots**: Run MinRecall.Optimizer (for OCR)
3. **View Your Data**: Open MinRecall.UI

The UI will automatically show your captured screenshot history!
