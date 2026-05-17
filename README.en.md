# SnapIt

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6.svg)](https://www.microsoft.com/windows)
[![WPF](https://img.shields.io/badge/UI-WPF-5C2D91.svg)](https://github.com/dotnet/wpf)
![Build](https://img.shields.io/badge/build-passing-brightgreen)

**Read in other languages:** **[English](README.en.md),** **[中文](README.md)**

***

\*\*SnapIt \*\*is a powerful window manager for Windows 10/11 that significantly boosts productivity in single-screen and multi-screen environments by organizing application windows into customizable snap zones. This project originates from [SnapIt](https://github.com/enginkirmaci/SnapIt) by Engin KIRMACI, with SnapIt-Plus evolving continuously to add new features and improvements.

Design philosophy: Fast, intuitive, and highly customizable — perfect for widescreen, ultrawide, and multi-monitor setups.

***

## Screenshots

<table>
  <tr>
    <td><img src="/documents/SnapIt-Plus/0.HomePage.png" alt="Home Page" width="400"></td>
    <td><img src="/documents/SnapIt-Plus/1.LayoutPage.png" alt="Layout Page" width="400"></td>
  </tr>
  <tr>
    <td align="center"><em>Home Page</em></td>
    <td align="center"><em>Layout Page</em></td>
  </tr>
  <tr>
    <td><img src="/documents/SnapIt-Plus/2.SettingPage.png" alt="Setting Page" width="400"></td>
    <td><img src="/documents/SnapIt-Plus/3.ThemePage.png" alt="Theme Page" width="400"></td>
  </tr>
  <tr>
    <td align="center"><em>Setting Page</em></td>
    <td align="center"><em>Theme Page</em></td>
  </tr>
  <tr>
    <td><img src="/documents/SnapIt-Plus/4.DragWindow.png" alt="Drag Window" width="400"></td>
    <td><img src="/documents/SnapIt-Plus/5.CreateMergeArea.png" alt="Create Merge Area" width="400"></td>
  </tr>
  <tr>
    <td align="center"><em>Drag Window</em></td>
    <td align="center"><em>Create Merge Area</em></td>
  </tr>
</table>

***

## Features

### Screen Partitioning & Layout System

- Divide your screen into customizable snap zones using **vertical/horizontal dividers**
- Each screen can have **independent layouts**
- **11 built-in layouts** covering common use cases (portrait/landscape)
- **Visual layout designer**: Drag-and-drop to create and edit custom layouts
- Snap alignment based on **SnapEngine** (8px snap threshold)
- Import/export layouts as JSON files
- Branch features: Create new overlays based on divided zones
- Branch features: Multi-equal distribution layouts, hover over icons to change the number of equal divisions
- Branch features: Edge snap and center alignment, double-click to delete dividers
- Branch features: Chinese UI support (i18n)
- Branch features: Design window properties panel supports four-corner docking toggle
- Branch features: Overlay center coordinate display and editing

### Window Snapping Methods

**Mouse Drag Snapping**

- Simply drag a window to a snap zone for automatic placement
- Configurable mouse button (left/middle/right)
- Adjustable drag tolerance
- Support for "hold titlebar to drag" mode
- Support for modifier keys to toggle enable/disable

**Keyboard Snapping**

- Move windows between zones using keyboard shortcuts
- Default shortcut: `Ctrl+Alt+Arrow Keys`
- Override Windows default snap hotkeys (`Win+Arrow Keys`)
- Cycle layout shortcut (`Ctrl+Alt+C`)
- Start/stop shortcut (`Ctrl+Alt+S`)

### App Launcher (App Groups)

- Configure **app groups** for each screen
- Launch multiple apps at once and automatically place them into designated zones
- Support for startup delay settings

### Theme System

- Customize snap zone **highlight color, overlay color, border color, border thickness, and opacity**
- Three theme modes: **Dark/Light/System**
- Real-time theme preview

### Window Exclusion Rules

- Exclude specific apps from snapping by window title matching
- Three matching rules: **Contains / Exact / Wildcard**
- Apply separately to mouse and keyboard snapping
- Default exclusions: Action Center, Start Menu, New Notifications

### Multi-Monitor & DPI Support (Branch Enhancements)

- Full multi-monitor environment support
- Independent DPI awareness per screen
- Hot-plug detection for screens
- Support for screens with different taskbar positions

### Application Management

- System tray icon for quick access
- Auto-start on boot
- New version check
- Run as administrator
- Auto-disable snapping for fullscreen/modal windows
- Automatic handling of window rounded corners

### Settings Storage

- Uses **SQLite** database (`SnapItSettings.db`), stored in `%LocalApplicationData%\SnapIt\`
- **Automatic migration** from legacy JSON files on first run
- Benefits: ACID transaction guarantees, improved read/write performance, single-file backup

***

## Requirements

- **OS**: Windows 10 (Build 17763+) or Windows 11
- **Runtime**: .NET 9.0 Desktop Runtime (required for standalone version)
- **Architecture**: x86 / x64

***

## Installation

### Microsoft Store Version (MSIX Packaging)

1. Open the SnapIt.Packaging project in Visual Studio
2. Build the WAP project to generate `.msix`
3. Install using the generated package

### Standalone Installer (Inno Setup)

1. Download the latest `setup_SnapIt_x.x.x.x.exe` from the [Releases](https://github.com/Ghost-Girls/SnapIt-Plus/releases) page
2. Run the installer and follow the wizard

### Build from Source

```bash
git clone https://github.com/Ghost-Girls/SnapIt-Plus.git
cd SnapIt-Plus
```

Open `SnapIt.sln` in Visual Studio 2022+, then build:

```bash
# Debug build
dotnet build SnapIt.sln -c Debug

# Standalone publish (for Inno Setup packaging)
dotnet publish SnapIt/SnapIt.csproj -c Standalone -a x86 -o ./SnapIt.Setup/build --self-contained false

# Generate Inno Setup installer
iscc.exe SnapIt.Setup/innoSetup.iss /DMyAppVersion=5.3.0.0
```

***

## Configuration

Open the **SnapIt** application window to access all settings:

### Layouts

| Option        | Description                                        |
| ------------- | -------------------------------------------------- |
| Select Layout | Choose predefined or custom layout for each screen |
| Edit Layout   | Open visual designer to create/modify layouts      |
| Import Layout | Load layout from JSON file                         |
| Export Layout | Save layout as JSON file                           |

### Snapping Settings

| Option               | Default             | Description                                  |
| -------------------- | ------------------- | -------------------------------------------- |
| Mouse Snap           | On                  | Enable mouse drag snapping                   |
| Snap Mouse Button    | Middle              | Left / Middle / Right button to trigger snap |
| Drag Delay           | 0ms                 | Delay before snap activates (0-1000ms)       |
| Override Win+Arrows  | Off                 | Override Windows default snap hotkeys        |
| Keyboard Snap Hotkey | Ctrl+Alt+Arrow Keys | Hotkey to move window between zones          |

### Appearance

| Option           | Default | Description                   |
| ---------------- | ------- | ----------------------------- |
| Theme            | Dark    | Dark / Light / System         |
| Highlight Color  | Custom  | Zone highlight color on hover |
| Overlay Color    | Custom  | Zone overlay color            |
| Border Color     | Custom  | Zone border color             |
| Border Thickness | Custom  | Zone border thickness         |
| Opacity          | Custom  | Zone overlay opacity          |

### App Groups

| Option           | Description                                |
| ---------------- | ------------------------------------------ |
| Add Group        | Create a new app group for a screen        |
| Add App          | Add app to group and assign target zone    |
| Launch Delay     | Configure delay between launching each app |
| One-Click Launch | Launch all apps in group and auto-place    |

### Exclusion Rules

| Option              | Default | Description                          |
| ------------------- | ------- | ------------------------------------ |
| Mouse Exclusions    | Off     | Contains / Exact / Wildcard matching |
| Keyboard Exclusions | Off     | Contains / Exact / Wildcard matching |

***

## Architecture

```
SnapIt-Plus
├── SnapIt                          # Main WPF app (Views, ViewModels, Pages)
├── SnapIt.Application              # Core business logic
│   ├── SnapManager.cs              # Central snap coordinator
│   ├── WindowManager.cs            # Window position &amp; state management
│   └── ScreenManager.cs            # Multi-screen management
├── SnapIt.Common                   # Shared library
│   ├── Entities/                   # Data models: Settings, Layout, Theme, Constants, Enums
│   ├── Graphics/                   # Custom graphics primitives (Rect, Point, Size, Line, Dpi)
│   ├── Math/                       # FindRectangle, FindClosest algorithms
│   ├── Converters/                 # WPF value converters
│   └── Contracts/                  # Interfaces &amp; base classes
├── SnapIt.Controls                 # Custom WPF controls
│   ├── SnapArea.cs                 # Individual snap zone control
│   ├── SnapBorder.cs               # Zone border decoration
│   ├── SnapOverlay.cs              # Zone overlay rendering
│   └── SnapEngine.cs               # Snap alignment engine
├── SnapIt.Services                 # Service layer
│   ├── Hooks/                      # Global mouse/keyboard hooks (SharpHook)
│   ├── HotKeys/                    # Global hotkey registration
│   ├── Database/                   # EF Core + SQLite (SettingsDbContext)
│   └── License/                    # Store &amp; standalone licensing services
├── SnapIt.Layouts                  # Predefined layouts library (11 built-in JSON layouts)
├── SnapIt.Packaging                # MSIX packaging (Windows Application Project)
├── SnapIt.Setup                    # Inno Setup installer scripts
├── SnapIt.Test                     # Test/design-time project
├── WindowInspector                 # Helper tool: Window info inspector
└── SnapSample                      # SDK usage examples
```

### Key Components

| Component           | File                                                                                                                    | Responsibility                                                                   |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| **SnapManager**     | [SnapManager.cs](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.App/SnapManager.cs)                       | Central coordinator: manages snap sessions, zone detection, and window placement |
| **SnapEngine**      | [SnapEngine.cs](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.Controls/SnapEngine.cs)                    | Snap alignment calculation (8px threshold)                                       |
| **ScreenManager**   | [ScreenManager.cs](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.App/ScreenManager.cs)                   | Multi-monitor enumeration, DPI handling, hot-plug events                         |
| **WindowManager**   | [WindowManager.cs](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.App/WindowManager.cs)                   | Window positioning, resizing, and state management (P/Invoke)                    |
| **KeyboardService** | [KeyboardService.cs](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.Services/Hooks/KeyboardService.cs)    | Global keyboard hook and hotkey dispatch                                         |
| **MouseService**    | [MouseService.cs](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.Services/Hooks/MouseService.cs)          | Global mouse hook, detects drag snapping                                         |
| **SettingsService** | [SettingsService.cs](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.Services/Services/SettingsService.cs) | EF Core SQLite persistence for settings and layouts                              |

***

## Tech Stack

| Technology                          | Purpose                                                          |
| ----------------------------------- | ---------------------------------------------------------------- |
| **.NET 9.0**                        | Target framework (`net9.0-windows10.0.17763.0`)                  |
| **WPF**                             | Desktop UI framework                                             |
| **WPF-UI 4.0.3**                    | Modern UI control library (Navigation, Snackbar, Dialog, Themes) |
| **MVVM**                            | Architecture pattern (custom ViewModelBase + DelegateCommand)    |
| **Microsoft.Extensions.Hosting/DI** | Dependency injection & hosted services                           |
| **Serilog**                         | Structured file logging                                          |
| **Entity Framework Core + SQLite**  | Persistent storage for settings and layouts                      |
| **SharpHook 7.0**                   | Global mouse/keyboard hooks                                      |
| **GlobalHotKeyCore**                | Global hotkey registration                                       |
| **WpfScreenHelper**                 | Multi-screen DPI awareness                                       |
| **WindowsDisplayAPI**               | Display/monitor API                                              |
| **PInvoke.SHCore**                  | Windows P/Invoke interop                                         |
| **XamlAnimatedGif**                 | GIF animation support                                            |
| **Inno Setup**                      | Standalone installer creation                                    |

***

## Development

### Prerequisites

- **Visual Studio 2022+** (with .NET Desktop Development workload)
- **.NET 9.0 SDK**
- Windows 10 (Build 17763+) or Windows 11

***

### Build Configurations

| Configuration | Description                                    |
| ------------- | ---------------------------------------------- |
| `Debug`       | Debug build, for development                   |
| `Release`     | Release build                                  |
| `Standalone`  | Standalone build (non-Microsoft Store version) |

### Debugging

Set `SnapIt` as the startup project and press **F5** to run. The application runs as a standalone WPF window.

### Logging

Logs are written to the application data directory via **Serilog** for troubleshooting.

***

## CI/CD

- **CI Pipeline**: Triggered on push/PR to `main` — builds all configurations
- **Release Pipeline**: Manually triggered — automatic versioning, Inno Setup packaging, GitHub Release creation

## Contributing

Contributions and issues are welcome!

### Getting Started

1. Fork this repository
2. Create a new branch for your changes
3. Commit your changes
4. Submit a Pull Request

Please follow the code style and conventions used in the project.

***

## License

[GNU General Public License v3.0](LICENSE)

**Original Author**: Engin KIRMACI
**Maintainer**: [Ghost-Girls](https://github.com/Ghost-Girls)

***

*Made for Windows power users who value a tidy desktop.*
