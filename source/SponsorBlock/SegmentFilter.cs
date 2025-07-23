using System;
using System.Collections.Generic;
using System.Linq;

namespace SponsorBlock;

public sealed class SegmentFilter
{
    public static SegmentFilter Empty { get; } = new();

    public static SegmentFilter Default { get; } = new()
    {
        Categories = SegmentCategory.List,
        Actions = SegmentAction.List,
        Service = SegmentService.YouTube,
    };

    public IEnumerable<SegmentCategory>? Categories { get; init; }

    public IEnumerable<Guid>? RequiredSegments { get; init; }

    public IEnumerable<SegmentAction>? Actions { get; init; }

    public SegmentService? Service { get; init; }

    internal string ToQueryParameters(string videoId)
    {
        var parameters = new Dictionary<string, string> { { "videoID", videoId } };

        if (Categories?.ToArray() is { Length: not 0 } categories)
        {
            parameters.Add(
                "categories",
                $"[{string.Join(',', categories.Select(category => $"\"{category.Name}\""))}]");
        }

        if (Actions?.ToArray() is { Length: not 0 } actions)
        {
            parameters.Add(
                "actions",
                $"[{string.Join(',', actions.Select(action => $"\"{action.Name}\""))}]");
        }

        if (Service is not null)
        {
            parameters.Add("service", Service.Name);
        }

        return string.Join('&', parameters.Select(pair => $"{pair.Key}={pair.Value}"));
    }
}
