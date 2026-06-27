using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Site_Workforce_Manager.Services;

public static class ToastNotificationService
{
    public static void ShowSuccess(string message)
    {
        Show(message, Color.FromRgb(22, 163, 74), Color.FromRgb(240, 253, 244));
    }

    private static void Show(string message, Color accentColor, Color backgroundColor)
    {
        var owner = Application.Current.MainWindow;
        var accentBrush = new SolidColorBrush(accentColor);

        var toast = new Window
        {
            Width = 360,
            SizeToContent = SizeToContent.Height,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ShowInTaskbar = false,
            Topmost = true,
            ResizeMode = ResizeMode.NoResize,
            Owner = owner
        };

        toast.Content = new Border
        {
            Padding = new Thickness(18, 14, 18, 14),
            Background = new SolidColorBrush(backgroundColor),
            BorderBrush = new SolidColorBrush(Color.FromRgb(187, 247, 208)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 18,
                ShadowDepth = 4,
                Direction = 270,
                Opacity = 0.16,
                Color = Colors.Black
            },
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new Border
                    {
                        Width = 28,
                        Height = 28,
                        Margin = new Thickness(0, 0, 12, 0),
                        Background = accentBrush,
                        CornerRadius = new CornerRadius(14),
                        Child = new TextBlock
                        {
                            Text = "✓",
                            Foreground = Brushes.White,
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    },
                    new TextBlock
                    {
                        Text = message,
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(20, 83, 45)),
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };

        toast.Loaded += (_, _) =>
        {
            var workArea = SystemParameters.WorkArea;
            double left, top;

            if (owner is not null && owner.WindowState == WindowState.Maximized)
            {
                left = workArea.Right - toast.ActualWidth - 24;
                top = workArea.Top + 24;
            }
            else if (owner is not null)
            {
                left = owner.Left + owner.ActualWidth - toast.ActualWidth - 32;
                top = owner.Top + 32;
            }
            else
            {
                left = workArea.Right - toast.ActualWidth - 24;
                top = workArea.Top + 24;
            }

            toast.Left = Math.Max(workArea.Left, Math.Min(left, workArea.Right - toast.ActualWidth));
            toast.Top = Math.Max(workArea.Top, Math.Min(top, workArea.Bottom - toast.ActualHeight));
        };

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2.4)
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            toast.Close();
        };

        toast.Show();
        timer.Start();
    }
}
