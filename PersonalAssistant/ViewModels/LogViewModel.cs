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
        set 
        { 
            if (_selectedDate == value || string.IsNullOrEmpty(value)) return;
            _selectedDate = value; 
            OnPropertyChanged(); 
            LoadLogs(false); 
        }
    }

    public RelayCommand AddCommand { get; }
    public RelayCommand ExportCommand { get; }

    public LogViewModel(DatabaseService db)
    {
        _db = db;
        AddCommand = new RelayCommand(_ => AddLog(), _ => !string.IsNullOrWhiteSpace(NewContent));
        ExportCommand = new RelayCommand(_ => ExportLogs());
        LoadLogs(true);
    }

    public void LoadLogs(bool reloadGroups = true)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            TodayLogs.Clear();
            var entries = _db.GetLogsByDate(SelectedDate);
            foreach (var entry in entries)
            {
                TodayLogs.Add(entry);
            }

            if (reloadGroups)
            {
                LogGroups.Clear();
                var allDates = _db.GetLogDates();
                // Ensure current date is in the list even if no logs exist yet
                if (!allDates.Contains(DateTime.Now.ToString("yyyy-MM-dd")))
                {
                    allDates.Insert(0, DateTime.Now.ToString("yyyy-MM-dd"));
                }
                foreach (var date in allDates)
                {
                    var logs = _db.GetLogsByDate(date);
                    LogGroups.Add(new LogGroup { Date = date, Entries = new List<LogEntry>(logs) });
                }
            }
        });
    }

    private void AddLog()
    {
        _db.AddLog(NewContent, "manual");
        NewContent = string.Empty;
        LoadLogs(true);
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
