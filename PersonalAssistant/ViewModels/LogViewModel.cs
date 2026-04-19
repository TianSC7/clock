using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using PersonalAssistant.Core;
using PersonalAssistant.Models;

namespace PersonalAssistant.ViewModels;

public class LogViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _db;

    public ObservableCollection<LogEntry> TodayLogs { get; } = new();
    public ObservableCollection<LogGroup> LogGroups { get; } = new();

    private string _newContent = string.Empty;
    private string _selectedDate = DateTime.Now.ToString("yyyy-MM-dd");

    public string NewContent
    {
        get => _newContent;
        set { _newContent = value; OnPropertyChanged(); AddCommand.RaiseCanExecuteChanged(); }
    }

    public string SelectedDate
    {
        get => _selectedDate;
        set { _selectedDate = value; OnPropertyChanged(); LoadLogs(); }
    }

    public RelayCommand AddCommand { get; }
    public RelayCommand ExportCommand { get; }

    public LogViewModel(DatabaseService db)
    {
        _db = db;
        AddCommand = new RelayCommand(_ => AddLog(), _ => !string.IsNullOrWhiteSpace(NewContent));
        ExportCommand = new RelayCommand(_ => ExportLogs());
        LoadLogs();
    }

    public void LoadLogs()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            TodayLogs.Clear();
            LogGroups.Clear();

            var entries = _db.GetLogsByDate(SelectedDate);
            foreach (var entry in entries)
            {
                TodayLogs.Add(entry);
            }

            var allDates = _db.GetLogDates();
            foreach (var date in allDates)
            {
                var logs = _db.GetLogsByDate(date);
                LogGroups.Add(new LogGroup { Date = date, Entries = new List<LogEntry>(logs) });
            }
        });
    }

    private void AddLog()
    {
        _db.AddLog(NewContent, "manual");
        NewContent = string.Empty;
        LoadLogs();
    }

    private void ExportLogs()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = $"日志_{SelectedDate}",
            DefaultExt = ".txt",
            Filter = "文本文件|*.txt"
        };

        if (dialog.ShowDialog() == true)
        {
            var lines = TodayLogs.Select(e => $"[{e.Time}] ({e.Source}) {e.Content}");
            System.IO.File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine, lines));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class LogGroup
{
    public string Date { get; set; } = string.Empty;
    public List<LogEntry> Entries { get; set; } = new();
}
