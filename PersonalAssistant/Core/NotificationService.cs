using System.Media;
using System.Windows;

namespace PersonalAssistant.Core;

public class NotificationService
{
    private readonly SettingsService _settings;

    public NotificationService(SettingsService settings)
    {
        _settings = settings;
    }

    public void ShowNotification(string title, string message)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (System.Windows.Application.Current.MainWindow is { } window)
            {
                var balloonTitle = title;
                var balloonText = message;
                var timeout = 5000;

                var notifyIcon = (System.Windows.Forms.NotifyIcon?)System.Windows.Application.Current.Properties["NotifyIcon"];
                notifyIcon?.ShowBalloonTip(timeout, balloonTitle, balloonText, System.Windows.Forms.ToolTipIcon.Info);
            }
        });
    }

    public void PlayNotificationSound()
    {
        if (!_settings.Current.EnableSound) return;

        try
        {
            SystemSounds.Exclamation.Play();
        }
        catch
        {
        }
    }

    public void NotifyFocusComplete(int cycleCount = 0)
    {
        ShowNotification("专注完成", $"恭喜！已完成第 {cycleCount} 个番茄钟，休息一下吧。");
        PlayNotificationSound();
    }

    public void NotifyBreakComplete()
    {
        ShowNotification("休息结束", "休息时间结束，开始新的专注吧！");
        PlayNotificationSound();
    }

    public void NotifyWaterReminder()
    {
        ShowNotification("喝水提醒", "该喝水了！保持健康的工作节奏。");
    }

    public void NotifyTodoDue(string title)
    {
        ShowNotification("待办到期", $"待办事项即将到期：{title}");
    }
}
