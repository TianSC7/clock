using System.Windows;
using System.Windows.Controls;
using PersonalAssistant.Core;
using PersonalAssistant.Models;
using WpfOrientation = System.Windows.Controls.Orientation;

namespace PersonalAssistant.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly AppTimer _timer;
    private readonly WorkSchedule _editSchedule;
    private int _focusMinutes;
    private int _breakMinutes;
    private int _waterReminderMinutes;
    private bool _enableSound;
    private bool _enableWaterReminder;
    private double _floatWindowOpacity;
    private bool _autoStartWithWindows;

    public int FocusMinutes { get => _focusMinutes; set { _focusMinutes = value; } }
    public int BreakMinutes { get => _breakMinutes; set { _breakMinutes = value; } }
    public int WaterReminderMinutes { get => _waterReminderMinutes; set { _waterReminderMinutes = value; } }
    public bool EnableSound { get => _enableSound; set { _enableSound = value; } }
    public bool EnableWaterReminder { get => _enableWaterReminder; set { _enableWaterReminder = value; } }
    public double FloatWindowOpacity { get => _floatWindowOpacity; set { _floatWindowOpacity = value; } }
    public bool AutoStartWithWindows { get => _autoStartWithWindows; set { _autoStartWithWindows = value; } }
    public WorkSchedule Schedule => _editSchedule;

    public SettingsWindow(SettingsService settingsService, AppTimer timer)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _timer = timer;

        var s = settingsService.Current;
        _focusMinutes = s.FocusMinutes;
        _breakMinutes = s.BreakMinutes;
        _waterReminderMinutes = s.WaterReminderMinutes;
        _enableSound = s.EnableSound;
        _enableWaterReminder = s.EnableWaterReminder;
        _floatWindowOpacity = s.FloatWindowOpacity;
        _autoStartWithWindows = s.AutoStartWithWindows;

        _editSchedule = new WorkSchedule
        {
            ScheduledMode = s.Schedule?.ScheduledMode ?? true,
            FocusMinutes = s.Schedule?.FocusMinutes ?? 45,
            BreakMinutes = s.Schedule?.BreakMinutes ?? 10,
            Slots = s.Schedule?.Slots.Select(slot => new TimeSlot
            {
                Start = slot.Start,
                End = slot.End,
                Enabled = slot.Enabled,
                Label = slot.Label
            }).ToList() ?? new WorkSchedule().Slots
        };

        DataContext = this;
        Loaded += (_, _) => { BuildSlotRows(); RefreshPreview(); };
    }

    private void BuildSlotRows()
    {
        SlotsPanel.Children.Clear();
        for (int i = 0; i < _editSchedule.Slots.Count; i++)
        {
            var slot = _editSchedule.Slots[i];
            var row = BuildSlotRow(slot, i);
            SlotsPanel.Children.Add(row);
        }
    }

    private Border BuildSlotRow(TimeSlot slot, int index)
    {
        var hours = Enumerable.Range(0, 24).ToList();
        var minutes = Enumerable.Range(0, 60).Where(m => m % 5 == 0).ToList();

        var border = new Border
        {
            Background = System.Windows.Media.Brushes.Transparent,
            Margin = new Thickness(0, 0, 0, 6)
        };

        var stack = new StackPanel { Orientation = WpfOrientation.Horizontal };

        var enableCb = new System.Windows.Controls.CheckBox
        {
            IsChecked = slot.Enabled,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        enableCb.Checked += (_, _) => { slot.Enabled = true; RefreshPreview(); };
        enableCb.Unchecked += (_, _) => { slot.Enabled = false; RefreshPreview(); };
        stack.Children.Add(enableCb);

        var labelTb = new System.Windows.Controls.TextBox
        {
            Text = slot.Label,
            Width = 50,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        labelTb.TextChanged += (_, _) => { slot.Label = labelTb.Text; };
        stack.Children.Add(labelTb);

        var startHourCombo = new System.Windows.Controls.ComboBox
        {
            Width = 48,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 2, 0),
            ItemsSource = hours,
            SelectedIndex = hours.IndexOf(slot.Start.Hour)
        };
        startHourCombo.SelectionChanged += (_, _) =>
        {
            if (startHourCombo.SelectedItem is int h)
            {
                slot.Start = new TimeOnly(h, slot.Start.Minute);
                RefreshPreview();
            }
        };
        stack.Children.Add(startHourCombo);

        stack.Children.Add(new System.Windows.Controls.TextBlock { Text = ":", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 2, 0) });

        var startMinuteCombo = new System.Windows.Controls.ComboBox
        {
            Width = 48,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0),
            ItemsSource = minutes,
            SelectedIndex = minutes.IndexOf(slot.Start.Minute)
        };
        startMinuteCombo.SelectionChanged += (_, _) =>
        {
            if (startMinuteCombo.SelectedItem is int m)
            {
                slot.Start = new TimeOnly(slot.Start.Hour, m);
                RefreshPreview();
            }
        };
        stack.Children.Add(startMinuteCombo);

        stack.Children.Add(new System.Windows.Controls.TextBlock { Text = "—", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) });

        var endHourCombo = new System.Windows.Controls.ComboBox
        {
            Width = 48,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 2, 0),
            ItemsSource = hours,
            SelectedIndex = hours.IndexOf(slot.End.Hour)
        };
        endHourCombo.SelectionChanged += (_, _) =>
        {
            if (endHourCombo.SelectedItem is int h)
            {
                slot.End = new TimeOnly(h, slot.End.Minute);
                RefreshPreview();
            }
        };
        stack.Children.Add(endHourCombo);

        stack.Children.Add(new System.Windows.Controls.TextBlock { Text = ":", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 2, 0) });

        var endMinuteCombo = new System.Windows.Controls.ComboBox
        {
            Width = 48,
            VerticalContentAlignment = VerticalAlignment.Center,
            ItemsSource = minutes,
            SelectedIndex = minutes.IndexOf(slot.End.Minute)
        };
        endMinuteCombo.SelectionChanged += (_, _) =>
        {
            if (endMinuteCombo.SelectedItem is int m)
            {
                slot.End = new TimeOnly(slot.End.Hour, m);
                RefreshPreview();
            }
        };
        stack.Children.Add(endMinuteCombo);

        border.Child = stack;
        return border;
    }

    private void SettingsTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.RadioButton rb && rb.Tag is string tag && int.TryParse(tag, out int idx))
        {
            PageGeneral.Visibility = idx == 0 ? Visibility.Visible : Visibility.Collapsed;
            PageSchedule.Visibility = idx == 1 ? Visibility.Visible : Visibility.Collapsed;
            if (idx == 1) { BuildSlotRows(); RefreshPreview(); }
        }
    }

    private void AddSlot_Click(object sender, RoutedEventArgs e)
    {
        _editSchedule.Slots.Add(new TimeSlot { Start = new TimeOnly(19, 0), End = new TimeOnly(21, 0), Label = "晚间" });
        BuildSlotRows();
        RefreshPreview();
    }

    private void RemoveSlot_Click(object sender, RoutedEventArgs e)
    {
        if (_editSchedule.Slots.Count > 1)
        {
            _editSchedule.Slots.RemoveAt(_editSchedule.Slots.Count - 1);
            BuildSlotRows();
            RefreshPreview();
        }
    }

    private void RefreshPreview()
    {
        try
        {
            var engine = new ScheduleEngine();
            var blocks = engine.BuildTodayPlan(_editSchedule);
            var focusBlocks = blocks.Where(b => b.Type == BlockType.Focus).ToList();

            if (focusBlocks.Count == 0)
            {
                PreviewText.Text = "无专注块（请检查时间段设置）";
                return;
            }

            var lines = new List<string>();
            int focusIndex = 1;
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].Type == BlockType.Focus)
                {
                    string line = $"{focusIndex}. {blocks[i].StartTime:HH:mm}-{blocks[i].EndTime:HH:mm} 专注";
                    
                    if (i + 1 < blocks.Count && blocks[i+1].Type == BlockType.Break)
                    {
                        line += $"\n   {blocks[i+1].StartTime:HH:mm}-{blocks[i+1].EndTime:HH:mm} 休息";
                    }
                    
                    lines.Add(line);
                    focusIndex++;
                }
            }

            PreviewText.Text = $"共 {focusBlocks.Count} 个专注块\n\n" +
                               string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            PreviewText.Text = $"预览出错：{ex.Message}";
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ApplySettings();
        DialogResult = true;
        Close();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        ApplySettings();
    }

    private void ApplySettings()
    {
        var s = _settingsService.Current;
        s.FocusMinutes = _focusMinutes;
        s.BreakMinutes = _breakMinutes;
        s.WaterReminderMinutes = _waterReminderMinutes;
        s.EnableSound = _enableSound;
        s.EnableWaterReminder = _enableWaterReminder;
        s.FloatWindowOpacity = _floatWindowOpacity;
        s.Schedule = _editSchedule;

        if (s.AutoStartWithWindows != _autoStartWithWindows)
        {
            SetAutoStart(_autoStartWithWindows);
            s.AutoStartWithWindows = _autoStartWithWindows;
        }

        _settingsService.Save();
        _timer.ReloadSettings();
        _timer.ReloadSchedule();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SetAutoStart(bool enable)
    {
        var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", true);
        var appName = "PersonalAssistant";
        var exePath = Environment.ProcessPath;

        if (enable && exePath != null)
            key?.SetValue(appName, $"\"{exePath}\"");
        else
            key?.DeleteValue(appName, false);
    }
}
