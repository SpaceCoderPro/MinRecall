# MinRecall - Installation Guide

MinRecall is a Windows Recall-style screenshot application that captures your screen activity and allows you to search through it.

## System Requirements

- **Operating System**: Windows 10 (version 1809 or later) or Windows 11
- **Architecture**: x64 or ARM64
- **RAM**: 4GB minimum, 8GB recommended
- **Storage**: 128GB SSD (or HDD with at least 2GB free space)

## Installation

### Option 1: MSIX Installer (Recommended)

1. Download the latest MSIX package from the [Releases](https://github.com/yourusername/MinRecall/releases) page
2. Double-click the MSIX file to install
3. Follow the installation wizard

### Option 2: Manual Installation

1. Download and extract the latest release zip file
2. Run `install.bat` as Administrator
3. The services will be registered and the UI will be available

### Option 3: Building from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/MinRecall.git
cd MinRecall

# Build the solution
dotnet build --configuration Release

# Run the installer
cd src/MinRecall.UI/bin/Release/net8.0-windows/win-x64/publish/
Install.ps1
```

## Services

MinRecall runs two background services:

1. **MinRecall Capture Service** - Captures screenshots periodically
2. **MinRecall Optimizer Service** - Compresses and optimizes screenshots

You can manage these services using the Windows Services manager (`services.msc`).

## First Launch

1. Open MinRecall from the Start menu
2. The capture service will start automatically
3. Configure your settings using the Settings dialog (gear icon)

## Configuration

Default settings:
- Capture interval: 60 seconds
- Screenshot quality: Balanced
- Storage location: `%APPDATA%\MinRecall\screenshots`
- Retention period: 12 months

You can adjust these settings in the Settings panel.

## Uninstallation

### MSIX Installation

1. Go to Settings > Apps > Installed apps
2. Find MinRecall and click Uninstall

### Manual Installation

Run `uninstall.bat` as Administrator to:
- Stop and remove Windows services
- Delete application files
- Keep your data (screenshots and database)

## Data Location

All data is stored locally:
- **Screenshots**: `%APPDATA%\MinRecall\screenshots\`
- **Database**: `%APPDATA%\MinRecall\minrecall.db`
- **Settings**: `%APPDATA%\MinRecall\settings.json`

## Troubleshooting

### Service won't start

1. Check Windows Event Viewer for error messages
2. Ensure you have .NET 8.0 Runtime installed
3. Run the service installation as Administrator

### Screenshots not capturing

1. Check if the capture service is running
2. Verify your privacy blacklist settings
3. Ensure the application has necessary permissions

### High CPU usage

1. Increase the CPU threshold in settings
2. Reduce the screenshot quality
3. Increase the capture interval

## Privacy

MinRecall processes all data locally. No data is sent to the cloud. You can:
- Blacklist specific applications from being captured
- Enable data blurring for sensitive information
- Set automatic cleanup periods
- Export or delete all your data at any time
