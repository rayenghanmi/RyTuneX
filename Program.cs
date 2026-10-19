using Microsoft.UI.Xaml.Settings;

namespace RyTuneX;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // Windows App SDK 2.x+ XAML performance & memory optimizations
            XamlOptionalChanges.EnableChange(XamlChangeId.DefaultStyleOptimizations);
            XamlOptionalChanges.EnableChange(XamlChangeId.IconNoGridOptimization);
            XamlOptionalChanges.EnableChange(XamlChangeId.DeferContextFlyoutInit);
            XamlOptionalChanges.EnableChange(XamlChangeId.OptimizeApplyStyles);
            XamlOptionalChanges.Lock();

            // Configure garbage collection latency for interactive desktop responsiveness
            System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.Interactive;
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"Failed to enable XamlOptionalChanges: {ex.Message}");
        }

        XamlGeneratedProgram.XamlGeneratedMain();
    }
}
