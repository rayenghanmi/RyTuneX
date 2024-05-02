using Microsoft.UI.Xaml;

namespace RyTuneX.Helpers;

public class NavigationHelper
{
    public static string GetNavigateTo(DependencyObject obj) => (string)obj.GetValue(NavigateToProperty);

    public static void SetNavigateTo(DependencyObject obj, string value) => obj.SetValue(NavigateToProperty, value);

    public static readonly DependencyProperty NavigateToProperty =
        DependencyProperty.RegisterAttached("NavigateTo", typeof(string), typeof(NavigationHelper), new PropertyMetadata(null));
}