using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using PersonalAssistant.Helpers;
using PersonalAssistant.ViewModels;

namespace PersonalAssistant.Views;

public partial class RestOverlayWindow : Window
{
    private readonly PomodoroViewModel _viewModel;
    private readonly DispatcherTimer _topmostTimer;

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

    [Flags]
    private enum EXECUTION_STATE : uint
    {
        ES_AWAYMODE_REQUIRED = 0x00000040,
        ES_CONTINUOUS = 0x80000000,
        ES_DISPLAY_REQUIRED = 0x00000002,
        ES_SYSTEM_REQUIRED = 0x00000001
    }

    public RestOverlayWindow(PomodoroViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        _topmostTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _topmostTimer.Tick += (_, _) => WindowHelper.MakeTopmostSticky(this);
        _topmostTimer.Start();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_SYSTEM_REQUIRED);
        WindowHelper.MakeTopmostSticky(this);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (_viewModel.CurrentPhase != Models.TimerPhase.Break)
            {
                Close();
            }
            else
            {
                WindowHelper.MakeTopmostSticky(this);
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

    protected override void OnClosed(EventArgs e)
    {
        _topmostTimer.Stop();
        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        base.OnClosed(e);
    }
}
