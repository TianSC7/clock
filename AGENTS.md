# AGENTS.md

Guide for AI agents working in this repository.

## Project Overview

PersonalAssistant — a .NET 8.0 Windows system tray app with Pomodoro timer, Todo list, and Daily log. Built with WPF + WinForms (NotifyIcon) + SQLite. The UI language is Chinese (Simplified).

## Build & Run Commands

```bash
# Build (kill running instance first)
taskkill /IM PersonalAssistant.exe /F 2>nul; cd PersonalAssistant && dotnet build

# Run debug build
PersonalAssistant/bin/Debug/net8.0-windows/PersonalAssistant.exe

# Publish single-file release
cd PersonalAssistant && dotnet publish -c Release -r win-x64 --self-contained false
```

Output path: `PersonalAssistant/bin/Release/net8.0-windows/win-x64/PersonalAssistant.exe`

There are **no tests** in this project. There is **no lint or format command**.

## Tech Stack

- **Framework**: .NET 8.0 (`net8.0-windows`)
- **UI**: WPF (`<UseWPF>true</UseWPF>`) with WinForms for NotifyIcon (`<UseWindowsForms>true</UseWindowsForms>`)
- **Database**: Microsoft.Data.Sqlite (SQLite, stored at `%AppData%/PersonalAssistant/data.db`)
- **Settings**: JSON file at `%AppData%/PersonalAssistant/settings.json`
- **Packaging**: PublishSingleFile, framework-dependent (not self-contained)

## Solution Structure

```
clock.sln                              # Solution file
PersonalAssistant/                     # Single project
├── App.xaml / App.xaml.cs             # Entry point, global styles, tray icon, single-instance
├── PersonalAssistant.csproj           # Project config
├── Core/
│   ├── AppTimer.cs                    # Timer state machine (free mode + scheduled mode)
│   ├── ScheduleEngine.cs              # Builds daily work plan from time slots
│   ├── DatabaseService.cs             # SQLite CRUD for todos, logs, sessions
│   ├── NotificationService.cs         # Balloon tips + system sounds
│   └── SettingsService.cs             # JSON config load/save
├── Models/
│   ├── AppSettings.cs                 # Settings model (includes WorkSchedule)
│   ├── TimerSession.cs                # TimerPhase enum, PhaseChangedEventArgs, BlockType enum
│   ├── WorkSchedule.cs                # TimeSlot, WorkBlock, WorkSchedule
│   ├── TodoItem.cs                    # Todo model
│   └── LogEntry.cs                    # Log model
├── ViewModels/
│   ├── PomodoroViewModel.cs           # Timer UI state + RelayCommand class
│   ├── TodoViewModel.cs               # Todo CRUD + RelayCommand<T>
│   └── LogViewModel.cs                # Log list + export
├── Views/
│   ├── FloatingWindow.xaml/.cs        # Always-on-top transparent countdown
│   ├── RestOverlayWindow.xaml/.cs     # Full-screen break overlay (uncloseable)
│   └── SettingsWindow.xaml/.cs        # Two-tab settings (general + work schedule)
├── Helpers/
│   ├── WindowHelper.cs                # Win32 API (click-through, tool window)
│   └── TopmostManager.cs              # Centralized topmost window manager with priority-based ordering
└── Assets/
    └── app.ico                        # App icon
```

## Code Patterns & Conventions

### Architecture

- **MVVM pattern**: Models → ViewModels (implement `INotifyPropertyChanged`) → Views (XAML with code-behind for edge cases)
- **No DI framework**: All services are manually instantiated in `App.xaml.cs.OnStartup` and passed via constructors
- **Single-instance app**: Uses named `Mutex` ("PersonalAssistant_SingleInstance") to prevent duplicate processes
- **MainWindow is a singleton**: Stored as `_mainWindow` field in App; closing hides it instead of destroying it. `ForceClose()` bypasses the cancel logic for app exit.

### Naming

- **Namespace**: `PersonalAssistant` (root), `PersonalAssistant.Core`, `PersonalAssistant.Models`, `PersonalAssistant.ViewModels`, `PersonalAssistant.Views`, `PersonalAssistant.Helpers`
- **Private fields**: `_camelCase` (e.g., `_timer`, `_settings`, `_mainWindow`)
- **Properties**: `PascalCase` (e.g., `CurrentPhase`, `TimeDisplay`)
- **Events**: `PascalCase` (e.g., `PhaseChanged`, `Tick`)

### ViewModels

- Implement `INotifyPropertyChanged` with `OnPropertyChanged([CallerMemberName])`
- Use custom `RelayCommand` and `RelayCommand<T>` classes (defined in PomodoroViewModel.cs and TodoViewModel.cs respectively) instead of community libraries
- Call `RaiseCanExecuteChanged()` in property setters that affect command availability (e.g., `NewTitle` setter calls `AddCommand.RaiseCanExecuteChanged()`)

