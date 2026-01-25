# MinRecall Architecture

## Overview

MinRecall consists of three main components working together to capture, optimize, and present screen activity:

1. **Capture Service** - Background Windows service for screenshot capture
2. **Optimizer Service** - Background Windows service for compression and OCR
3. **UI Application** - Avalonia desktop application for user interaction

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        User Interface                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Timeline   │  │    Search    │  │   Heatmap    │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────┬───────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Core Library                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  Database    │  │   Settings   │  │    Models    │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└───────────┬──────────────────────────┬─────────────────────────┘
            │                          │
            ▼                          ▼
    ┌──────────────┐          ┌──────────────┐
    │   Capture    │          │  Optimizer   │
    │   Service    │          │   Service    │
    └──────────────┘          └──────────────┘
            │                          │
            └──────────┬───────────────┘
                       ▼
              ┌──────────────┐
              │   SQLite DB  │
              └──────────────┘
```

## Component Details

### Capture Service

**Purpose**: Continuously capture foreground window screenshots

**Responsibilities**:
- Detect foreground window using Win32 API
- Capture screenshot at configurable intervals (default: 60s)
- Apply adaptive quality based on CPU usage
- Respect privacy blacklist
- Log window activity (app name, title, duration)
- Store temporary PNG files
- Mark screenshots as pending optimization

**Key Classes**:
- `CaptureWorker` - Main background service
- `WindowCapture` - Win32 API wrappers for screenshot capture
- `CpuMonitor` - CPU usage monitoring for adaptive quality

**Data Flow**:
```
Window Changed → Screenshot Captured → PNG Saved → DB Record Created (Pending)
```

### Optimizer Service

**Purpose**: Compress and optimize captured screenshots

**Responsibilities**:
- Monitor CPU usage (runs when CPU < threshold)
- Process pending screenshots from queue
- Compress keyframes to AVIF/JPEG
- Calculate and store deltas between frames
- Perform OCR text extraction
- Update database with optimized data
- Clean up original PNG files
- Auto-cleanup old data

**Key Classes**:
- `OptimizationWorker` - Main background service
- `AvifCompressor` - Image compression utilities
- `DeltaCalculator` - Frame-to-frame delta computation
- `OcrProcessor` - Windows.Media.Ocr wrapper

**Data Flow**:
```
Pending Screenshot → Load PNG →
  ├─ If Keyframe: Compress to AVIF → Save → Update DB
  └─ If Delta: Calculate Delta → Save → Update DB
OCR Extraction → Update FTS Index → Clean up PNG
```

### UI Application

**Purpose**: Provide user interface for browsing and searching

**Responsibilities**:
- Display timeline of screenshots
- Show activity heatmap
- Full-text search (FTS5)
- Screenshot viewer with export options
- Settings management
- System tray integration
- Global hotkey handling

**Key Classes**:
- `MainWindow` - Main application window with navigation
- `TimelineView` - Screenshot timeline view
- `SearchView` - Full-text search interface
- `HeatmapView` - Activity visualization
- `ActivityView` - Detailed activity log
- `SettingsView` - Settings configuration

**Data Flow**:
```
User Action → Query DB → Display Results → User Selects → View Screenshot
```

### Core Library

**Purpose**: Shared functionality and data management

**Responsibilities**:
- SQLite database management
- Data models (Screenshots, ActivityLog, Settings)
- FTS5 full-text search
- Settings persistence
- Database operations

**Key Classes**:
- `DatabaseManager` - All database operations
- `SettingsManager` - Settings load/save
- `Screenshot` - Screenshot data model
- `ActivityLog` - Activity session model
- `AppSettings` - Settings configuration

## Database Schema

### Screenshots Table
```sql
CREATE TABLE Screenshots (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp INTEGER NOT NULL,           -- FileTimeUtc
    WindowTitle TEXT NOT NULL,
    ProcessName TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    IsKeyframe INTEGER NOT NULL,          -- 0/1
    KeyframeId INTEGER,                   -- Reference to keyframe
    DeltaFileId INTEGER,                  -- Reference to delta file
    Width INTEGER NOT NULL,
    Height INTEGER NOT NULL,
    FileSize INTEGER NOT NULL,
    Status INTEGER NOT NULL,              -- Pending/Optimizing/Optimized/Failed
    ProcessedAt INTEGER                   -- FileTimeUtc
);
```

### ActivityLog Table
```sql
CREATE TABLE ActivityLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProcessName TEXT NOT NULL,
    WindowTitle TEXT NOT NULL,
    StartTime INTEGER NOT NULL,            -- FileTimeUtc
    EndTime INTEGER NOT NULL,              -- FileTimeUtc
    DurationSeconds INTEGER NOT NULL,
    ScreenshotCount INTEGER NOT NULL
);
```

### DeltaFiles Table
```sql
CREATE TABLE DeltaFiles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ScreenshotId INTEGER NOT NULL,
    FilePath TEXT NOT NULL,
    FileSize INTEGER NOT NULL,
    CreatedAt INTEGER NOT NULL,           -- FileTimeUtc
    FOREIGN KEY (ScreenshotId) REFERENCES Screenshots(Id)
);
```

### OcrData Table
```sql
CREATE TABLE OcrData (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ScreenshotId INTEGER NOT NULL UNIQUE,
    Text TEXT NOT NULL,
    Language TEXT NOT NULL,
    Confidence REAL,
    ProcessedAt INTEGER NOT NULL,         -- FileTimeUtc
    FOREIGN KEY (ScreenshotId) REFERENCES Screenshots(Id)
);
```

### ScreenshotsFts Table (FTS5)
```sql
CREATE VIRTUAL TABLE ScreenshotsFts USING fts5(
    ScreenshotId,
    WindowTitle,
    OcrText
);
```

### Settings Table
```sql
CREATE TABLE Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);
```

## Compression Strategy

### Keyframe + Delta Compression

The compression strategy dramatically reduces storage requirements:

1. **Keyframes**: Every N screenshots (default: 25)
   - Compressed to AVIF (~80KB)
   - Serve as reference for subsequent frames

2. **Deltas**: Between keyframes
   - Binary format storing only changed regions
   - ~10KB average for typical screen changes
   - 16x16 pixel blocks with thresholding

3. **Storage Efficiency**:
   - Average per screenshot: 12-17KB
   - Compared to raw PNG: ~95% reduction
   - Typical monthly usage: <1GB

### Delta Calculation Algorithm

```
For each 16x16 block:
  1. Compare pixel values with previous frame
  2. If >5% pixels changed (threshold):
     - Store changed pixels in delta
  3. Else:
     - Skip block (unchanged)
