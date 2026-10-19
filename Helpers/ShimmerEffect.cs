using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System.Numerics;

namespace RyTuneX.Helpers;

public static class ShimmerEffect
{
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.RegisterAttached(
            "IsActive",
            typeof(bool),
            typeof(ShimmerEffect),
            new PropertyMetadata(false, OnIsActiveChanged));

    public static bool GetIsActive(DependencyObject obj) => (bool)obj.GetValue(IsActiveProperty);
    public static void SetIsActive(DependencyObject obj, bool value) => obj.SetValue(IsActiveProperty, value);

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element) return;

        if ((bool)e.NewValue)
        {
            AttachShimmer(element);
        }
        else
        {
            DetachShimmer(element);
        }
    }

    private static void AttachShimmer(FrameworkElement element)
    {
        element.Loaded -= Element_Loaded;
        element.Loaded += Element_Loaded;
        element.Unloaded -= Element_Unloaded;
        element.Unloaded += Element_Unloaded;

        // Monitor direct visibility changes
        element.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, Element_VisibilityChanged);

        // Monitor ancestor visibility changes so animation pauses when parent placeholder collapses
        HookAncestorVisibility(element);

        if (element.IsLoaded && element.Visibility == Visibility.Visible)
        {
            StartAnimation(element);
        }
    }

    private static void DetachShimmer(FrameworkElement element)
    {
        element.Loaded -= Element_Loaded;
        element.Unloaded -= Element_Unloaded;
        StopAnimation(element);
    }

    private static void Element_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && GetIsActive(element) && element.Visibility == Visibility.Visible)
        {
            HookAncestorVisibility(element);
            StartAnimation(element);
        }
    }

    private static void Element_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            StopAnimation(element);
        }
    }

    private static void Element_VisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (sender is FrameworkElement element && GetIsActive(element))
        {
            if (element.Visibility == Visibility.Visible && element.IsLoaded)
            {
                StartAnimation(element);
            }
            else
            {
                StopAnimation(element);
            }
        }
    }

    private static void HookAncestorVisibility(FrameworkElement element)
    {
        try
        {
            DependencyObject? current = VisualTreeHelper.GetParent(element) ?? element.Parent;
            while (current != null)
            {
                if (current is UIElement uie)
                {
                    uie.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, (s, dp) =>
                    {
                        if (s is UIElement ancestor && element.IsLoaded)
                        {
                            if (ancestor.Visibility == Visibility.Visible && element.Visibility == Visibility.Visible)
                            {
                                StartAnimation(element);
                            }
                            else
                            {
                                StopAnimation(element);
                            }
                        }
                    });
                }
                current = VisualTreeHelper.GetParent(current) ?? (current is FrameworkElement fe ? fe.Parent : null);
            }
        }
        catch
        {
        }
    }

    public static void Start(FrameworkElement element) => StartAnimation(element);
    public static void Stop(FrameworkElement element) => StopAnimation(element);

    private static void StartAnimation(FrameworkElement element)
    {
        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            if (visual == null) return;

            var compositor = visual.Compositor;
            if (compositor == null) return;

            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.Duration = TimeSpan.FromMilliseconds(950);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            animation.Direction = AnimationDirection.Alternate;

            var easing = compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.42f, 0.0f),
                new Vector2(0.58f, 1.0f));

            animation.InsertKeyFrame(0.0f, 0.40f);
            animation.InsertKeyFrame(1.0f, 1.0f, easing);

            visual.StartAnimation("Opacity", animation);
        }
        catch
        {
        }
    }

    private static void StopAnimation(FrameworkElement element)
    {
        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            if (visual != null)
            {
                visual.StopAnimation("Opacity");
                visual.Opacity = 1.0f;
            }
        }
        catch
        {
        }
    }
}
