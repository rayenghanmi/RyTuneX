using Windows.UI;
using RyTuneX.Models;

namespace RyTuneX.Contracts.Services;

public interface IAccentColorService
{
    AccentColorMode Mode { get; }
    Color CustomColor { get; }
    Color CurrentColor { get; }

    Task InitializeAsync();
    Task SetAccentColorModeAsync(AccentColorMode mode);
    Task SetCustomColorAsync(Color color);
    void RefreshAccentColor();
}