### WPF/XAML

- **Global styles** defined in `App.xaml` Resources — colors, brushes, fonts, and full control templates for Button, TextBox, ComboBox, CheckBox, ListBox, Slider, Expander, RadioButton
- **Color system**: Primary `#FF6B6B`, Accent `#4ECDC4`, Success `#4CAF50`, Text `#2C3E50`/`#7F8C8D`
- **Font**: Main = Segoe UI, Mono = Consolas
- **Custom value converters** in `MainWindow.xaml.cs`: `BoolToVisibilityConverter`, `InverseBoolToVisibilityConverter`, `NullToCollapsingConverter`
- **Tab navigation** uses `RadioButton` with `TabRadioStyle` (pill-shaped) + code-behind `Tab_Click` to toggle page visibility — not a TabControl
- **Shutdown mode**: `OnExplicitShutdown` — app lives in tray, exits only via tray menu

### Timer Engine (AppTimer)

- Uses `System.Timers.Timer` (1-second interval) — callbacks run on thread pool, UI updates must use `Dispatcher.Invoke`
- Two modes: **Free mode** (manual start/pause/resume/skip/reset) and **Scheduled mode** (auto-runs based on WorkSchedule time slots)
- Scheduled mode uses a separate `ScheduleWatcher` timer (2-second poll) to detect block transitions
- State machine: `Idle` → `Focus` ↔ `Break` (cyclic)

### Database (DatabaseService)

- Raw ADO.NET with `Microsoft.Data.Sqlite` — no ORM
- Each method opens its own connection (`using var conn = new SqliteConnection(...)`)
- Three tables: `todos`, `logs`, `sessions`
- Parameters use `@paramName` syntax with `AddWithValue`

## Gotchas & Important Details

1. **WPF + WinForms namespace conflicts**: `UseWindowsForms=true` causes type ambiguity. Use fully qualified names (e.g., `System.Windows.Controls.RadioButton`) or type aliases when needed.

2. **ComboBox initialization order**: Set `ItemsSource` before `SelectedIndex`/`SelectedItem`. Setting selection before items causes null/empty state.

3. **CheckBox + Command conflict**: Don't bind both `IsChecked` and `Command` on CheckBox — the binding and event handler will double-toggle. Use Click event in code-behind + call ViewModel method directly (see `TodoCheckBox_Clicked` in MainWindow.xaml.cs).

4. **TextBox text clipping**: PART_ContentHost needs `Margin="8,4"` to avoid clipping text. Don't use `VerticalAlignment="Center"` on PART_ContentHost alone — bind it to `VerticalContentAlignment`.

5. **Thread safety**: All `System.Timers.Timer` callbacks need `Dispatcher.Invoke()` for UI updates. ViewModels handle this internally.

6. **MainWindow close behavior**: The `Window_Closing` handler cancels close and hides the window. Only `ForceClose()` (called during app exit) bypasses this.

7. **NotifyIcon cleanup**: Must set `Visible = false` and call `Dispose()` on NotifyIcon before shutdown, otherwise the tray icon persists.

8. **CopyLocalLockFileAssemblies**: Set to `true` in csproj to ensure SQLite DLLs are copied to output. The SOP says `false` but the actual csproj uses `true`.

9. **UI is in Chinese**: All user-facing strings are in Chinese (e.g., "番茄钟", "待办事项", "专注中", "休息中"). Maintain this when modifying UI text.

## Key File References

| File | Key content |
|------|------------|
| `App.xaml` | Color system (lines 1-30), all control styles (lines 40-316) |
| `App.xaml.cs` | Mutex single-instance (line 11), MainWindow singleton (lines 90-98), tray icon (lines 55-78) |
| `MainWindow.xaml.cs` | Value converters, Tab_Click handler, TodoCheckBox_Clicked |
| `Core/AppTimer.cs` | Free mode + scheduled mode timer engine, ScheduleWatcher (line 50) |
| `Core/ScheduleEngine.cs` | BuildTodayPlan, FillBlocks, FindCurrentBlock, GetGapStatus |
| `Core/DatabaseService.cs` | All SQLite CRUD, table schemas in Initialize() |
| `ViewModels/PomodoroViewModel.cs` | RelayCommand class, timer UI bindings, Dispatcher.Invoke usage |
| `Views/SettingsWindow.xaml.cs` | BuildSlotRow — programmatic UI construction for schedule slots |
| `Views/FloatingWindow.xaml.cs` | Always-on-top transparent countdown, registers with TopmostManager (priority: 1) |
| `Views/RestOverlayWindow.xaml.cs` | Full-screen break overlay, registers with TopmostManager (priority: 2), emergency password dialog (priority: 3) |
| `Helpers/TopmostManager.cs` | Centralized topmost window management with priority-based ordering |
