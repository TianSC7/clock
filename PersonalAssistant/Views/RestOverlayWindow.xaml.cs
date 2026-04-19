using System.ComponentModel;
using System.Windows;
using PersonalAssistant.ViewModels;

namespace PersonalAssistant.Views;

public partial class RestOverlayWindow : Window
{
    private readonly PomodoroViewModel _viewModel;

    public RestOverlayWindow(PomodoroViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (_viewModel.CurrentPhase != Models.TimerPhase.Break)
            {
                Close();
            }
        });
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_viewModel.CurrentPhase == Models.TimerPhase.Break)
        {
            e.Cancel = true;
        }
        base.OnClosing(e);
    }
}
