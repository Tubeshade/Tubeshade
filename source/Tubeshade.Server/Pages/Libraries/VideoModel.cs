using Tubeshade.Data.Media;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class VideoModel
{
    public required VideoEntity Video { get; init; }

    public required ChannelEntity Channel { get; init; }
}
