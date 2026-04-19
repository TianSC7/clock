using PersonalAssistant.Models;

namespace PersonalAssistant.Core;

public class ScheduleEngine
{
    public List<WorkBlock> BuildTodayPlan(WorkSchedule schedule)
    {
        var blocks = new List<WorkBlock>();
        var today = DateTime.Today;

        foreach (var slot in schedule.Slots.Where(s => s.Enabled))
        {
            var cursor = today.Add(slot.Start.ToTimeSpan());
            var slotEnd = today.Add(slot.End.ToTimeSpan());

            while (cursor < slotEnd)
            {
                var focusEnd = cursor.AddMinutes(schedule.FocusMinutes);
                if (focusEnd > slotEnd) break;

                blocks.Add(new WorkBlock
                {
                    StartTime = cursor,
                    EndTime = focusEnd,
                    Type = BlockType.Focus,
                    SlotStart = slot.Start
                });
                cursor = focusEnd;

                var breakEnd = cursor.AddMinutes(schedule.BreakMinutes);
                if (breakEnd > slotEnd)
                {
                    break;
                }

                blocks.Add(new WorkBlock
                {
                    StartTime = cursor,
                    EndTime = breakEnd,
                    Type = BlockType.Break,
                    SlotStart = slot.Start
                });
                cursor = breakEnd;
            }
        }

        return blocks;
    }

    public WorkBlock? FindCurrentBlock(List<WorkBlock> blocks)
    {
        var now = DateTime.Now;
        return blocks.FirstOrDefault(b => now >= b.StartTime && now < b.EndTime);
    }

    public WorkBlock? FindNextBlock(List<WorkBlock> blocks)
    {
        var now = DateTime.Now;
        return blocks.FirstOrDefault(b => b.StartTime > now);
    }

    public (string status, string detail) GetGapStatus(List<WorkBlock> blocks, WorkSchedule schedule)
    {
        var now = DateTime.Now;
        var enabledSlots = schedule.Slots.Where(s => s.Enabled).OrderBy(s => s.Start).ToList();

        if (enabledSlots.Count == 0)
            return ("今日无计划", "");

        var firstSlotStart = DateTime.Today.Add(enabledSlots[0].Start.ToTimeSpan());
        if (now < firstSlotStart)
        {
            var diff = firstSlotStart - now;
            return ("等待开始", $"距 {enabledSlots[0].Label} 还差 {(int)diff.TotalMinutes} 分钟");
        }

        if (blocks.Count == 0)
            return ("今日无计划", "");

        var lastEnd = blocks.Max(b => b.EndTime);
        if (now >= lastEnd)
            return ("今日结束", "已完成所有计划");

        for (int i = 0; i < enabledSlots.Count; i++)
        {
            var slot = enabledSlots[i];
            var slotStartDt = DateTime.Today.Add(slot.Start.ToTimeSpan());
            var slotEndDt = DateTime.Today.Add(slot.End.ToTimeSpan());

            if (i > 0)
            {
                var prev = enabledSlots[i - 1];
                var prevEndDt = DateTime.Today.Add(prev.End.ToTimeSpan());
                if (now >= prevEndDt && now < slotStartDt)
                {
                    var diff = slotStartDt - now;
                    return ("午休中", $"{slot.Label} {slot.Start:HH:mm} 恢复，还有 {(int)diff.TotalMinutes} 分钟");
                }
            }
        }

        var next = FindNextBlock(blocks);
        if (next != null)
        {
            var diff = next.StartTime - now;
            return ("空闲", $"下个专注块 {(int)diff.TotalMinutes} 分钟后开始");
        }

        return ("今日结束", "");
    }
}
