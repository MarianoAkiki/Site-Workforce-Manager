using System.Windows;
using System.Windows.Media.Animation;

namespace Site_Workforce_Manager.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public void FadeOutAndClose(Action onComplete)
    {
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(350));
        fade.Completed += (_, _) =>
        {
            Close();
            onComplete();
        };
        BeginAnimation(OpacityProperty, fade);
    }
}
