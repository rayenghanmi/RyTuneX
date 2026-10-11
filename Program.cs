using Microsoft.UI.Xaml.Settings;

namespace RyTuneX;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // Windows App SDK 2.3+ XAML performance optimizations
            XamlOptionalChanges.EnableChange(XamlChangeId.DefaultStyleOptimizations);
            XamlOptionalChanges.EnableChange(XamlChangeId.IconNoGridOptimization);
            XamlOptionalChanges.EnableChange(XamlChangeId.DeferContextFlyoutInit);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"Failed to enable XamlOptionalChanges: {ex.Message}");
        }

        XamlGeneratedProgram.XamlGeneratedMain();
    }
}
