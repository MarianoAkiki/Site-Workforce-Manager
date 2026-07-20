using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Site_Workforce_Manager.Services;

public static class ConfirmationDialogService
{
    public static bool Show(
        string title,
        string message,
        string confirmText = "Confirm",
        string cancelText = "Cancel",
        bool isDanger = false)
    {
        var result = false;
        var accentBrush = new SolidColorBrush(isDanger ? Color.FromRgb(220, 38, 38) : Color.FromRgb(37, 99, 235));
        var accentHoverBrush = new SolidColorBrush(isDanger ? Color.FromRgb(185, 28, 28) : Color.FromRgb(29, 78, 216));

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ShowInTaskbar = false,
            Owner = Application.Current.MainWindow
        };

        var confirmButton = CreateButton(confirmText, accentBrush, Brushes.White);
        var cancelButton = CreateButton(cancelText, Brushes.White, new SolidColorBrush(Color.FromRgb(15, 23, 42)));

        confirmButton.BorderBrush = accentBrush;
        cancelButton.BorderBrush = new SolidColorBrush(Color.FromRgb(217, 226, 236));

        confirmButton.MouseEnter += (_, _) =>
        {
            confirmButton.Background = accentHoverBrush;
            confirmButton.BorderBrush = accentHoverBrush;
        };
        confirmButton.MouseLeave += (_, _) =>
        {
            confirmButton.Background = accentBrush;
            confirmButton.BorderBrush = accentBrush;
        };

        confirmButton.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            result = false;
            dialog.Close();
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 24, 0, 0)
        };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(confirmButton);

        var contentPanel = new StackPanel();
        contentPanel.Children.Add(new Border
        {
            Width = 52,
            Height = 52,
            CornerRadius = new CornerRadius(26),
            Background = new SolidColorBrush(isDanger ? Color.FromRgb(254, 226, 226) : Color.FromRgb(224, 234, 255)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new TextBlock
            {
                Text = isDanger ? "!" : "?",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = accentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        contentPanel.Children.Add(new TextBlock
        {
            Text = title,
            Margin = new Thickness(0, 18, 0, 0),
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
            TextAlignment = TextAlignment.Center
        });

        contentPanel.Children.Add(new TextBlock
        {
            Text = message,
            Margin = new Thickness(0, 10, 0, 0),
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });

        contentPanel.Children.Add(buttonPanel);

        dialog.Content = new Border
        {
            Padding = new Thickness(28),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(217, 226, 236)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 24,
                ShadowDepth = 6,
                Direction = 270,
                Opacity = 0.18,
                Color = Colors.Black
            },
            Child = contentPanel
        };

        dialog.ShowDialog();
        return result;
    }

    public static void ShowInfo(string title, string message)
    {
        var accentBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ShowInTaskbar = false,
            Owner = Application.Current.MainWindow
        };

        var okButton = CreateButton("OK", accentBrush, Brushes.White);
        okButton.BorderBrush = accentBrush;
        okButton.MouseEnter += (_, _) =>
        {
            okButton.Background = new SolidColorBrush(Color.FromRgb(29, 78, 216));
            okButton.BorderBrush = new SolidColorBrush(Color.FromRgb(29, 78, 216));
        };
        okButton.MouseLeave += (_, _) =>
        {
            okButton.Background = accentBrush;
            okButton.BorderBrush = accentBrush;
        };
        okButton.Click += (_, _) => dialog.Close();

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 24, 0, 0)
        };
        buttonPanel.Children.Add(okButton);

        var contentPanel = new StackPanel();
        contentPanel.Children.Add(new Border
        {
            Width = 52,
            Height = 52,
            CornerRadius = new CornerRadius(26),
            Background = new SolidColorBrush(Color.FromRgb(224, 234, 255)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new TextBlock
            {
                Text = "i",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = accentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });
        contentPanel.Children.Add(new TextBlock
        {
            Text = title,
            Margin = new Thickness(0, 18, 0, 0),
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
            TextAlignment = TextAlignment.Center
        });
        contentPanel.Children.Add(new TextBlock
        {
            Text = message,
            Margin = new Thickness(0, 10, 0, 0),
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
        contentPanel.Children.Add(buttonPanel);

        dialog.Content = new Border
        {
            Padding = new Thickness(28),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(217, 226, 236)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 24,
                ShadowDepth = 6,
                Direction = 270,
                Opacity = 0.18,
                Color = Colors.Black
            },
            Child = contentPanel
        };

        dialog.ShowDialog();
    }

    private static Button CreateButton(string text, Brush background, Brush foreground)
    {
        return new Button
        {
            Content = text,
            MinWidth = 104,
            Margin = new Thickness(10, 0, 0, 0),
            Padding = new Thickness(16, 10, 16, 10),
            Background = background,
            Foreground = foreground,
            BorderThickness = new Thickness(1),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Cursor = System.Windows.Input.Cursors.Hand
        };
    }
}
