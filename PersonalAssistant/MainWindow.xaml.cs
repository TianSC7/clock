using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using PersonalAssistant.ViewModels;

namespace PersonalAssistant;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class NullToCollapsingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public partial class MainWindow : Window
{
    public PomodoroViewModel Pomodoro { get; }
    public TodoViewModel Todo { get; }
    public LogViewModel Log { get; }

    public MainWindow(PomodoroViewModel pomodoro, TodoViewModel todo, LogViewModel log)
    {
        Pomodoro = pomodoro;
        Todo = todo;
        Log = log;
        InitializeComponent();
        DataContext = this;
    }

    private bool _forceClose;

    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_forceClose) return;
        e.Cancel = true;
        Hide();
    }

    private void Tab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.RadioButton rb && rb.Tag is string tag && int.TryParse(tag, out int idx))
        {
            PagePomodoro.Visibility = idx == 0 ? Visibility.Visible : Visibility.Collapsed;
            PageTodo.Visibility = idx == 1 ? Visibility.Visible : Visibility.Collapsed;
            PageLog.Visibility = idx == 2 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void TodoCheckBox_Clicked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.CheckBox cb && cb.DataContext is Models.TodoItem item)
        {
            Todo.ToggleDone(item);
        }
    }
}
