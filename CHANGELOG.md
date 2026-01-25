# Changelog

All notable changes to MinRecall will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial release of MinRecall
- Screenshot capture service with adaptive quality
- Background optimizer with keyframe + delta compression
- Avalonia user interface
- Timeline view with screenshot browsing
- Activity heatmap visualization
- Full-text search (FTS5) for window titles and OCR text
- Activity log with detailed session tracking
- Comprehensive settings panel
- Privacy mode with app blacklist
- Sensitive data blurring
- Auto-cleanup with configurable retention
- Global hotkey support (Ctrl+Alt+R)
- Export functionality (PNG/JPEG/WebP)
- Windows Services for background tasks
- MSIX packaging for Windows Store deployment
- GitHub Actions CI/CD pipeline

### Features
- **Capture**: Automatic foreground window capture every 60 seconds (configurable)
- **Storage**: Smart compression achieving ~12-17KB per screenshot
- **Search**: <100ms full-text search across all screenshots
- **Privacy**: Local-only data processing, no cloud
- **Performance**: <200MB RAM total, <5% CPU during idle
- **Accessibility**: Keyboard navigation, screen reader support

### Documentation
- README with feature overview
- INSTALL guide with installation instructions
- ARCHITECTURE documentation
- DEVELOPMENT guide for contributors
- LICENSE (MIT)

## [1.0.0] - Future Release

### Planned
- AVIF compression with native Windows API
- WebM export for video playback
- Machine learning for smart filtering
- Multi-monitor support
- Custom hotkeys
- Plugin system
- Mobile viewing app
