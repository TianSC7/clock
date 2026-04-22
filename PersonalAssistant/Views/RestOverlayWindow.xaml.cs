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

    private void EmergencyButton_Click(object sender, RoutedEventArgs e)
    {
        ShowEmergencyDialog();
    }

    private void ShowEmergencyDialog()
    {
        var dialog = new Window
        {
            Title = "紧急情况验证",
            Width = 360,
            Height = 220,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true
        };

        var border = new System.Windows.Controls.Border
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2C, 0x3E, 0x50)),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(32)
        };

        var stackPanel = new System.Windows.Controls.StackPanel();

        var title = new System.Windows.Controls.TextBlock
        {
            Text = "\u26a0 紧急情况验证",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        };
        stackPanel.Children.Add(title);

        var hint = new System.Windows.Controls.TextBlock
        {
            Text = "请输入密码以解除休息",
            FontSize = 13,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF)),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        };
        stackPanel.Children.Add(hint);

        var passwordBox = new System.Windows.Controls.TextBox
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            MaxLength = 4,
            Margin = new Thickness(0, 0, 0, 16)
        };
        stackPanel.Children.Add(passwordBox);

        var errorMsg = new System.Windows.Controls.TextBlock
        {
            Text = "密码错误",
            FontSize = 12,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x6B, 0x6B)),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Visibility = Visibility.Collapsed
        };
        stackPanel.Children.Add(errorMsg);

        border.Child = stackPanel;
        dialog.Content = border;

        dialog.Loaded += (_, _) => { _topmostTimer.Stop(); passwordBox.Focus(); };
        dialog.Closed += (_, _) => { if (!_forceClosed) _topmostTimer.Start(); };
        passwordBox.KeyDown += (_, ke) =>
        {
            if (ke.Key == System.Windows.Input.Key.Enter)
            {
                var todayPassword = DateTime.Now.ToString("MMdd");
                if (passwordBox.Text == todayPassword)
                {
                    ForceClose();
                    dialog.Close();
                }
                else
                {
                    errorMsg.Visibility = Visibility.Visible;
                    passwordBox.Clear();
                }
            }
        };

        dialog.Show();
    }

    private void ForceClose()
    {
        _topmostTimer.Stop();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _forceClosed = true;
        _viewModel.PhaseDisplay = "已跳过休息";
        Close();
    }

    private bool _forceClosed;

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_forceClosed)
        {
            base.OnClosing(e);
            return;
        }

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
