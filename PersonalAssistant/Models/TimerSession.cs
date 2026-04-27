namespace PersonalAssistant.Models;

public enum TimerPhase
{
    Idle,
    Focus,
    Break
}

public class PhaseChangedEventArgs : EventArgs
{
    public TimerPhase OldPhase { get; init; }
    public TimerPhase NewPhase { get; init; }
}

public class DailyStats
{
    public string Date { get; set; } = string.Empty;
    public int FocusMinutes { get; set; }
    public int BreakMinutes { get; set; }
    public int CompletedFocus { get; set; }
    public int TotalFocusSessions { get; set; }
    public int CompletedBreak { get; set; }
}
