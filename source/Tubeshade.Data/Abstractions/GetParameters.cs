using System;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Abstractions;

internal class DeleteParameters(Guid id, Guid userId) : GetSingleParameters(id, userId, Access.Delete);

internal class GetSingleVideoParameters : GetVideoParameters
{
    public GetSingleVideoParameters(Guid id, Guid videoId, Guid userId, Access access)
        : base(videoId, userId, access)
    {
        Id = id;
    }

    public Guid Id { get; }
}


internal class GetVideosParameters : GetParameters
{
    public GetVideosParameters(Guid[] videoIds, Guid userId, Access access)
        : base(userId, access)
    {
        VideoIds = videoIds;
    }

    public Guid[] VideoIds { get; }
}

internal class GetVideoParameters : GetParameters
{
    public GetVideoParameters(Guid videoId, Guid userId, Access access)
        : base(userId, access)
    {
        VideoId = videoId;
    }

    public Guid VideoId { get; }
}

public class GetFromLibraryChannelParameters : GetParameters
{
    public GetFromLibraryChannelParameters(Guid userId, Guid? libraryId, Guid? channelId, Access access)
        : base(userId, access)
    {
        LibraryId = libraryId;
        ChannelId = channelId;
    }

    public Guid? LibraryId { get; }
    public Guid? ChannelId { get; }
}

public class GetFromLibraryParameters : GetParameters
{
    public GetFromLibraryParameters(Guid userId, Guid libraryId, Access access)
        : base(userId, access)
    {
        LibraryId = libraryId;
    }

    public Guid LibraryId { get; }
}

internal class GetFromChannelParameters : GetParameters
{
    public GetFromChannelParameters(Guid userId, Guid channelId, Access access)
        : base(userId, access)
    {
        ChannelId = channelId;
    }

    public Guid ChannelId { get; }
}

public class GetParameters : IAccessParameters
{
    public GetParameters(Guid userId, Access access)
    {
        UserId = userId;
        Access = access;
    }

    public Guid UserId { get; }
    public Access Access { get; }

    public int? Limit { get; init; }
    public int? Offset { get; init; }
    public string? Query { get; init; }
}

internal class GetSingleExternalUrlParameters : GetParameters
{
    public GetSingleExternalUrlParameters(string externalUrl, Guid userId, Access access)
        : base(userId, access)
    {
        ExternalUrl = externalUrl;
    }

    public string ExternalUrl { get; }
}

internal class GetSingleExternalParameters : GetParameters
{
    public GetSingleExternalParameters(string externalId, Guid userId, Access access)
        : base(userId, access)
    {
        ExternalId = externalId;
    }

    public string ExternalId { get; }
}

internal class GetSingleParameters : GetParameters
{
    public GetSingleParameters(Guid id, Guid userId, Access access)
        : base(userId, access)
    {
        Id = id;
    }

    public Guid Id { get; }
}

internal interface IAccessParameters
{
    Access Access { get; }
}
