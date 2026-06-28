namespace YoutubeDLSharp.Options;

public partial class OptionSet
{
    private Option<int?> _extractorRetries = new("--extractor-retries");
    private Option<bool> _allowDynamicMpd = new("--allow-dynamic-mpd", "--no-ignore-dynamic-mpd");
    private Option<bool> _ignoreDynamicMpd = new("--ignore-dynamic-mpd", "--no-allow-dynamic-mpd");
    private Option<bool> _hlsSplitDiscontinuity = new("--hls-split-discontinuity");
    private Option<bool> _noHlsSplitDiscontinuity = new("--no-hls-split-discontinuity");
    private MultiOption<string> _extractorArgs = new("--extractor-args");

    /// <summary>
    /// Number of retries for known extractor errors
    /// (default is 3), or &quot;infinite&quot;
    /// </summary>
    public int? ExtractorRetries
    {
        get => _extractorRetries.Value;
        set => _extractorRetries.Value = value;
    }

    /// <summary>
    /// Process dynamic DASH manifests (default)
    /// (Alias: --no-ignore-dynamic-mpd)
    /// </summary>
    public bool AllowDynamicMpd
    {
        get => _allowDynamicMpd.Value;
        set => _allowDynamicMpd.Value = value;
    }

    /// <summary>
    /// Do not process dynamic DASH manifests
    /// (Alias: --no-allow-dynamic-mpd)
    /// </summary>
    public bool IgnoreDynamicMpd
    {
        get => _ignoreDynamicMpd.Value;
        set => _ignoreDynamicMpd.Value = value;
    }

    /// <summary>
    /// Split HLS playlists to different formats at
    /// discontinuities such as ad breaks
    /// </summary>
    public bool HlsSplitDiscontinuity
    {
        get => _hlsSplitDiscontinuity.Value;
        set => _hlsSplitDiscontinuity.Value = value;
    }

    /// <summary>
    /// Do not split HLS playlists into different
    /// formats at discontinuities such as ad breaks
    /// (default)
    /// </summary>
    public bool NoHlsSplitDiscontinuity
    {
        get => _noHlsSplitDiscontinuity.Value;
        set => _noHlsSplitDiscontinuity.Value = value;
    }

    /// <summary>
    /// Pass ARGS arguments to the IE_KEY extractor.
    /// See &quot;EXTRACTOR ARGUMENTS&quot; for details. You
    /// can use this option multiple times to give
    /// arguments for different extractors
    /// </summary>
    public MultiValue<string>? ExtractorArgs
    {
        get => _extractorArgs.Value;
        set => _extractorArgs.Value = value;
    }
}
