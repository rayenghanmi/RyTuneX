namespace RyTuneX.Models;

// Represents a searchable item in the application that can be found via the title bar search.

public sealed class SearchableItem
{

    // The display name shown in search results.

    public required string DisplayName
    {
        get; init;
    }

    // Optional description for additional context in search results.

    public string? Description
    {
        get; init;
    }

    // The glyph icon code for the item.

    public string? Glyph
    {
        get; init;
    }

    // The full type name of the page to navigate to (e.g., "RyTuneX.Views.OptimizeSystemPage").

    public required string PageTypeName
    {
        get; init;
    }

    // The tag/name of the specific option on the page (e.g., toggle switch name).
    // Used to scroll to or highlight the specific option after navigation.

    public string? OptionTag
    {
        get; init;
    }

    // The category or section this item belongs to.

    public string? Category
    {
        get; init;
    }

    public override string ToString() => DisplayName;
}
