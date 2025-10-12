using System;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media;

public sealed class VideoParameters : IAccessParameters
{
    public required Guid UserId { get; init; }

    public Access Access => Access.Read;

    public Guid? LibraryId { get; init; }

    public Guid? ChannelId { get; init; }

    public required int Limit { get; init; }

    public required int Offset { get; init; }

    public bool? Viewed { get; init; }

    public string? Query { get; init; }

    public VideoType? Type { get; init; }
}
