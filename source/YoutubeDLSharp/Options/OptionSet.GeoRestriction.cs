namespace YoutubeDLSharp.Options;

public partial class OptionSet
{
    private Option<string> _geoVerificationProxy = new("--geo-verification-proxy");
    private Option<string> _xff = new("--xff");

    /// <summary>
    /// Use this proxy to verify the IP address for
    /// some geo-restricted sites. The default proxy
    /// specified by --proxy (or none, if the option
    /// is not present) is used for the actual
    /// downloading
    /// </summary>
    public string? GeoVerificationProxy
    {
        get => _geoVerificationProxy.Value;
        set => _geoVerificationProxy.Value = value;
    }

    /// <summary>
    /// How to fake X-Forwarded-For HTTP header to
    /// try bypassing geographic restriction. One of
    /// &quot;default&quot; (only when known to be useful),
    /// &quot;never&quot;, an IP block in CIDR notation, or a
    /// two-letter ISO 3166-2 country code
    /// </summary>
    public string? Xff
    {
        get => _xff.Value;
        set => _xff.Value = value;
    }
}
