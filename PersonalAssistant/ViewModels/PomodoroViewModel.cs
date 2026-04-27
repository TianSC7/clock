using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using PersonalAssistant.Core;
using PersonalAssistant.Models;

namespace PersonalAssistant.ViewModels;

public class PomodoroViewModel : INotifyPropertyChanged
{
    private readonly AppTimer _timer;
    private readonly DatabaseService _db;
    private readonly NotificationService _notification;
    private readonly SettingsService _settings;

    private string _timeDisplay = "00:00";
    private string _phaseDisplay = "空闲";
    private bool _isRunning;
    private bool _isPaused;
    private int _completedCount;
    private string _scheduleStatus = "";
    private string _scheduleDetail = "";
    private string _nextBlockInfo = "";
    private int _totalFocusBlocks;
    private string _todayFocusTime = "0分钟";
    private string _todayBreakTime = "0分钟";

    public string TimeDisplay
    {
        get => _timeDisplay;
        set { _timeDisplay = value; OnPropertyChanged(); }
    }

    public string PhaseDisplay
    {
        get => _phaseDisplay;
        set { _phaseDisplay = value; OnPropertyChanged(); }
    }

    public bool IsRunning
    {
        get => _isRunning;
        set { _isRunning = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowFreeControls)); }
    }

    public bool IsPaused
    {
        get => _isPaused;
        set { _isPaused = value; OnPropertyChanged(); }
    }

    public int CompletedCount
    {
        get => _completedCount;
        set { _completedCount = value; OnPropertyChanged(); }
    }

    public int TotalFocusBlocks
    {
        get => _totalFocusBlocks;
        set { _totalFocusBlocks = value; OnPropertyChanged(); }
    }

    public string TodayFocusTime
    {
        get => _todayFocusTime;
        set { _todayFocusTime = value; OnPropertyChanged(); }
    }

    public string TodayBreakTime
    {
        get => _todayBreakTime;
        set { _todayBreakTime = value; OnPropertyChanged(); }
    }

    public string ScheduleStatus
    {
        get => _scheduleStatus;
        set { _scheduleStatus = value; OnPropertyChanged(); }
    }

    public string ScheduleDetail
    {
        get => _scheduleDetail;
        set { _scheduleDetail = value; OnPropertyChanged(); }
    }

    public string NextBlockInfo
    {
        get => _nextBlockInfo;
        set { _nextBlockInfo = value; OnPropertyChanged(); }
    }

    public bool IsScheduledMode => _timer.IsScheduledMode;
    public bool ShowFreeControls => !_timer.IsScheduledMode && !IsRunning;
    public TimerPhase CurrentPhase => _timer.CurrentPhase;

    public RelayCommand StartCommand { get; }
    public RelayCommand PauseCommand { get; }
    public RelayCommand ResumeCommand { get; }
    public RelayCommand SkipCommand { get; }
    public RelayCommand ResetCommand { get; }

    public PomodoroViewModel(AppTimer timer, DatabaseService db, NotificationService notification, SettingsService settings)
    {
        _timer = timer;
        _db = db;
        _notification = notification;
        _settings = settings;

        StartCommand = new RelayCommand(_ => Start(), _ => !IsRunning && !IsScheduledMode);
        PauseCommand = new RelayCommand(_ => Pause(), _ => IsRunning && !IsPaused && !IsScheduledMode);
        ResumeCommand = new RelayCommand(_ => Resume(), _ => IsPaused && !IsScheduledMode);
        SkipCommand = new RelayCommand(_ => Skip(), _ => IsRunning && !IsScheduledMode);
        ResetCommand = new RelayCommand(_ => Reset(), _ => IsRunning && !IsScheduledMode);

        _timer.Tick += OnTick;
        _timer.PhaseChanged += OnPhaseChanged;
        _timer.WaterReminderDue += OnWaterReminder;
        _timer.ScheduleInfoChanged += OnScheduleInfoChanged;

        CompletedCount = _db.GetCompletedFocusCountToday();
        RefreshDailyStats();

        if (IsScheduledMode)
        {
            TotalFocusBlocks = _timer.TotalFocusBlocks;
            UpdateNextBlockInfo();
        }
    }

    private void Start()
    {
        _timer.Start();
        IsRunning = true;
        IsPaused = false;
    }

    private void Pause()
    {
        _timer.Pause();
        IsPaused = true;
    }

    private void Resume()
    {
        _timer.Resume();
        IsPaused = false;
    }

    private void Skip()
    {
        _timer.Skip();
    }

    private void Reset()
    {
        _timer.Reset();
        IsRunning = false;
        IsPaused = false;
        TimeDisplay = "00:00";
        PhaseDisplay = "空闲";
    }

    private void OnTick(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            TimeDisplay = _timer.Remaining.ToString(@"mm\:ss");
        });
    }

    private void OnPhaseChanged(object? sender, PhaseChangedEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            PhaseDisplay = e.NewPhase switch
            {
                TimerPhase.Focus => "专注中",
                TimerPhase.Break => "休息中",
                _ => IsScheduledMode ? _timer.ScheduleStatus : "空闲"
            };

            if (e.OldPhase == TimerPhase.Focus && e.NewPhase == TimerPhase.Break)
            {
                CompletedCount = _timer.CompletedCycles;
                _db.AddLog($"完成第 {_timer.CompletedCycles} 个番茄钟", "pomodoro");
                _notification.NotifyFocusComplete(_timer.CompletedCycles);
                RefreshDailyStats();
            }
            else if (e.OldPhase == TimerPhase.Break && e.NewPhase == TimerPhase.Focus)
            {
                _notification.NotifyBreakComplete();
            }

            IsRunning = e.NewPhase != TimerPhase.Idle;
            IsPaused = false;
            UpdateNextBlockInfo();
        });
    }

    private void OnScheduleInfoChanged(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            ScheduleStatus = _timer.ScheduleStatus;
            ScheduleDetail = _timer.ScheduleDetail;
            TotalFocusBlocks = _timer.TotalFocusBlocks;
            UpdateNextBlockInfo();

            if (!IsRunning && IsScheduledMode)
            {
                PhaseDisplay = _timer.ScheduleStatus;
                TimeDisplay = "00:00";
            }
        });
    }

    private void UpdateNextBlockInfo()
    {
        if (!IsScheduledMode) return;

        var schedule = _settings.Current.Schedule;
        if (schedule == null) return;

        var engine = new ScheduleEngine();
        var next = engine.FindNextBlock(_timer.TodayPlan);
        if (next != null)
        {
            NextBlockInfo = $"下个：{next.StartTime:HH:mm} {(next.Type == BlockType.Focus ? "专注" : "休息")}";
        }
        else
        {
            NextBlockInfo = "";
        }
    }

    private void OnWaterReminder(object? sender, EventArgs e)
    {
        _notification.NotifyWaterReminder();
    }

    public void RefreshSchedule()
    {
        TotalFocusBlocks = _timer.TotalFocusBlocks;
        UpdateNextBlockInfo();
        OnPropertyChanged(nameof(IsScheduledMode));
        OnPropertyChanged(nameof(ShowFreeControls));
    }

    public void RefreshDailyStats()
    {
        var stats = _db.GetDailyStats(DateTime.Now.ToString("yyyy-MM-dd"));
        TodayFocusTime = FormatDuration(stats.FocusMinutes);
        TodayBreakTime = FormatDuration(stats.BreakMinutes);
    }

    private static string FormatDuration(int totalMinutes)
    {
        if (totalMinutes == 0) return "0分钟";
        var hours = totalMinutes / 60;
        var mins = totalMinutes % 60;
        if (hours > 0 && mins > 0) return $"{hours}小时{mins}分钟";
        if (hours > 0) return $"{hours}小时";
        return $"{mins}分钟";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
