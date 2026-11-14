using System;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Abstractions;

internal class DeleteParameters(Guid id, long userId) : GetSingleParameters(id, userId, Permissions.Delete);

internal class GetSingleVideoParameters : GetVideoParameters
{
    public GetSingleVideoParameters(Guid id, Guid videoId, long userId, Permissions permissions)
        : base(videoId, userId, permissions)
    {
        Id = id;
    }

    public Guid Id { get; }
}

internal class GetVideosParameters : GetParameters
{
    public GetVideosParameters(Guid[] videoIds, long userId, Permissions permissions)
        : base(userId, permissions)
    {
        VideoIds = videoIds;
    }

    public Guid[] VideoIds { get; }
}

internal class GetVideoParameters : GetParameters
{
    public GetVideoParameters(Guid videoId, long userId, Permissions permissions)
        : base(userId, permissions)
    {
        VideoId = videoId;
    }

    public Guid VideoId { get; }
}

public class GetFromLibraryParameters : GetParameters
{
    public GetFromLibraryParameters(long userId, Guid libraryId, Permissions permissions)
        : base(userId, permissions)
    {
        LibraryId = libraryId;
    }

    public Guid LibraryId { get; }
}

public class GetParameters : IAccessParameters
{
    public GetParameters(long userId, Permissions permissions)
    {
        PrincipalId = userId;
        Permissions = permissions;
    }

    public long PrincipalId { get; }

    public Permissions Permissions { get; }
}

internal class GetSingleExternalUrlParameters : GetParameters
{
    public GetSingleExternalUrlParameters(string externalUrl, long userId, Permissions permissions)
        : base(userId, permissions)
    {
        ExternalUrl = externalUrl;
    }

    public string ExternalUrl { get; }
}

internal class GetSingleExternalParameters : GetParameters
{
    public GetSingleExternalParameters(string externalId, long userId, Permissions permissions)
        : base(userId, permissions)
    {
        ExternalId = externalId;
    }

    public string ExternalId { get; }
}

internal class GetSingleLibraryExternalParameters : GetParameters
{
    public GetSingleLibraryExternalParameters(string externalId, Guid libraryId, long userId, Permissions permissions)
        : base(userId, permissions)
    {
        ExternalId = externalId;
        LibraryId = libraryId;
    }

    public string ExternalId { get; }
    public Guid LibraryId { get; }
}

internal class GetSingleParameters : GetParameters
{
    public GetSingleParameters(Guid id, long userId, Permissions permissions)
        : base(userId, permissions)
    {
        Id = id;
    }

    public Guid Id { get; }
}
