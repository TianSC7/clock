using System.Windows;
using System.Windows.Threading;
using PersonalAssistant.Models;

namespace PersonalAssistant.Core;

public class AppTimer
{
    private readonly object _lock = new();
    private readonly System.Timers.Timer _timer;
    private readonly SettingsService _settings;
    private readonly ScheduleEngine _scheduleEngine = new();
    private TimeSpan _remaining;
    private DateTime _phaseStartTime;
    private TimeSpan _waterRemaining;
    private List<WorkBlock> _todayPlan = new();
    private WorkBlock? _currentBlock;
    private System.Timers.Timer? _scheduleWatcher;

    public TimerPhase CurrentPhase { get; private set; } = TimerPhase.Idle;
    public TimeSpan Remaining => _remaining;
    public int CompletedCycles { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsScheduledMode => _settings.Current.Schedule?.ScheduledMode ?? false;
    public List<WorkBlock> TodayPlan => _todayPlan;
    public WorkBlock? CurrentScheduledBlock => _currentBlock;
    public string ScheduleStatus { get; private set; } = "";
    public string ScheduleDetail { get; private set; } = "";
    public int TotalFocusBlocks => _todayPlan.Count(b => b.Type == BlockType.Focus);

    public event EventHandler<PhaseChangedEventArgs>? PhaseChanged;
    public event EventHandler? Tick;
    public event EventHandler? WaterReminderDue;
    public event EventHandler? ScheduleInfoChanged;

    public AppTimer(SettingsService settings)
    {
        _settings = settings;
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += OnTimerElapsed;
        _waterRemaining = TimeSpan.FromMinutes(settings.Current.WaterReminderMinutes);
    }

    public void InitializeScheduledMode()
    {
        if (!IsScheduledMode) return;

        var schedule = _settings.Current.Schedule ?? new WorkSchedule();
        _todayPlan = _scheduleEngine.BuildTodayPlan(schedule);

        _scheduleWatcher = new System.Timers.Timer(2000);
        _scheduleWatcher.Elapsed += OnScheduleWatch;
        _scheduleWatcher.Start();

        UpdateScheduleStatus();
    }

    private void OnScheduleWatch(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (!IsScheduledMode) return;

        lock (_lock)
        {
            var oldBlock = _currentBlock;
            _currentBlock = _scheduleEngine.FindCurrentBlock(_todayPlan);

            if (_currentBlock != null)
            {
                var targetPhase = _currentBlock.Type == BlockType.Focus ? TimerPhase.Focus : TimerPhase.Break;

                if (CurrentPhase != targetPhase || oldBlock != _currentBlock)
                {
                    CurrentPhase = targetPhase;
                    _phaseStartTime = _currentBlock.StartTime;
                    _remaining = _currentBlock.EndTime - DateTime.Now;
                    if (_remaining < TimeSpan.Zero) _remaining = TimeSpan.Zero;
                    IsPaused = false;

                    if (!_timer.Enabled)
                        _timer.Start();

                    PhaseChanged?.Invoke(this, new PhaseChangedEventArgs
                    {
                        OldPhase = oldBlock?.Type == BlockType.Focus ? TimerPhase.Focus :
                                   oldBlock?.Type == BlockType.Break ? TimerPhase.Break : TimerPhase.Idle,
                        NewPhase = targetPhase
                    });
                }
                else
                {
                    var newRemaining = _currentBlock.EndTime - DateTime.Now;
                    if (newRemaining < TimeSpan.Zero) newRemaining = TimeSpan.Zero;
                    _remaining = newRemaining;
                }
            }
            else
            {
                if (CurrentPhase != TimerPhase.Idle)
                {
                    var oldPhase = CurrentPhase;
                    CurrentPhase = TimerPhase.Idle;
                    _remaining = TimeSpan.Zero;
                    _timer.Stop();
                    PhaseChanged?.Invoke(this, new PhaseChangedEventArgs
                    {
                        OldPhase = oldPhase,
                        NewPhase = TimerPhase.Idle
                    });
                }
            }
        }

        UpdateScheduleStatus();
        Tick?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateScheduleStatus()
    {
        var schedule = _settings.Current.Schedule ?? new WorkSchedule();
        var (status, detail) = _scheduleEngine.GetGapStatus(_todayPlan, schedule);
        ScheduleStatus = status;
        ScheduleDetail = detail;
        ScheduleInfoChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Start()
    {
        if (IsScheduledMode) return;

        if (CurrentPhase == TimerPhase.Idle)
        {
            SwitchPhase(TimerPhase.Focus);
            IsPaused = false;
            _timer.Start();
        }
    }

    public void Pause()
    {
        if (IsScheduledMode) return;

        if (CurrentPhase != TimerPhase.Idle && !IsPaused)
        {
            IsPaused = true;
            _timer.Stop();
        }
    }

    public void Resume()
    {
        if (IsScheduledMode) return;

        if (CurrentPhase != TimerPhase.Idle && IsPaused)
        {
            IsPaused = false;
            _timer.Start();
        }
    }

    public void Skip()
    {
        if (IsScheduledMode) return;

        if (CurrentPhase == TimerPhase.Idle) return;

        if (CurrentPhase == TimerPhase.Focus)
            SwitchPhase(TimerPhase.Break);
        else
            SwitchPhase(TimerPhase.Focus);

        IsPaused = false;
        _timer.Start();
    }

    public void Reset()
    {
        _timer.Stop();
        var old = CurrentPhase;
        CurrentPhase = TimerPhase.Idle;
        _remaining = TimeSpan.Zero;
        IsPaused = false;
        PhaseChanged?.Invoke(this, new PhaseChangedEventArgs { OldPhase = old, NewPhase = TimerPhase.Idle });
    }

    private void SwitchPhase(TimerPhase newPhase)
    {
        var oldPhase = CurrentPhase;
        CurrentPhase = newPhase;
        _phaseStartTime = DateTime.Now;

        _remaining = newPhase == TimerPhase.Focus
            ? TimeSpan.FromMinutes(_settings.Current.FocusMinutes)
            : TimeSpan.FromMinutes(_settings.Current.BreakMinutes);

        _waterRemaining = TimeSpan.FromMinutes(_settings.Current.WaterReminderMinutes);

        PhaseChanged?.Invoke(this, new PhaseChangedEventArgs { OldPhase = oldPhase, NewPhase = newPhase });
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        lock (_lock)
        {
            if (_remaining > TimeSpan.Zero)
            {
                _remaining = _remaining.Subtract(TimeSpan.FromSeconds(1));
            }

            if (_settings.Current.EnableWaterReminder)
            {
                _waterRemaining = _waterRemaining.Subtract(TimeSpan.FromSeconds(1));
                if (_waterRemaining <= TimeSpan.Zero)
                {
                    _waterRemaining = TimeSpan.FromMinutes(_settings.Current.WaterReminderMinutes);
                    WaterReminderDue?.Invoke(this, EventArgs.Empty);
                }
            }

            if (_remaining <= TimeSpan.Zero)
            {
                if (IsScheduledMode)
                {
                    _timer.Stop();
                }
                else
                {
                    OnPhaseComplete();
                }
            }
        }

        Tick?.Invoke(this, EventArgs.Empty);
    }

    private void OnPhaseComplete()
    {
        _timer.Stop();

        if (CurrentPhase == TimerPhase.Focus)
        {
            CompletedCycles++;
            SwitchPhase(TimerPhase.Break);
        }
        else
        {
            SwitchPhase(TimerPhase.Focus);
        }

        IsPaused = false;
        _timer.Start();
    }

    public void ReloadSettings()
    {
        _waterRemaining = TimeSpan.FromMinutes(_settings.Current.WaterReminderMinutes);
    }

    public void ReloadSchedule()
    {
        var schedule = _settings.Current.Schedule ?? new WorkSchedule();
        _todayPlan = _scheduleEngine.BuildTodayPlan(schedule);
        UpdateScheduleStatus();
    }
}
