using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace RunHold;

public partial class StartupSplashWindow
{
    private static readonly TimeSpan DisplayDuration = TimeSpan.FromSeconds(3);
    private readonly DispatcherTimer timer;

    public StartupSplashWindow()
    {
        InitializeComponent();
        timer = new DispatcherTimer
        {
            Interval = DisplayDuration
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Close();
        };
        Loaded += OnLoaded;
    }

    public void Restart()
    {
        PositionNearNotificationArea();
        DismissProgress.BeginAnimation(
            System.Windows.Controls.Primitives.RangeBase.ValueProperty,
            null);
        DismissProgress.Value = 100;
        DismissProgress.BeginAnimation(
            System.Windows.Controls.Primitives.RangeBase.ValueProperty,
            new DoubleAnimation(100, 0, DisplayDuration));

        timer.Stop();
        timer.Start();

        if (IsVisible)
        {
            Topmost = false;
            Topmost = true;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Restart();
    }

    private void PositionNearNotificationArea()
    {
        const double margin = 16;
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - margin;
        Top = workArea.Bottom - Height - margin;
    }
}
