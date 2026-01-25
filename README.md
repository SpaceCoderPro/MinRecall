# MinRecall

A complete Windows Recall-style screenshot application for Windows 10/11. Capture, store, and search through your screen activity with smart compression and OCR.

## Features

### Core Capture
- 📸 **Automatic Screenshots**: Captures foreground window every 60 seconds (configurable)
- 🎯 **Smart Capture**: Only captures active windows, not full desktop
- 📊 **Adaptive Quality**: Adjusts quality based on CPU usage
- 📝 **Activity Logging**: Tracks which apps you use and for how long

### Smart Storage
- 💾 **Keyframe + Delta Compression**: Achieves ~12-17KB per screenshot average
- 🗜️ **AVIF Compression**: Compresses keyframes to ~80KB
- 📉 **Storage Efficient**: <1GB per month typical usage
- 🔍 **Full-Text Search**: Search through window titles and OCR text

### Beautiful UI
- 🖥️ **Avalonia UI**: Cross-platform modern interface
- 📅 **Timeline View**: Visual timeline of your activity
- 🔥 **Activity Heatmap**: See where you spend your time
- 🔎 **Instant Search**: Find anything in milliseconds
- ⌨️ **Global Hotkey**: Ctrl+Alt+R to open search

### Privacy & Control
- 🔒 **Local Only**: All data stored locally, no cloud
- 🚫 **Privacy Mode**: Blacklist apps you don't want to capture
- 👁️ **Blur Sensitive Data**: Auto-blur emails, phone numbers
- 🗑️ **Auto Cleanup**: Delete old data automatically
- ⚙️ **Comprehensive Settings**: Full control over every aspect

## Screenshot

![Timeline View](docs/timeline.png)
![Activity Heatmap](docs/heatmap.png)
![Search](docs/search.png)

## Installation

See [INSTALL.md](INSTALL.md) for detailed installation instructions.

### Quick Install (MSIX)

1. Download the latest MSIX from [Releases](https://github.com/yourusername/MinRecall/releases)
2. Double-click to install
3. Done!

## Usage

### Basic Usage

1. **Install** MinRecall
2. **Start** working normally - screenshots are captured automatically
3. **Open** MinRecall from the Start menu or press `Ctrl+Alt+R`
4. **Browse** your timeline, search, or view activity heatmap

### Timeline View

- Browse screenshots by hour, day, or week
- Click any screenshot to view full size
- Filter by application or time period

### Search

- Search by window title
- Search by OCR text (text visible in screenshots)
- Filter by date range
- Results appear in <100ms

### Activity Heatmap

- Visual representation of time spent per application
- See your most-used apps at a glance
- Click to view detailed activity log

### Settings

Access all settings from the gear icon:

- **Capture**: Interval, quality profile, keyframe frequency
- **Optimization**: CPU threshold, background OCR, auto-cleanup
- **Privacy**: Blacklist apps, blur sensitive data, retention period
- **Storage**: Change storage location
- **Startup**: Auto-start with Windows
- **OCR**: Languages, enable/disable
- **Timeline**: Default view preference
- **Export**: Default format (PNG/JPEG/WebP)

## Architecture

```
MinRecall/
├── MinRecall.CaptureService/    # Background screenshot capture
├── MinRecall.Optimizer/         # Compression & OCR processing
├── MinRecall.UI/               # Avalonia user interface
└── MinRecall.Core/             # Shared database & models
```

### Components

#### Capture Service (Windows Service)
- Captures foreground window screenshots
- Logs window activity
- Stores temporary PNG files
- Adaptive quality based on CPU
- Privacy blacklist filtering

#### Optimizer Service (Windows Service)
- Runs when CPU < 15% (configurable)
- Compresses keyframes to AVIF
- Calculates and stores frame deltas
- Performs OCR processing
- Updates database indexes
- Auto-cleanup old data

#### UI Application (Avalonia)
- Timeline viewer with thumbnails
- Activity heatmap visualization
- Full-text search (FTS5)
- Settings panel
- System tray integration
- Global hotkey support

#### Core Library
- SQLite database with FTS5
- Data models and settings management
- Delta compression algorithms
- AVIF/JPEG compression

## Technical Details

### Storage Efficiency

- **Keyframes**: Every N screenshots (default 25)
  - Compressed to AVIF (~80KB)
- **Deltas**: Between keyframes
  - Binary delta format (~10KB)
- **Average**: ~12-17KB per screenshot
- **Monthly**: <1GB for typical usage

### Database Schema

```sql
Screenshots:           -- All screenshot metadata
ActivityLog:           -- App usage sessions
DeltaFiles:            -- Delta compression data
OcrData:               -- OCR text (FTS5 indexed)
ScreenshotsFts:         -- Full-text search index
Settings:              -- Application settings
```

### Performance

- **Memory**: <200MB total (both services)
- **CPU**: <5% during idle, adaptive during capture
- **Storage**: <1GB/month typical usage
- **Search**: <100ms response time
- **Startup**: Services start in <2 seconds

## Development

### Prerequisites

- .NET 8.0 SDK
- Windows 10/11 SDK
- Visual Studio 2022 (recommended) or VS Code

### Building

```bash
# Build solution
dotnet build --configuration Release

# Run tests
dotnet test

# Build for Windows x64
dotnet publish -c Release -r win-x64 --self-contained
```

### Running Services (Development)

```bash
# Run capture service
cd src/MinRecall.CaptureService
dotnet run

# Run optimizer
cd src/MinRecall.Optimizer
dotnet run

# Run UI
cd src/MinRecall.UI
dotnet run
```

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

MIT License - see LICENSE file for details

## Roadmap

- [ ] AVIF compression with native Windows API
- [ ] WebM export for video playback
- [ ] Machine learning for smart filtering
- [ ] Cloud sync (optional, opt-in)
- [ ] Mobile app for viewing
- [ ] Plugin system for custom analyzers
- [ ] Multi-monitor support
- [ ] Custom hotkeys
- [ ] Export to PDF
- [ ] Calendar view integration

## Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/MinRecall/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/MinRecall/discussions)
- **Documentation**: [Wiki](https://github.com/yourusername/MinRecall/wiki)

## Privacy Policy

MinRecall is designed with privacy in mind:

- All data is stored locally on your computer
- No data is transmitted to any server
- No telemetry or analytics
- You have full control over your data
- Easy export and deletion options

## Acknowledgments

- Inspired by Windows Recall feature
- Built with Avalonia UI framework
- OCR powered by Windows.Media.Ocr
- Database: SQLite with FTS5

## Star History

[![Star History Chart](https://api.star-history.com/svg?repos=yourusername/MinRecall&type=Date)](https://star-history.com/#yourusername/MinRecall&Date)
