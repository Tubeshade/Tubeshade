# Tubeshade

## Main features

* Subscribe to YouTube channels, either by checking channels based on schedule or via [webhooks](https://pubsubhubbub.appspot.com/subscribe)
* Automatically or manually download configured video format(s) using [yt-dlp](https://github.com/yt-dlp/yt-dlp)
* [SponsorBlock](https://sponsor.ajay.app/) integration to automatically skip segments during playback
* Sync cookies from Firefox using a browser extension
* Separate channels into libraries
* SSO supported via OIDC

## Installation

Tubeshade can be installed using a debian package from [releases](https://github.com/Tubeshade/Tubeshade/releases)
or a [docker image](https://github.com/Tubeshade/Tubeshade/pkgs/container/tubeshade).
The debian package only contains Tubeshade - dependencies such as Postgres, yt-dlp, ffmpeg and deno (or other JS runtime) need to be installed separately.
However, the docker image contains all the needed dependencies, except Postgres.

## Configuration

Available configuration options can be found at
[SchedulerOptions](./source/Tubeshade.Server/Configuration/SchedulerOptions.cs),
[YtdlpOptions](./source/Tubeshade.Server/Configuration/YtdlpOptions.cs),
[OidcProviderOptions](./source/Tubeshade.Server/Configuration/Auth/Options/OidcProviderOptions.cs),
[DatabaseOptions](./source/Tubeshade.Data/Configuration/DatabaseOptions.cs),
[PubSubHubbubOptions](./source/PubSubHubbub/PubSubHubbubOptions.cs),
[SponsorBlockOptions](./source/SponsorBlock/SponsorBlockOptions.cs).
For the HTTP server configuration, see [Microsoft documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-9.0).
