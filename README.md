# [<img src="/Logo/kavita.svg" width="32" alt="">]() Kavita (LeviBickel Fork)
<div align="center">

![new_github_preview_stills](https://github.com/user-attachments/assets/f016b34f-3c4c-4f07-8e72-12cd6f4e71ea)

> **This is a personal fork of [Kavita](https://github.com/Kareadita/Kavita) by [majora2007](https://github.com/majora2007) and the Kavita team.**
> All original credit goes to them. This fork adds features not accepted upstream. It tracks upstream `develop` and is not affiliated with the Kavita project.

Kavita is a fast, feature rich, cross-platform reading server. Built with a focus for being a full solution for all your reading needs. Set up your own server and share
your reading collection with your friends and family!

[![License](https://img.shields.io/badge/license-GPLv3-blue.svg?style=flat)](https://github.com/Kareadita/Kavita/blob/master/LICENSE)
[![Upstream Release](https://img.shields.io/github/release/Kareadita/Kavita.svg?style=flat&label=upstream&maxAge=3600)](https://github.com/Kareadita/Kavita/releases)
[![Docker Image](https://img.shields.io/badge/ghcr.io-levibickel%2Fkavita-blue?style=flat&logo=docker)](https://github.com/LeviBickel/Kavita/pkgs/container/kavita)

</div>


## Fork Additions

### Audiobook Support
This fork adds a native **Audiobook** library type. Audio files (MP3, M4B, FLAC, OGG, WAV) are scanned and served directly from your server with a dedicated in-browser player.

- Metadata parsed from embedded tags via TagLibSharp (album → series, title → track, performers → author, composers → narrator)
- Cover art extracted from embedded artwork, with fallback to `cover.jpg` / `folder.jpg` in the same or parent directory
- In-browser streaming player with play/pause, skip ±30s, speed cycling (0.5×–2×), volume, and seek
- Playback progress saved every 10 seconds and restored on next open
- Auto-advances to the next track on completion


## What Kavita Provides
- Serve up Manga/Webtoons/Comics (cbr, cbz, zip/rar/rar5, 7zip, raw images) and Books (epub, pdf)
- **Audiobooks (MP3, M4B, FLAC, OGG, WAV) — added in this fork**
- First class responsive readers that work great on any device (phone, tablet, desktop)
- Customizable theming support: [Theme Repo](https://github.com/Kareadita/Themes) and [Documentation](https://wiki.kavitareader.com/guides/themes)
- External metadata integration and scrobbling for read status, ratings, and reviews (available via [Kavita+](https://wiki.kavitareader.com/kavita+))
- Rich Metadata support with filtering, searching, and smart filters
- Ways to group reading material: Collections, Reading Lists (CBL Import), Want to Read
- Ability to manage users with rich Role-based management for age restrictions, abilities within the app, OIDC, etc
- Rich web readers supporting webtoon, continuous reading mode (continue without leaving the reader), virtual pages (epub), etc
- Ability to customize your dashboard and side nav with smart filters, custom order and visibility toggles
- Full Localization Support ([Weblate](https://hosted.weblate.org/engage/kavita/))
- Ability to download metadata, reviews, ratings, and more (available via [Kavita+](https://wiki.kavitareader.com/kavita+))
- Epub-based Annotation/Highlight support


## Docker (this fork)

```bash
docker pull ghcr.io/levibickel/kavita:nightly

docker run -d \
  -p 5000:5000 \
  -v /your/data:/kavita/config \
  ghcr.io/levibickel/kavita:nightly
```

For full setup options, refer to the [upstream wiki](https://wiki.kavitareader.com/getting-started).


## Support

For issues with features in this fork, open an issue here.
For all other support, refer to the upstream project:

[![Discord](https://img.shields.io/badge/discord-chat-7289DA.svg?maxAge=60)](https://discord.gg/eczRp9eeem)
[![GitHub - Upstream Issues](https://img.shields.io/badge/github-upstream%20issues-red.svg?maxAge=60)](https://github.com/Kareadita/Kavita/issues)


## Notice
Kavita is being actively developed and should be considered beta software until the 1.0 release.
Kavita may be subject to changes in how the platform functions as it is being built out toward the
vision. You may lose data and have to restart. The Kavita team strives to avoid any data loss.


## Contributors

All credit for the original Kavita project goes to the Kavita team and contributors. [Contribute upstream](https://github.com/Kareadita/Kavita/blob/develop/CONTRIBUTING.md).
<a href="https://github.com/Kareadita/Kavita/graphs/contributors">
<img src="https://opencollective.com/kavita/contributors.svg?width=890&button=false&avatarHeight=42" />
</a>

If you find Kavita valuable, consider supporting the original project:
[OpenCollective](https://opencollective.com/Kavita#backer) · [Paypal](https://www.paypal.com/paypalme/majora2007) · [Kavita+](https://wiki.kavitareader.com/kavita+)


## Powered By
[![JetBrains logo.](https://resources.jetbrains.com/storage/products/company/brand/logos/jetbrains.svg)](https://jb.gg/OpenSource)

### License
* [GNU GPL v3](http://www.gnu.org/licenses/gpl.html)
* Copyright 2020-2024 Kavita Contributors
