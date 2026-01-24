# MinRecall Development Guide

## Getting Started

### Prerequisites
- Windows 10/11 SDK
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension

### Build and Run

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run capture service (for development)
cd src/MinRecall.CaptureService
dotnet run

# Run optimizer (in separate terminal)
cd src/MinRecall.Optimizer
dotnet run

# Run UI (in separate terminal)
cd src/MinRecall.UI
dotnet run
```

### Install as Windows Services (Testing)

Run `install.bat` as Administrator to register services.

## Project Structure

```
MinRecall/
├── src/
│   ├── MinRecall.Core/              # Shared library
│   │   ├── Database/               # SQLite operations
│   │   ├── Models/                 # Data models
│   │   └── Settings/               # Settings management
│   │
│   ├── MinRecall.CaptureService/     # Screenshot capture
│   │   ├── WindowsApi/             # Win32 API wrappers
│   │   ├── Utilities/              # CPU monitoring
│   │   ├── CaptureWorker.cs        # Main service
│   │   └── Program.cs              # Entry point
│   │
│   ├── MinRecall.Optimizer/        # Compression & OCR
│   │   ├── ImageCompression/       # AVIF & delta algorithms
│   │   ├── Ocr/                   # OCR processing
│   │   ├── Utilities/              # CPU monitoring
│   │   ├── OptimizationWorker.cs   # Main service
│   │   └── Program.cs              # Entry point
│   │
│   └── MinRecall.UI/               # Avalonia application
│       ├── Pages/                  # UI pages
│       ├── Controls/               # Custom controls
│       ├── Styles/                 # XAML styles
│       ├── Extensions/             # Helper extensions
│       ├── MainWindow.xaml         # Main window
│       └── App.xaml                # Application entry
│
├── .github/workflows/              # CI/CD
├── docs/                          # Documentation
└── scripts/                       # Utility scripts
```

## Key Algorithms

### Delta Compression

The delta compression algorithm (in `DeltaCalculator.cs`) works as follows:

1. **Block Division**: Image divided into 16x16 pixel blocks
2. **Comparison**: Each block compared with previous frame
3. **Threshold**: Block stored if >5% pixels changed
4. **Encoding**: Changed blocks stored as raw pixel data

### Adaptive Quality

CPU-aware quality adjustment (`CpuMonitor.cs`):

```
CPU < 30%   → Full quality (settings value)
CPU < 60%   → Quality - 10
CPU < 85%   → Quality - 20
CPU >= 85%   → Minimum quality (50)
```

### OCR Processing

Windows.Media.Ocr integration:

1. Convert Bitmap to SoftwareBitmap
2. Initialize OcrEngine with language
3. RecognizeAsync() extracts text
4. Store in database with FTS5 index

## Database Operations

### Querying Screenshots

```csharp
var db = new DatabaseManager(dbPath);
var screenshots = db.GetScreenshots(
    start: DateTime.Today,
    end: DateTime.Today.AddDays(1),
    processName: "chrome.exe",
    limit: 100
);
```

### Full-Text Search

```csharp
var results = db.SearchScreenshots(
    searchTerm: "invoice",
    start: DateTime.Today.AddDays(-7),
    end: DateTime.Now,
    limit: 50
);
```

### Activity Heatmap

```csharp
var heatmap = db.GetActivityHeatmap(
    start: DateTime.Today.AddDays(-30),
    end: DateTime.Now
);
// Returns: Dictionary<string, long> { processName -> totalSeconds }
```

## Debugging

### Service Debugging

To debug Windows services:

1. Build in Debug configuration
2. Run services as console apps (not as services):
   ```bash
   set DOTNET_ENVIRONMENT=Development
   dotnet run --project src/MinRecall.CaptureService
   ```
3. Attach Visual Studio debugger

### Logging

Services log to Windows Event Log:
- **Source**: "MinRecall Capture Service" / "MinRecall Optimizer Service"
- **Log**: Windows Logs > Application

### Database Inspection

Use DB Browser for SQLite:
1. Open `%APPDATA%\MinRecall\minrecall.db`
2. Browse tables and run queries
3. Test FTS5 searches

## Testing

### Manual Testing

1. **Capture Test**: Use PC for 5-10 minutes, verify screenshots in DB
2. **Optimization Test**: Check if PNGs are converted to AVIF/JPEG
3. **Search Test**: Search for text visible in screenshots
4. **Heatmap Test**: Verify activity accuracy for different apps

### Performance Testing

```csharp
// Measure capture performance
var sw = Stopwatch.StartNew();
await CaptureScreenshot();
sw.Stop();
Console.WriteLine($"Capture: {sw.ElapsedMilliseconds}ms");

