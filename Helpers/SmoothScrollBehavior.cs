using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Site_Workforce_Manager.Helpers;

public static class SmoothScrollBehavior
{
    private sealed class ScrollState
    {
        public DispatcherTimer? Timer { get; set; }
        public double TargetOffset { get; set; }
    }

    private static readonly Dictionary<ScrollViewer, ScrollState> States = new();
    private const double MouseWheelStep = 220d;
    private const double SmoothingFactor = 0.35d;
    private const double StopThreshold = 0.5d;

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SmoothScrollBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsEnabledProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer scrollViewer)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
            scrollViewer.ScrollChanged += OnScrollChanged;
            scrollViewer.Unloaded += OnUnloaded;

            if (!States.ContainsKey(scrollViewer))
            {
                States[scrollViewer] = new ScrollState
                {
                    TargetOffset = scrollViewer.VerticalOffset
                };
            }
        }
        else
        {
            scrollViewer.PreviewMouseWheel -= OnPreviewMouseWheel;
            scrollViewer.ScrollChanged -= OnScrollChanged;
            scrollViewer.Unloaded -= OnUnloaded;
            RemoveState(scrollViewer);
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer || scrollViewer.ScrollableHeight <= 0)
        {
            return;
        }

        e.Handled = true;

        var state = GetOrCreateState(scrollViewer);
        var deltaSteps = e.Delta / 120d;
        state.TargetOffset = Math.Clamp(
            state.TargetOffset - (deltaSteps * MouseWheelStep),
            0d,
            scrollViewer.ScrollableHeight);

        EnsureTimer(scrollViewer, state);
    }

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        var state = GetOrCreateState(scrollViewer);

        if (state.Timer is null || !state.Timer.IsEnabled)
        {
            state.TargetOffset = scrollViewer.VerticalOffset;
        }
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            RemoveState(scrollViewer);
        }
    }

    private static ScrollState GetOrCreateState(ScrollViewer scrollViewer)
    {
        if (!States.TryGetValue(scrollViewer, out var state))
        {
            state = new ScrollState
            {
                TargetOffset = scrollViewer.VerticalOffset
            };
            States[scrollViewer] = state;
        }

        return state;
    }

    private static void EnsureTimer(ScrollViewer scrollViewer, ScrollState state)
    {
        if (state.Timer is null)
        {
            state.Timer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(15),
                DispatcherPriority.Render,
                (_, _) => AnimateScroll(scrollViewer),
                scrollViewer.Dispatcher);
        }

        if (!state.Timer.IsEnabled)
        {
            state.Timer.Start();
        }
    }

    private static void AnimateScroll(ScrollViewer scrollViewer)
    {
        if (!States.TryGetValue(scrollViewer, out var state))
        {
            return;
        }

        var currentOffset = scrollViewer.VerticalOffset;
        var difference = state.TargetOffset - currentOffset;

        if (Math.Abs(difference) <= StopThreshold)
        {
            scrollViewer.ScrollToVerticalOffset(state.TargetOffset);
            state.Timer?.Stop();
            return;
        }

        var nextOffset = currentOffset + (difference * SmoothingFactor);
        scrollViewer.ScrollToVerticalOffset(nextOffset);
    }

    private static void RemoveState(ScrollViewer scrollViewer)
    {
        if (!States.TryGetValue(scrollViewer, out var state))
        {
            return;
        }

        state.Timer?.Stop();
        state.Timer = null;
        States.Remove(scrollViewer);
    }
}
