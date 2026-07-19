using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Placard.Configuration;

/// <summary>Rule for picking which child item's backdrop represents a library.</summary>
public enum SourceRule
{
    TopRated = 0,
    Random = 1,
    Newest = 2
}

public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Master on/off.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How to choose the source backdrop for each library.</summary>
    public SourceRule Source { get; set; } = SourceRule.TopRated;

    /// <summary>Darkening overlay opacity, 0-255 (default ~41%).</summary>
    public int ScrimOpacity { get; set; } = 105;

    /// <summary>Label height as a percent of image height.</summary>
    public int FontHeightPercent { get; set; } = 20;

    /// <summary>Also label Live TV (SMPTE color bars) and Playlists.</summary>
    public bool IncludeSpecialViews { get; set; } = true;

    /// <summary>
    /// Per-library pinned source titles, one per line as "Library Name=Item Title".
    /// A pinned library uses that item's backdrop instead of the <see cref="Source"/> rule.
    /// </summary>
    public string PinnedSources { get; set; } = string.Empty;
}
