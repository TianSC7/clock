using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using PersonalAssistant.Helpers;
using PersonalAssistant.ViewModels;

namespace PersonalAssistant.Views;

public partial class FloatingWindow : Window
{
    public FloatingWindow(PomodoroViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdatePhaseVisibility(viewModel);

        Loaded += OnLoaded;
        Closed += OnWindowClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        WindowHelper.HideFromTaskbar(this);
        TopmostManager.Register(this, 1);
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        TopmostManager.Unregister(this);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PomodoroViewModel.PhaseDisplay) ||
            e.PropertyName == nameof(PomodoroViewModel.IsScheduledMode))
        {
            Dispatcher.Invoke(() => UpdatePhaseVisibility((PomodoroViewModel)DataContext));
        }
    }

    private void UpdatePhaseVisibility(PomodoroViewModel vm)
    {
        if (vm.IsScheduledMode)
        {
            PhaseLabel.Visibility = Visibility.Visible;
            Height = 58;
        }
        else
        {
            PhaseLabel.Visibility = Visibility.Collapsed;
            Height = 50;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
