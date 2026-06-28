namespace YoutubeDLSharp.Options;

public partial class OptionSet
{
    private Option<string> _proxy = new("--proxy");
    private Option<int?> _socketTimeout = new("--socket-timeout");
    private Option<string> _sourceAddress = new("--source-address");
    private Option<string> _impersonate = new("--impersonate");
    private Option<bool> _listImpersonateTargets = new("--list-impersonate-targets");
    private Option<bool> _forceIPv4 = new("-4", "--force-ipv4");
    private Option<bool> _forceIPv6 = new("-6", "--force-ipv6");
    private Option<bool> _enableFileUrls = new("--enable-file-urls");

    /// <summary>
    /// Use the specified HTTP/HTTPS/SOCKS proxy. To
    /// enable SOCKS proxy, specify a proper scheme,
    /// e.g. socks5://user:pass@127.0.0.1:1080/.
    /// Pass in an empty string (--proxy &quot;&quot;) for
    /// direct connection
    /// </summary>
    public string? Proxy
    {
        get => _proxy.Value;
        set => _proxy.Value = value;
    }

    /// <summary>
    /// Time to wait before giving up, in seconds
    /// </summary>
    public int? SocketTimeout
    {
        get => _socketTimeout.Value;
        set => _socketTimeout.Value = value;
    }

    /// <summary>
    /// Client-side IP address to bind to
    /// </summary>
    public string? SourceAddress
    {
        get => _sourceAddress.Value;
        set => _sourceAddress.Value = value;
    }

    /// <summary>
    /// Client to impersonate for requests. E.g.
    /// chrome, chrome-110, chrome:windows-10. Pass
    /// --impersonate=&quot;&quot; to impersonate any client.
    /// Note that forcing impersonation for all
    /// requests may have a detrimental impact on
    /// download speed and stability
    /// </summary>
    public string? Impersonate
    {
        get => _impersonate.Value;
        set => _impersonate.Value = value;
    }

    /// <summary>
    /// List available clients to impersonate.
    /// </summary>
    public bool ListImpersonateTargets
    {
        get => _listImpersonateTargets.Value;
        set => _listImpersonateTargets.Value = value;
    }

    /// <summary>
    /// Make all connections via IPv4
    /// </summary>
    public bool ForceIPv4
    {
        get => _forceIPv4.Value;
        set => _forceIPv4.Value = value;
    }

    /// <summary>
    /// Make all connections via IPv6
    /// </summary>
    public bool ForceIPv6
    {
        get => _forceIPv6.Value;
        set => _forceIPv6.Value = value;
    }

    /// <summary>
    /// Enable file:// URLs. This is disabled by
    /// default for security reasons.
    /// </summary>
    public bool EnableFileUrls
    {
        get => _enableFileUrls.Value;
        set => _enableFileUrls.Value = value;
    }
}
