namespace YoutubeDLSharp.Options;

public partial class OptionSet
{
    private Option<string> _encoding = new("--encoding");
    private Option<bool> _legacyServerConnect = new("--legacy-server-connect");
    private Option<bool> _noCheckCertificates = new("--no-check-certificates");
    private Option<bool> _preferInsecure = new("--prefer-insecure");
    private MultiOption<string> _addHeaders = new("--add-headers");
    private Option<bool> _bidiWorkaround = new("--bidi-workaround");
    private Option<int?> _sleepRequests = new("--sleep-requests");
    private Option<int?> _sleepInterval = new("--sleep-interval", "--min-sleep-interval");
    private Option<int?> _maxSleepInterval = new("--max-sleep-interval");
    private Option<int?> _sleepSubtitles = new("--sleep-subtitles");

    /// <summary>
    /// Force the specified encoding (experimental)
    /// </summary>
    public string? Encoding
    {
        get => _encoding.Value;
        set => _encoding.Value = value;
    }

    /// <summary>
    /// Explicitly allow HTTPS connection to servers
    /// that do not support RFC 5746 secure
    /// renegotiation
    /// </summary>
    public bool LegacyServerConnect
    {
        get => _legacyServerConnect.Value;
        set => _legacyServerConnect.Value = value;
    }

    /// <summary>
    /// Suppress HTTPS certificate validation
    /// </summary>
    public bool NoCheckCertificates
    {
        get => _noCheckCertificates.Value;
        set => _noCheckCertificates.Value = value;
    }

    /// <summary>
    /// Use an unencrypted connection to retrieve
    /// information about the video (Currently
    /// supported only for YouTube)
    /// </summary>
    public bool PreferInsecure
    {
        get => _preferInsecure.Value;
        set => _preferInsecure.Value = value;
    }

    /// <summary>
    /// Specify a custom HTTP header and its value,
    /// separated by a colon &quot;:&quot;. You can use this
    /// option multiple times
    /// </summary>
    public MultiValue<string>? AddHeaders
    {
        get => _addHeaders.Value;
        set => _addHeaders.Value = value;
    }

    /// <summary>
    /// Work around terminals that lack
    /// bidirectional text support. Requires bidiv
    /// or fribidi executable in PATH
    /// </summary>
    public bool BidiWorkaround
    {
        get => _bidiWorkaround.Value;
        set => _bidiWorkaround.Value = value;
    }

    /// <summary>
    /// Number of seconds to sleep between requests
    /// during data extraction
    /// </summary>
    public int? SleepRequests
    {
        get => _sleepRequests.Value;
        set => _sleepRequests.Value = value;
    }

    /// <summary>
    /// Number of seconds to sleep before each
    /// download. This is the minimum time to sleep
    /// when used along with --max-sleep-interval
    /// (Alias: --min-sleep-interval)
    /// </summary>
    public int? SleepInterval
    {
        get => _sleepInterval.Value;
        set => _sleepInterval.Value = value;
    }

    /// <summary>
    /// Maximum number of seconds to sleep. Can only
    /// be used along with --min-sleep-interval
    /// </summary>
    public int? MaxSleepInterval
    {
        get => _maxSleepInterval.Value;
        set => _maxSleepInterval.Value = value;
    }

    /// <summary>
    /// Number of seconds to sleep before each
    /// subtitle download
    /// </summary>
    public int? SleepSubtitles
    {
        get => _sleepSubtitles.Value;
        set => _sleepSubtitles.Value = value;
    }
}
