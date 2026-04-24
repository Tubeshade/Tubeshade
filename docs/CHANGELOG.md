# Changelog

## [Nightly]

_Latest build from master branch._

### Added

- Handle deleted entry feed updates
  ([#273](https://github.com/Tubeshade/Tubeshade/pull/273))
- Store file metadata
  ([#274](https://github.com/Tubeshade/Tubeshade/pull/274))

### Changed

- Process feed updates in a background task instead of during the request
  ([#264](https://github.com/Tubeshade/Tubeshade/pull/264))
- Use textarea instead of CSV for multi-value inputs
  ([#269](https://github.com/Tubeshade/Tubeshade/pull/269))
- Save changes after each video when scanning channels
  ([#271](https://github.com/Tubeshade/Tubeshade/pull/271))
- Schedule tasks only after listening to database notifications
  ([#274](https://github.com/Tubeshade/Tubeshade/pull/274))
- Format file sizes to closest unit instead of MiB
  ([#274](https://github.com/Tubeshade/Tubeshade/pull/274))
- Update .NET SDK from 10.0.201 to 10.0.203, and runtime from 10.0.5 to 10.0.7
  ([#279](https://github.com/Tubeshade/Tubeshade/pull/279), [#281](https://github.com/Tubeshade/Tubeshade/pull/281))
- Don't hold an open database transaction for the whole download, move each file separately
  ([#280](https://github.com/Tubeshade/Tubeshade/pull/280))

### Fixed

- Identify and filter livestream feed updates
  ([#121](https://github.com/Tubeshade/Tubeshade/issues/121))
- Access control rules for new users and system user
  ([#264](https://github.com/Tubeshade/Tubeshade/pull/264))
- Don't automatically reindex videos that have not yet been published
  ([#265](https://github.com/Tubeshade/Tubeshade/issues/265))
- Fix slow query for downloadable video search
  ([#275](https://github.com/Tubeshade/Tubeshade/issues/275))

## [0.1.5] - 2026-04-04

### Added

- Periodically report video playback position
  ([#141](https://github.com/Tubeshade/Tubeshade/issues/141))
- Link to indexed videos in video descriptions
  ([#169](https://github.com/Tubeshade/Tubeshade/issues/169))

### Changed

- Fall back to [Process.Kill](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.kill?view=net-10.0) after signaling process to exit
  ([#247](https://github.com/Tubeshade/Tubeshade/pull/247))
- Ignore formats with lower framerate than downloaded videos
  ([#249](https://github.com/Tubeshade/Tubeshade/pull/249))
- Treat videos shorter than 20 seconds of unknown type as shorts
  ([#250](https://github.com/Tubeshade/Tubeshade/pull/250))
- Ignore YouTube feed updates that are posts
  ([#251](https://github.com/Tubeshade/Tubeshade/pull/251))
- Rework how error details are displayed for tasks
  ([#252](https://github.com/Tubeshade/Tubeshade/pull/252))
- Switch to `notify` systemd service type
  ([#253](https://github.com/Tubeshade/Tubeshade/pull/253))
- Update .NET SDK from 10.0.103 to 10.0.201, and runtime from 10.0.3 to 10.0.5
  ([#248](https://github.com/Tubeshade/Tubeshade/pull/248))
- Update yt-dlp from 2026.02.21 to 2026.03.17
  ([#248](https://github.com/Tubeshade/Tubeshade/pull/248))
- Don't create a task if the same one is already queued/running
  ([#164](https://github.com/Tubeshade/Tubeshade/issues/164))

### Fixed

- Refresh cookies between requests during long-running tasks
  ([#260](https://github.com/Tubeshade/Tubeshade/pull/260))
- Don't redirect htmx requests to login page
  ([#218](https://github.com/Tubeshade/Tubeshade/issues/218))

## [0.1.4] - 2026-02-22

### Added

- Sorting by view and like count
  ([#229](https://github.com/Tubeshade/Tubeshade/pull/229))
- Task run source tracking
  ([#174](https://github.com/Tubeshade/Tubeshade/issues/174))

### Changed

- Index livestreams while they are live
  ([#176](https://github.com/Tubeshade/Tubeshade/issues/176))
- Use yt-dlp built-in logic for downloading the best thumbnail
  ([#238](https://github.com/Tubeshade/Tubeshade/pull/238))
- Show rate/remaining estimate for more tasks
  ([#88](https://github.com/Tubeshade/Tubeshade/issues/88))
- Update .NET SDK from 10.0.101 to 10.0.103, and runtime from 10.0.1 to 10.0.3
  ([#245](https://github.com/Tubeshade/Tubeshade/pull/245))
- Update yt-dlp from 2025.12.08 to 2026.02.21
  ([#245](https://github.com/Tubeshade/Tubeshade/pull/245))

### Fixed

- Skip overlapping Sponsor Block segments
  ([#117](https://github.com/Tubeshade/Tubeshade/issues/117))
- Correctly display rate for non-download tasks
  ([#234](https://github.com/Tubeshade/Tubeshade/pull/234))
- Blocking task run ordering
  ([#242](https://github.com/Tubeshade/Tubeshade/pull/242))

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

[0.1.5]: https://github.com/Tubeshade/Tubeshade/releases/tag/v0.1.5

[0.1.4]: https://github.com/Tubeshade/Tubeshade/releases/tag/v0.1.4

[0.1.3]: https://github.com/Tubeshade/Tubeshade/releases/tag/v0.1.3

[0.1.2]: https://github.com/Tubeshade/Tubeshade/releases/tag/v0.1.2

[0.1.1]: https://github.com/Tubeshade/Tubeshade/releases/tag/v0.1.1

[0.1.0]: https://github.com/Tubeshade/Tubeshade/releases/tag/v0.1.0

[nightly]: https://github.com/Tubeshade/Tubeshade/releases/tag/nightly
