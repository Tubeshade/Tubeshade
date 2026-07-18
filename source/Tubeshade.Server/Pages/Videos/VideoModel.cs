using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Data.Media.Videos;

namespace Tubeshade.Server.Pages.Videos;

public sealed class VideoModel
{
    public required DetailedVideo Video { get; init; }

    public required Period? ActualDuration { get; init; }

    public required ChannelEntity Channel { get; init; }

    public bool Viewed => Video.Viewed is true;

    public double? DurationInSeconds => Video.Duration?.ToDuration().TotalSeconds;

    public string? GetThumbnailSource(IUrlHelper urlHelper) => Video.Thumbnails.GetSource(urlHelper);

    public string GetThumbnailSourceSet(IUrlHelper urlHelper) => Video.Thumbnails.GetSourceSet(urlHelper);
}
