using System.Runtime.InteropServices;
using System.Text;

namespace RyTuneX.Helpers;

public class RuntimeHelper
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);

    public static bool IsMSIX
    {
        get
        {
            var length = 0;

            return GetCurrentPackageFullName(ref length, null) != 15700L;
        }
    }

    private static string? _versionDescription;

    public static string GetVersionDescription()
    {
        if (_versionDescription != null)
        {
            return _versionDescription;
        }

        try
        {
            var version = typeof(RuntimeHelper).Assembly.GetName().Version;
            _versionDescription = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.8.0";
        }
        catch
        {
            _versionDescription = "1.8.0";
        }

        return _versionDescription;
    }
}

