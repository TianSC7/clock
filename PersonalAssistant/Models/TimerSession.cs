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