```

## Performance Considerations

### Memory Usage
- Capture Service: ~80MB
- Optimizer Service: ~100MB
- UI Application: ~20-50MB
- Total: <200MB

### CPU Impact
- Idle: <5% total
- Capture burst: ~15-20% for <100ms
- Optimization: Adaptive (only when CPU < 15%)

### Storage Impact
- Write operations: Minimized (keyframes only)
- SSD-friendly: Delta files are small
- Cleanup: Automatic by retention policy

### Search Performance
- FTS5 indexed: <100ms queries
- Database: SQLite with WAL mode
- Caching: Thumbnail images cached in memory

## Security & Privacy

### Data Locality
- All data stored locally
- No network transmission
- No cloud sync (by default)

### Privacy Features
- Application blacklist (user-controlled)
- Sensitive data blurring (optional)
- Automatic cleanup (retention policy)
- Export and delete capabilities

### Permissions Required
- Screenshot capture (foreground window)
- File system access (storage)
- Service management (Windows services)

## Extension Points

### Adding New Export Formats
1. Implement in `ScreenshotViewer.xaml.cs`
2. Add format to `ExportFormat` enum
3. Update Settings dialog

### Adding New OCR Languages
1. Check `OcrProcessor.GetAvailableLanguages()`
2. Add to settings
3. Update UI language selector

### Adding New Compression Formats
1. Implement in `AvifCompressor.cs`
2. Update optimization workflow
3. Add format option to settings

## Deployment Architecture

### Production Deployment
```
1. MSIX Package
    - UI application (Avalonia)
    - Embedded services
    - Auto-update support

2. Installation
   - Windows Services registered
   - Desktop shortcut created
   - Start menu entry added

3. Runtime
   - Services run as SYSTEM
   - UI runs as user
   - IPC through SQLite database
```

### Development Deployment
```
1. Manual service registration
   - sc create commands
   - Development builds

2. Console mode
   - Services can run as console apps
   - Easier debugging

3. File watching
   - Hot reload for UI
   - Manual service restart
```

## Monitoring & Debugging

### Logging
- Windows Event Log (services)
- Console output (development)
- File logging (optional)

### Performance Counters
- CPU usage monitoring
- Memory usage tracking
- Disk I/O measurement

### Debugging Tools
- Visual Studio debugger
- Windows Performance Analyzer
- SQLite Database Browser
- Event Viewer

## Future Enhancements

### Planned Features
- AVIF with native Windows API
- WebM video export
- Machine learning filtering
- Cloud sync (optional)
- Mobile viewing app
- Plugin system

### Architecture Considerations
- Microservices for scalability
- Message queue for IPC
- Distributed OCR processing
- Caching layer (Redis)
- Analytics dashboard

## Conclusion

The MinRecall architecture is designed for:
- **Efficiency**: Minimal CPU/storage impact
- **Reliability**: Robust error handling
- **Privacy**: Local-only data processing
- **Extensibility**: Easy to add features
- **Maintainability**: Clear separation of concerns
