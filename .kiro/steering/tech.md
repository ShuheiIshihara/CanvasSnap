# Technology Stack

## Architecture

**MVVM + Dependency Injection + Platform Abstraction**

- Clean separation of UI (View), logic (ViewModel), and data (Model)
- Service interfaces with platform-specific implementations (Strategy pattern)
- DI container for service lifecycle management

## Core Technologies

- **Language**: C# (.NET 8.0)
- **UI Framework**: Avalonia 11.x (cross-platform desktop UI)
- **MVVM**: ReactiveUI for reactive bindings
- **Image Processing**: SixLabors.ImageSharp (System.Drawing.Common avoided due to macOS/.NET 6+ deprecation)
- **Configuration**: System.Text.Json for settings persistence

## Key Libraries

- **Avalonia.Desktop**: Native desktop application support
- **ReactiveUI**: MVVM reactive extensions
- **SixLabors.ImageSharp**: Cross-platform image manipulation
- **System.Text.Json**: Settings serialization

## Development Standards

### Type Safety
- C# with nullable reference types enabled
- Avoid `dynamic` and implicit typing where clarity matters

### Code Quality
- Follow standard C# naming conventions (PascalCase for types/public members, camelCase for locals)
- MVVM binding patterns for UI updates
- Async/await for I/O operations

### Testing
- Unit tests for services (platform-specific implementations)
- Integration tests for capture workflow
- Performance benchmarks: <0.5s capture time, <50MB idle memory

## Development Environment

### Required Tools
- .NET SDK 8.0+
- IDE: Visual Studio, Rider, or VS Code with C# extensions

### Common Commands
```bash
# Build: dotnet build
# Run: dotnet run --project ScreenshotApp
# Test: dotnet test
```

## Platform-Specific Implementations

### macOS (v1.0)
- **Screen Capture**: `screencapture` command via Process.Start
  - Future: ScreenCaptureKit for better performance
- **Hotkey**: CGEvent API (Quartz Event Services) via P/Invoke
  - Reason: Carbon Framework deprecated, CGEvent API is modern and officially supported
  - Requires: Accessibility permission in addition to Screen Recording
- **Permission**: Screen Recording permission required

### Windows (Planned)
- **Screen Capture**: BitBlt or Windows.Graphics.Capture API
- **Hotkey**: Win32 API RegisterHotKey (P/Invoke)
- **DPI Scaling**: Coordinate translation for HiDPI displays

## Key Technical Decisions

1. **Avalonia over other UI frameworks**: Cross-platform native feel, XAML-like syntax
2. **ImageSharp over System.Drawing**: Cross-platform, actively maintained, no GDI+ dependency
3. **Process-based capture (macOS v1.0)**: Quick implementation, sufficient performance for MVP
4. **Interface-based platform abstraction**: Easy testing, clean Windows port path

---
_Created: 2025-11-27_
