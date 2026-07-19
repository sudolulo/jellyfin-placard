using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.Placard.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Placard;

/// <summary>
/// Placard: bakes each library's name onto a representative backdrop,
/// matching Jellyfin's default library-card styling.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "Placard";

    public override Guid Id => Guid.Parse("b6f8e2a4-1c3d-4e5f-9a7b-2d4c6e8f0a1b");

    public override string Description =>
        "Bakes each library's name onto a representative backdrop, matching Jellyfin's default card style.";

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = string.Format(
                CultureInfo.InvariantCulture,
                "{0}.Configuration.configPage.html",
                GetType().Namespace)
        };
    }
}
