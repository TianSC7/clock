namespace PersonalAssistant.Models;

public enum BlockType { Focus, Break }

public class TimeSlot
{
    public TimeOnly Start { get; set; } = new TimeOnly(9, 30);
    public TimeOnly End { get; set; } = new TimeOnly(12, 0);
    public bool Enabled { get; set; } = true;
    public string Label { get; set; } = "";
}

public class WorkBlock
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public BlockType Type { get; set; }
    public TimeOnly SlotStart { get; set; }
}

public class WorkSchedule
{
    public List<TimeSlot> Slots { get; set; } = new()
    {
        new TimeSlot { Start = new TimeOnly(9, 30), End = new TimeOnly(12, 0), Label = "上午" },
        new TimeSlot { Start = new TimeOnly(14, 0), End = new TimeOnly(18, 30), Label = "下午" }
    };
    public int FocusMinutes { get; set; } = 45;
    public int BreakMinutes { get; set; } = 10;
    public bool ScheduledMode { get; set; } = true;
}
