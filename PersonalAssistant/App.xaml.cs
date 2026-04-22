using System.Windows;
using System.Windows.Threading;
using PersonalAssistant.Core;
using PersonalAssistant.ViewModels;
using PersonalAssistant.Views;

namespace PersonalAssistant;

public partial class App : System.Windows.Application
{
    private static readonly Mutex Mutex = new(true, "PersonalAssistant_SingleInstance");
    private SettingsService _settings = null!;
    private DatabaseService _db = null!;
    private AppTimer _timer = null!;
    private NotificationService _notification = null!;
    private PomodoroViewModel _pomodoroVm = null!;
    private TodoViewModel _todoVm = null!;
    private LogViewModel _logVm = null!;
    private FloatingWindow _floatingWindow = null!;
    private RestOverlayWindow? _restOverlay;
    private MainWindow? _mainWindow;
    private System.Windows.Forms.NotifyIcon _notifyIcon = null!;
    private System.Windows.Forms.ToolStripMenuItem _toggleFloatItem = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        if (!Mutex.WaitOne(TimeSpan.Zero, true))
        {
            System.Windows.MessageBox.Show("程序已在运行中。");
            Shutdown();
            return;
        }

        base.OnStartup(e);

        _settings = new SettingsService();
        _settings.Load();

        _db = new DatabaseService();
        _db.Initialize();

        _timer = new AppTimer(_settings);
        _notification = new NotificationService(_settings);

        _pomodoroVm = new PomodoroViewModel(_timer, _db, _notification, _settings);
        _todoVm = new TodoViewModel(_db);
        _logVm = new LogViewModel(_db);

        _timer.PhaseChanged += OnPhaseChanged;
        _timer.InitializeScheduledMode();

        InitializeTrayIcon();
        ShowFloatingWindow();
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon();
        var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app.ico");
        _notifyIcon.Icon = System.IO.File.Exists(iconPath)
            ? new System.Drawing.Icon(iconPath)
            : System.Drawing.SystemIcons.Application;
        _notifyIcon.Text = "PersonalAssistant";
        _notifyIcon.Visible = true;

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();

        contextMenu.Items.Add("暂停/继续", null, (_, _) => TogglePause());
        contextMenu.Items.Add("跳过当前阶段", null, (_, _) => _timer.Skip());
        contextMenu.Items.Add("-");
        _toggleFloatItem = new System.Windows.Forms.ToolStripMenuItem("隐藏浮窗", null, (_, _) => ToggleFloatingWindow());
        contextMenu.Items.Add(_toggleFloatItem);
        contextMenu.Items.Add("打开主面板", null, (_, _) => ShowMainWindow());
        contextMenu.Items.Add("设置", null, (_, _) => ShowSettingsWindow());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("退出", null, (_, _) => ExitApp());

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();

        Properties["NotifyIcon"] = _notifyIcon;
    }

    private void ShowFloatingWindow()
    {
        _floatingWindow = new FloatingWindow(_pomodoroVm);
        _floatingWindow.Left = SystemParameters.WorkArea.Right - _floatingWindow.Width - 20;
        _floatingWindow.Top = SystemParameters.WorkArea.Top + 10;
        _floatingWindow.Opacity = _settings.Current.FloatWindowOpacity;
        _floatingWindow.Show();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow(_pomodoroVm, _todoVm, _logVm);
        }
        _mainWindow.Show();
        _mainWindow.Activate();
        _mainWindow.WindowState = WindowState.Normal;
    }

    private void ShowSettingsWindow()
    {
        var window = new Views.SettingsWindow(_settings, _timer);
        window.ShowDialog();

        if (_floatingWindow != null)
        {
            _floatingWindow.Opacity = _settings.Current.FloatWindowOpacity;
        }

        _pomodoroVm.RefreshSchedule();
    }

    private void TogglePause()
    {
        if (_timer.IsPaused)
            _timer.Resume();
        else
            _timer.Pause();
    }

    private void ToggleFloatingWindow()
    {
        if (_floatingWindow.IsVisible)
        {
            _floatingWindow.Hide();
            _toggleFloatItem.Text = "显示浮窗";
        }
        else
        {
            _floatingWindow.Show();
            _toggleFloatItem.Text = "隐藏浮窗";
        }
    }

    private void OnPhaseChanged(object? sender, Models.PhaseChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (e.NewPhase == Models.TimerPhase.Break)
            {
                _restOverlay = new RestOverlayWindow(_pomodoroVm);
                _restOverlay.Show();
            }
            else if (e.NewPhase == Models.TimerPhase.Focus)
            {
                _restOverlay?.Close();
                _restOverlay = null;
            }
        });
    }

    private void ExitApp()
    {
        _timer.Reset();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _floatingWindow.Close();
        _restOverlay?.Close();
        if (_mainWindow != null)
        {
            _mainWindow.ForceClose();
        }
        Mutex.ReleaseMutex();
        Shutdown();
    }
}
