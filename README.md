# PersonalAssistant

A .NET 8.0 Windows system tray application with Pomodoro timer, Todo list, and Daily log features.

## Features

- **Pomodoro Timer**: 45min focus → 10min break cycle (configurable)
- **Floating Window**: Always-on-top countdown display with transparency control
- **System Tray**: Pause/resume, skip phase, open main panel
- **Rest Overlay**: Full-screen blocking overlay during break time
- **Todo List**: Priority-based todo with due dates
- **Daily Log**: Manual entries + auto-logging of completed pomodoros and todos
- **Water Reminder**: Configurable reminder every 30 minutes
- **Scheduled Mode**: Plan your day with focus/break blocks

## Tech Stack

- .NET 8.0 Windows (`net8.0-windows`)
- WPF + WinForms (NotifyIcon)
- Microsoft.Data.Sqlite (SQLite local storage)

## Project Structure

```
PersonalAssistant/
├── App.xaml.cs              # Entry point, tray icon, single-instance mutex
├── Core/
│   ├── AppTimer.cs          # Timer engine, state machine
│   ├── DatabaseService.cs   # SQLite CRUD
│   ├── NotificationService.cs
│   └── SettingsService.cs
├── Models/
│   ├── AppSettings.cs
│   ├── TimerSession.cs
│   ├── TodoItem.cs
│   ├── LogEntry.cs
│   └── WorkSchedule.cs
├── ViewModels/
│   ├── PomodoroViewModel.cs
│   ├── TodoViewModel.cs
│   └── LogViewModel.cs
├── Views/
│   ├── FloatingWindow.xaml  # Always-on-top timer display
│   ├── MainWindow.xaml       # Main panel (todo + log tabs)
│   ├── RestOverlayWindow.xaml
│   └── SettingsWindow.xaml
├── Helpers/
│   └── WindowHelper.cs
└── Assets/
    └── app.ico
```

## Build

```bash
cd PersonalAssistant
dotnet publish -c Release -r win-x64 --self-contained false
```

Output: `PersonalAssistant/bin/Release/net8.0-windows/win-x64/PersonalAssistant.exe`

Requires .NET 8.0 Runtime installed.

## Settings

Stored in `%AppData%\PersonalAssistant\settings.json`:
- FocusMinutes, BreakMinutes
- WaterReminderMinutes
- EnableSound, EnableWaterReminder
- FloatWindowOpacity
- AutoStartWithWindows