// Measure search performance
sw.Restart();
var results = db.SearchScreenshots("test");
sw.Stop();
Console.WriteLine($"Search: {sw.ElapsedMilliseconds}ms");
```

## Common Issues

### Service won't start

**Solution**: Check Event Viewer for errors. Common causes:
- .NET 8.0 Runtime not installed
- Missing dependencies
- Insufficient permissions

### Screenshots not capturing

**Solution**: Verify:
- Service is running
- Privacy blacklist settings
- Application has capture permissions

### High CPU usage

**Solution**:
- Increase CPU threshold in settings
- Reduce screenshot quality
- Increase capture interval

### Database locked

**Solution**:
- Ensure services aren't both writing simultaneously
- Check for hanging transactions
- Restart services

## Code Style

### Naming Conventions
- **Classes**: PascalCase (`DatabaseManager`)
- **Methods**: PascalCase (`GetScreenshots`)
- **Properties**: PascalCase (`FileSize`)
- **Fields**: _camelCase (`_database`)
- **Constants**: PascalCase (`DefaultQuality`)

### Async Patterns
- All async methods end with `Async`
- Use `ConfigureAwait(false)` in library code
- Avoid `async void` (use `async Task`)

### Error Handling
- Use try-catch in service loops
- Log all exceptions
- Never swallow exceptions silently

## Adding New Features

### Adding a New Page

1. Create `Pages/NewPage.xaml` and `.xaml.cs`
2. Add navigation item to `MainWindow.xaml`
3. Handle navigation in `NavView_ItemInvoked`
4. Register page routes if using frame navigation

### Adding a New Setting

1. Add property to `AppSettings` class
2. Add UI control to `SettingsDialog.xaml`
3. Load/save in `SettingsDialog.xaml.cs`
4. Update `SettingsManager` if needed

### Adding a New Export Format

1. Add to `ExportFormat` enum
2. Implement export logic in `ScreenshotViewer.xaml.cs`
3. Add to settings dropdown
4. Update format string handling

## Performance Optimization

### Database Optimization

- Use WAL mode for concurrent reads/writes
- Create indexes on frequently queried columns
- Use prepared statements (parameterized queries)
- Batch operations when possible

### Memory Optimization

- Dispose Bitmap objects after use
- Use `using` statements for file streams
- Clear collections when not needed
- Use object pools for frequent allocations

### CPU Optimization

- Adaptive quality based on CPU usage
- Background processing only when CPU < threshold
- Efficient algorithms (e.g., block-based delta)
- Parallel processing where appropriate

## Contributing

### Pull Request Checklist

- [ ] Code compiles without warnings
- [ ] Follows code style guidelines
- [ ] Includes tests for new features
- [ ] Updates documentation
- [ ] No breaking changes (or documented)

### Commit Messages

Follow conventional commits:
```
feat: add new export format
fix: resolve memory leak in optimizer
docs: update architecture documentation
test: add unit tests for delta calculator
```

## Release Process

1. Update version numbers
2. Update CHANGELOG.md
3. Tag release in git
4. Create GitHub release
5. Upload MSIX package
6. Update documentation

## Resources

### Documentation
- [Avalonia Docs](https://docs.avaloniaui.net/)
- [.NET 8.0 Docs](https://docs.microsoft.com/en-us/dotnet/)
- [SQLite FTS5](https://www.sqlite.org/fts5.html)

### Tools
- [DB Browser for SQLite](https://sqlitebrowser.org/)
- [Windows Performance Analyzer](https://docs.microsoft.com/en-us/windows-hardware/test/wpt/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/)

### Community
- [GitHub Issues](https://github.com/yourusername/MinRecall/issues)
- [GitHub Discussions](https://github.com/yourusername/MinRecall/discussions)
