# Changelog

All notable changes to Placard are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- Retarget Jellyfin 12.4 and .NET 10, using the server's SkiaSharp 3.119.4.
- Secret scanning runs Gitleaks 8.30.1 from a digest-pinned container image instead of downloading a release archive in the job.

## [1.0.0] - 2026-07-19

### Added
- Scheduled task ("Generate Placard library cards") that bakes each library's
  name onto a representative backdrop, matching Jellyfin's default cover style
  (Noto Sans, centered, soft drop shadow over a darkening scrim).
- Backdrop source rules: highest-rated / random / newest title in the library.
- Per-library pinned sources (`Library Name=Item Title`) to override the rule.
- Configuration page (enable, source rule, scrim opacity, label size, pins).
- SkiaSharp-based renderer that clamps out-of-range settings.

### Notes
- Sets images through the internal provider API, so the home-screen view cache
  updates without a server restart.
- Special views (Live TV / Playlists) are not covered yet. Planned for 1.1.
