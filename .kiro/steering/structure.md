# Project Structure

## Organization Philosophy

**MVVM with Service Layer**

- UI logic separated from platform-specific implementations
- Service interfaces abstract platform differences
- Models represent domain data without UI dependencies

## Directory Patterns

### ViewModels (`/ViewModels/`)
**Purpose**: UI logic and state management
**Pattern**: One ViewModel per View, inherits from ReactiveObject
**Example**: `MainWindowViewModel.cs`, `SettingsViewModel.cs`

### Views (`/Views/`)
**Purpose**: AXAML UI definitions
**Pattern**: One View per screen, data binding to ViewModel
**Example**: `MainWindow.axaml`, `SettingsWindow.axaml`

### Services (`/Services/`)
**Purpose**: Business logic and platform-specific implementations
**Pattern**: Interface + platform implementations
**Example**:
```csharp
IScreenCaptureService.cs          // Interface
MacOSScreenCaptureService.cs      // macOS implementation
WindowsScreenCaptureService.cs    // Windows implementation
```

### Models (`/Models/`)
**Purpose**: Domain data structures
**Pattern**: POCOs with validation, no UI dependencies
**Example**: `CaptureSettings.cs`, `HotkeyConfig.cs`, `CapturedImage.cs`

### Helpers (`/Helpers/`)
**Purpose**: Utility functions and static helpers
**Pattern**: Static classes with pure functions
**Example**: `DisplayHelper.cs`, `FileNameHelper.cs`

## Naming Conventions

- **Files**: PascalCase matching type name (`MainWindowViewModel.cs`)
- **Types**: PascalCase (`CaptureSettings`, `IScreenCaptureService`)
- **Public Members**: PascalCase (`CaptureRegion`, `HasPermission()`)
- **Private Fields**: camelCase with underscore prefix (`_image`, `_settings`)
- **View Files**: PascalCase with `.axaml` extension

## Code Organization Principles

### Dependency Flow
```
Views → ViewModels → Services → Models
```
- Views depend on ViewModels only
- ViewModels depend on Services and Models
- Services may depend on Models
- Models have no dependencies on other layers

### Platform Abstraction
```csharp
// Define interface
public interface IScreenCaptureService
{
    Task<CapturedImage> CaptureRegionAsync(CaptureRegion region);
}

// Platform-specific implementations
public class MacOSScreenCaptureService : IScreenCaptureService { }
public class WindowsScreenCaptureService : IScreenCaptureService { }

// DI registration selects implementation at runtime
```

### Async Patterns
- I/O operations use async/await
- UI operations remain synchronous when appropriate
- Service methods return `Task<T>` for async operations

### Configuration Management
- Settings stored as JSON in platform-specific app data folder
- `SettingsService` handles serialization/deserialization
- Settings loaded at startup, saved on change

---
_Created: 2025-11-27_
