using NodaTime;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class VideoModel
{
    public required VideoEntity Video { get; init; }

    public required Period ActualDuration { get; init; }

    public required ChannelEntity Channel { get; init; }

    public bool Viewed => Video.ViewedAt is not null;
}
