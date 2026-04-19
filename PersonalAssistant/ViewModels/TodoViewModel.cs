using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using PersonalAssistant.Core;
using PersonalAssistant.Models;

namespace PersonalAssistant.ViewModels;

public class TodoViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _db;

    public ObservableCollection<TodoItem> Todos { get; } = new();
    public ObservableCollection<TodoItem> CompletedTodos { get; } = new();

    private string _newTitle = string.Empty;
    private int _newPriority = 1;
    private string? _newDueDate;
    private bool _showCompleted;

    public string NewTitle
    {
        get => _newTitle;
        set { _newTitle = value; OnPropertyChanged(); AddCommand.RaiseCanExecuteChanged(); }
    }

    public int NewPriority
    {
        get => _newPriority;
        set { _newPriority = value; OnPropertyChanged(); }
    }

    public string? NewDueDate
    {
        get => _newDueDate;
        set { _newDueDate = value; OnPropertyChanged(); }
    }

    public bool ShowCompleted
    {
        get => _showCompleted;
        set { _showCompleted = value; OnPropertyChanged(); }
    }

    public RelayCommand AddCommand { get; }
    public RelayCommand<TodoItem> ToggleDoneCommand { get; }
    public RelayCommand<TodoItem> DeleteCommand { get; }

    public TodoViewModel(DatabaseService db)
    {
        _db = db;
        AddCommand = new RelayCommand(_ => AddTodo(), _ => !string.IsNullOrWhiteSpace(NewTitle));
        ToggleDoneCommand = new RelayCommand<TodoItem>(item => ToggleDone(item));
        DeleteCommand = new RelayCommand<TodoItem>(item => DeleteTodo(item));
        LoadTodos();
    }

    public void LoadTodos()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            Todos.Clear();
            CompletedTodos.Clear();
            var all = _db.GetAllTodos();
            foreach (var item in all)
            {
                if (item.IsDone)
                    CompletedTodos.Add(item);
                else
                    Todos.Add(item);
            }
        });
    }

    private void AddTodo()
    {
        var item = new TodoItem
        {
            Title = NewTitle,
            Priority = NewPriority,
            DueDate = NewDueDate,
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        _db.AddTodo(item);
        NewTitle = string.Empty;
        NewDueDate = null;
        NewPriority = 1;
        LoadTodos();
    }

    public void ToggleDone(TodoItem? item)
    {
        if (item == null) return;
        item.IsDone = !item.IsDone;
        item.DoneAt = item.IsDone ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : null;
        _db.UpdateTodo(item);
        if (item.IsDone)
        {
            _db.AddLog($"完成待办：{item.Title}", "todo");
        }
        LoadTodos();
    }

    private void DeleteTodo(TodoItem? item)
    {
        if (item == null) return;
        _db.DeleteTodo(item.Id);
        LoadTodos();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class RelayCommand<T> : System.Windows.Input.ICommand
{
    private readonly Action<T?> _execute;
    private readonly Predicate<T?>? _canExecute;

    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => _execute((T?)parameter);
    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
