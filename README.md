# Placard

A Jellyfin plugin that bakes each **library's name onto a representative backdrop**,
matching Jellyfin's default library-card styling — so your home-screen library cards
look intentional and stop rotating.

![example](docs/example.jpg)

## What it does

Jellyfin auto-generates a collage for libraries with no image and re-rolls it over
time. Placard replaces that with a static, labeled card per library:

- Picks a backdrop from a title in the library (highest-rated, random, or newest),
  or a **pinned** title you choose.
- Composites the library name centered, in **Noto Sans**, with a soft drop shadow
  over a light darkening scrim — the same look Jellyfin uses for its default covers.
- Sets the image through the internal provider API, so the change shows **without a
  server restart**.
- Runs as a scheduled task (daily by default) and applies to new libraries too.

## Configuration

Dashboard → Plugins → **Placard**:

| Setting | Description |
|---|---|
| Enabled | Master on/off. |
| Backdrop source | Highest rated / Random / Newest title in each library. |
| Darkening | Scrim opacity (0–255); ~105 matches the default. |
| Label size | Label height as a percent of image height. |
| Pinned sources | One per line, `Library Name=Item Title`. Overrides the rule. |

Example pins:

```
Movies=The Dark Knight
Anime=Death Note
Collections=The Avengers Collection
```

Then run **Scheduled Tasks → Generate Placard library cards** (or wait for the daily run).

## Building

Requires the .NET 9 SDK (Jellyfin 10.11 targets net9.0).

```
dotnet build -c Release
```

Copy `bin/Release/net9.0/Jellyfin.Plugin.Placard.dll` (plus `meta.json`) into
`<jellyfin-config>/plugins/Placard_<version>/` and restart Jellyfin.

## Compatibility

- Jellyfin **10.11.x** (targetAbi `10.11.0.0`, net9.0).
- Uses the SkiaSharp bundled with the Jellyfin server.
