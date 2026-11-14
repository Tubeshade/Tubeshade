namespace Tubeshade.Server.Configuration.Auth.Options;

internal static class Claims
{
    public const string UserId = "TubeshadeUserId";

    // Added to the request's principal on each request and never issued, since the id is internal.
    public const string PrincipalId = "TubeshadePrincipalId";
}
