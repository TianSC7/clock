namespace PersonalAssistant.Models;

public class AppSettings
{
    public int FocusMinutes { get; set; } = 45;
    public int BreakMinutes { get; set; } = 10;
    public int WaterReminderMinutes { get; set; } = 30;
    public bool EnableSound { get; set; } = true;
    public bool EnableWaterReminder { get; set; } = true;
    public double FloatWindowOpacity { get; set; } = 0.85;
    public bool AutoStartWithWindows { get; set; } = false;
    public WorkSchedule? Schedule { get; set; } = new WorkSchedule();
}
