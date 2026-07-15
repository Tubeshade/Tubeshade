using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;

namespace Tubeshade.Server.Pages.Shared;

internal static class Defaults
{
    internal const int PageSize = 24;
    internal const int PageIndex = 0;

    internal const double PlaybackEpsilon = 10;
    internal const double ReportPeriodInSeconds = 60;

    internal static readonly SortVideoBy VideoOrder = SortVideoBy.PublishedAt;
    internal static readonly SortDirection VideoDirection = SortDirection.Descending;

    internal static readonly SortChannelBy ChannelOrder = SortChannelBy.ChannelName;
    internal static readonly SortDirection ChannelDirection = SortDirection.Ascending;
}
