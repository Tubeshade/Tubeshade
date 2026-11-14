namespace Tubeshade.Data.AccessControl;

/// <remarks><see cref="SystemPrincipal"/> is deliberately not a principal, since it holds no grants.</remarks>
public abstract record Principal
{
    public abstract long GrantHolderId { get; }

    /// <summary>Whether the principal's token scopes allow the operation; always true for a signed-in user.</summary>
    public virtual bool Allows(Resource resource, Permissions permissions) => true;
}

/// <summary>A user acting through the web UI, or a background task running for that user.</summary>
public sealed record UserPrincipal(long UserId) : Principal
{
    public override long GrantHolderId => UserId;
}

/// <summary>Work that belongs to no user, such as migrations, cleanup and incoming webhooks.</summary>
/// <remarks>Only accepted by methods named <c>...ForSystemAsync</c>, which run their own queries without the access check.</remarks>
public sealed record SystemPrincipal(string Reason);
