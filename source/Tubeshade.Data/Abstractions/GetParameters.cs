using System;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Abstractions;

internal class DeleteParameters(Guid id, Guid userId) : GetSingleParameters(id, userId, Permissions.Delete);

internal class GetSingleVideoParameters : GetVideoParameters
{
    public GetSingleVideoParameters(Guid id, Guid videoId, Guid userId, Permissions permissions)
        : base(videoId, userId, permissions)
    {
        Id = id;
    }

    public Guid Id { get; }
}

internal class GetVideosParameters : GetParameters
{
    public GetVideosParameters(Guid[] videoIds, Guid userId, Permissions permissions)
        : base(userId, permissions)
    {
        VideoIds = videoIds;
    }

    public Guid[] VideoIds { get; }
}

internal class GetVideoParameters : GetParameters
{
    public GetVideoParameters(Guid videoId, Guid userId, Permissions permissions)
        : base(userId, permissions)
    {
        VideoId = videoId;
    }

    public Guid VideoId { get; }
}

public class GetFromLibraryParameters : GetParameters
{
    public GetFromLibraryParameters(Guid userId, Guid libraryId, Permissions permissions)
        : base(userId, permissions)
    {
        LibraryId = libraryId;
    }

    public Guid LibraryId { get; }
}

public class GetParameters : IAccessParameters
{
    public GetParameters(Guid userId, Permissions permissions)
    {
        PrincipalId = userId;
        Permissions = permissions;
    }

    public Guid PrincipalId { get; }

    public Permissions Permissions { get; }
}

internal class GetSingleExternalUrlParameters : GetParameters
{
    public GetSingleExternalUrlParameters(string externalUrl, Guid userId, Permissions permissions)
        : base(userId, permissions)
    {
        ExternalUrl = externalUrl;
    }

    public string ExternalUrl { get; }
}

internal class GetSingleExternalParameters : GetParameters
{
    public GetSingleExternalParameters(string externalId, Guid userId, Permissions permissions)
        : base(userId, permissions)
    {
        ExternalId = externalId;
    }

    public string ExternalId { get; }
}

internal class GetSingleLibraryExternalParameters : GetParameters
{
    public GetSingleLibraryExternalParameters(string externalId, Guid libraryId, Guid userId, Permissions permissions)
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
    public GetSingleParameters(Guid id, Guid userId, Permissions permissions)
        : base(userId, permissions)
    {
        Id = id;
    }

    public Guid Id { get; }
}
