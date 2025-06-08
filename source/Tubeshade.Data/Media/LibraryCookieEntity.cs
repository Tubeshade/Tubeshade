using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media;

public sealed record LibraryCookieEntity : ModifiableEntity
{
    public required string Domain { get; set; }

    /// <seealso href="https://everything.curl.dev/http/cookies/fileformat.html"/>
    /// <seealso href="https://curl.haxx.se/rfc/cookie_spec.html"/>
    public required string Cookie { get; set; }
}
