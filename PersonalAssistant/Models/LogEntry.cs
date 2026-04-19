namespace PersonalAssistant.Models;

public class LogEntry
{
    public int Id { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = "manual";
}
