namespace PersonalAssistant.Models;

public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
    public string? DueDate { get; set; }
    public bool IsDone { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string? DoneAt { get; set; }
}
