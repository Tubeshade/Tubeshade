# Changelog

## [Nightly]

_Latest build from master branch._

### Added

- Sorting by view and like count
  ([#229](https://github.com/Tubeshade/Tubeshade/pull/229))
- Task run source tracking
  ([#174](https://github.com/Tubeshade/Tubeshade/issues/174))

### Changed

- Index livestreams while they are live
  ([#176](https://github.com/Tubeshade/Tubeshade/issues/176))
- Skip thumbnails that are not found while downloading
  ([#237](https://github.com/Tubeshade/Tubeshade/pull/237))

### Fixed

- Skip overlapping Sponsor Block segments
  ([#117](https://github.com/Tubeshade/Tubeshade/issues/117))
- Correctly display rate for non-download tasks
  ([#234](https://github.com/Tubeshade/Tubeshade/pull/234))

## [0.1.3] - 2025-12-28

### Added

- `streaming` download method for MP4 videos
  ([#224](https://github.com/Tubeshade/Tubeshade/pull/224))
- Pages for manually creating new channels
  ([#225](https://github.com/Tubeshade/Tubeshade/pull/225))
- Pages for manually creating new videos
  ([#86](https://github.com/Tubeshade/Tubeshade/issues/86))

### Changed

- Rewrite vendored code for calling yt-dlp
  ([#190](https://github.com/Tubeshade/Tubeshade/issues/190))

### Fixed

- Don't log cancelled requests
  ([#186](https://github.com/Tubeshade/Tubeshade/issues/186))
- Correctly select video thumbnail when scanning a channel
  ([#220](https://github.com/Tubeshade/Tubeshade/issues/220))
- Fixed default JS runtime parameter in docker image 
  ([#219](https://github.com/Tubeshade/Tubeshade/pull/219))

## [0.1.2] - 2025-12-14

### Added

- yt-dlp verbose output logs at trace level
  ([#215](https://github.com/Tubeshade/Tubeshade/pull/215))

### Fixed

- Correctly handle yt-dlp data parsing errors
  ([#216](https://github.com/Tubeshade/Tubeshade/pull/216))

## [0.1.1] - 2025-12-13

### Changed

- Vendor [YoutubeDLSharp](https://github.com/Bluegrams/YoutubeDLSharp)
  ([#213](https://github.com/Tubeshade/Tubeshade/pull/213))
- Update .NET SDK from 10.0.100 to 10.0.101, and runtime from 10.0.0 to 10.0.1
  ([#210](https://github.com/Tubeshade/Tubeshade/pull/210))
- Update yt-dlp from 2025.11.12 to 2025.12.08
  ([#210](https://github.com/Tubeshade/Tubeshade/pull/210))

### Fixed

- Correctly display all default values for preferences
  ([#212](https://github.com/Tubeshade/Tubeshade/pull/212))
- Include user selected filters in pagination links
  ([#204](https://github.com/Tubeshade/Tubeshade/issues/204))
- Remove ignored status from video after download
  ([#200](https://github.com/Tubeshade/Tubeshade/issues/200))

## [0.1.0] - 2025-12-06

_Initial release._

[0.1.3]: https://github.com/Tubeshade/Tubeshade/releases/tag/v0.1.3

[0.1.2]: https://github.com/Tubeshade/Tubeshade/releases/tag/v0.1.2

[0.1.1]: https://github.com/Tubeshade/Tubeshade/releases/tag/v0.1.1

[0.1.0]: https://github.com/Tubeshade/Tubeshade/releases/tag/v0.1.0

[nightly]: https://github.com/Tubeshade/Tubeshade/releases/tag/nightly
