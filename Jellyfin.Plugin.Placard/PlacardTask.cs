using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.Placard.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Placard;

/// <summary>Scheduled task that (re)generates a labeled Primary image for each library.</summary>
public class PlacardTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IProviderManager _providerManager;
    private readonly ILogger<PlacardTask> _logger;

    public PlacardTask(ILibraryManager libraryManager, IProviderManager providerManager, ILogger<PlacardTask> logger)
    {
        _libraryManager = libraryManager;
        _providerManager = providerManager;
        _logger = logger;
    }

    public string Name => "Generate Placard library cards";

    public string Key => "PlacardGenerate";

    public string Description => "Bake each library's name onto a representative backdrop.";

    public string Category => "Placard";

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.DailyTrigger,
            TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
        };
    }

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance!.Configuration;
        if (!config.Enabled)
        {
            _logger.LogInformation("Placard is disabled; skipping run.");
            return;
        }

        var folders = _libraryManager.GetUserRootFolder().Children
            .OfType<CollectionFolder>()
            .ToList();

        var pins = ParsePins(config.PinnedSources);
        _logger.LogInformation("Placard: processing {Count} libraries ({Pins} pinned)", folders.Count, pins.Count);
        int done = 0;
        foreach (var folder in folders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await ProcessFolderAsync(folder, config, pins, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Placard: failed for library {Name}", folder.Name);
            }

            progress.Report(100.0 * ++done / Math.Max(folders.Count, 1));
        }
    }

    private static readonly BaseItemKind[] SourceTypes =
    {
        BaseItemKind.Movie, BaseItemKind.Series,
        BaseItemKind.MusicArtist, BaseItemKind.BoxSet, BaseItemKind.MusicAlbum
    };

    private static Dictionary<string, string> ParsePins(string raw)
    {
        var pins = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return pins;
        }

        foreach (var line in raw.Split('\n'))
        {
            var trimmed = line.Trim();
            var eq = trimmed.IndexOf('=', StringComparison.Ordinal);
            if (eq > 0)
            {
                pins[trimmed[..eq].Trim()] = trimmed[(eq + 1)..].Trim();
            }
        }

        return pins;
    }

    private (ItemSortBy, SortOrder)[] OrderFor(SourceRule rule) => rule switch
    {
        SourceRule.Newest => new[] { (ItemSortBy.DateCreated, SortOrder.Descending) },
        SourceRule.Random => new[] { (ItemSortBy.Random, SortOrder.Ascending) },
        _ => new[] { (ItemSortBy.CommunityRating, SortOrder.Descending) }
    };

    private BaseItem? PickSource(CollectionFolder folder, PluginConfiguration config, IReadOnlyDictionary<string, string> pins)
    {
        // Pinned title wins.
        if (pins.TryGetValue(folder.Name, out var pinned) && !string.IsNullOrWhiteSpace(pinned))
        {
            var byName = _libraryManager.GetItemList(new InternalItemsQuery
            {
                Parent = folder,
                Recursive = true,
                SearchTerm = pinned,
                IncludeItemTypes = SourceTypes,
                Limit = 15
            });
            var match = byName.FirstOrDefault(i =>
                    string.Equals(i.Name, pinned, StringComparison.OrdinalIgnoreCase) && i.GetImages(ImageType.Backdrop).Any())
                ?? byName.FirstOrDefault(i => i.GetImages(ImageType.Backdrop).Any());
            if (match is not null)
            {
                return match;
            }

            _logger.LogWarning("Placard: pinned title '{Pin}' not found in {Name}; using rule", pinned, folder.Name);
        }

        return _libraryManager.GetItemList(new InternalItemsQuery
        {
            Parent = folder,
            Recursive = true,
            IncludeItemTypes = SourceTypes,
            OrderBy = OrderFor(config.Source),
            Limit = 60
        }).FirstOrDefault(i => i.GetImages(ImageType.Backdrop).Any());
    }

    private async Task ProcessFolderAsync(CollectionFolder folder, PluginConfiguration config, IReadOnlyDictionary<string, string> pins, CancellationToken cancellationToken)
    {
        var source = PickSource(folder, config, pins);

        if (source is null)
        {
            _logger.LogWarning("Placard: no backdrop candidate found for {Name}", folder.Name);
            return;
        }

        var backdropPath = source.GetImages(ImageType.Backdrop).First().Path;
        if (string.IsNullOrEmpty(backdropPath) || !File.Exists(backdropPath))
        {
            _logger.LogWarning("Placard: backdrop path missing for {Name}", folder.Name);
            return;
        }

        _logger.LogInformation("Placard: {Library} <- backdrop of {Item}", folder.Name, source.Name);
        var bytes = CardRenderer.Render(backdropPath, folder.Name, config.ScrimOpacity, config.FontHeightPercent);

        using var stream = new MemoryStream(bytes);
        await _providerManager
            .SaveImage(folder, stream, "image/jpeg", ImageType.Primary, null, cancellationToken)
            .ConfigureAwait(false);
        await folder.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, cancellationToken).ConfigureAwait(false);
    }
}
