namespace YoutubeDLSharp.Options;

public partial class OptionSet
{
    private Option<string> _format = new("-f", "--format");
    private Option<string> _formatSort = new("-S", "--format-sort");
    private Option<bool> _formatSortForce = new("--format-sort-force", "--S-force");
    private Option<bool> _noFormatSortForce = new("--no-format-sort-force");
    private Option<bool> _videoMultistreams = new("--video-multistreams");
    private Option<bool> _noVideoMultistreams = new("--no-video-multistreams");
    private Option<bool> _audioMultistreams = new("--audio-multistreams");
    private Option<bool> _noAudioMultistreams = new("--no-audio-multistreams");
    private Option<bool> _preferFreeFormats = new("--prefer-free-formats");
    private Option<bool> _noPreferFreeFormats = new("--no-prefer-free-formats");
    private Option<bool> _checkFormats = new("--check-formats");
    private Option<bool> _checkAllFormats = new("--check-all-formats");
    private Option<bool> _noCheckFormats = new("--no-check-formats");
    private Option<bool> _listFormats = new("-F", "--list-formats");
    private Option<DownloadMergeFormat> _mergeOutputFormat = new("--merge-output-format");

    /// <summary>
    /// Video format code, see &quot;FORMAT SELECTION&quot;
    /// for more details
    /// </summary>
    public string? Format
    {
        get => _format.Value;
        set => _format.Value = value;
    }

    /// <summary>
    /// Sort the formats by the fields given, see
    /// &quot;Sorting Formats&quot; for more details
    /// </summary>
    public string? FormatSort
    {
        get => _formatSort.Value;
        set => _formatSort.Value = value;
    }

    /// <summary>
    /// Force user specified sort order to have
    /// precedence over all fields, see &quot;Sorting
    /// Formats&quot; for more details (Alias: --S-force)
    /// </summary>
    public bool FormatSortForce
    {
        get => _formatSortForce.Value;
        set => _formatSortForce.Value = value;
    }

    /// <summary>
    /// Some fields have precedence over the user
    /// specified sort order (default)
    /// </summary>
    public bool NoFormatSortForce
    {
        get => _noFormatSortForce.Value;
        set => _noFormatSortForce.Value = value;
    }

    /// <summary>
    /// Allow multiple video streams to be merged
    /// into a single file
    /// </summary>
    public bool VideoMultistreams
    {
        get => _videoMultistreams.Value;
        set => _videoMultistreams.Value = value;
    }

    /// <summary>
    /// Only one video stream is downloaded for each
    /// output file (default)
    /// </summary>
    public bool NoVideoMultistreams
    {
        get => _noVideoMultistreams.Value;
        set => _noVideoMultistreams.Value = value;
    }

    /// <summary>
    /// Allow multiple audio streams to be merged
    /// into a single file
    /// </summary>
    public bool AudioMultistreams
    {
        get => _audioMultistreams.Value;
        set => _audioMultistreams.Value = value;
    }

    /// <summary>
    /// Only one audio stream is downloaded for each
    /// output file (default)
    /// </summary>
    public bool NoAudioMultistreams
    {
        get => _noAudioMultistreams.Value;
        set => _noAudioMultistreams.Value = value;
    }

    /// <summary>
    /// Prefer video formats with free containers
    /// over non-free ones of the same quality. Use
    /// with &quot;-S ext&quot; to strictly prefer free
    /// containers irrespective of quality
    /// </summary>
    public bool PreferFreeFormats
    {
        get => _preferFreeFormats.Value;
        set => _preferFreeFormats.Value = value;
    }

    /// <summary>
    /// Don&#x27;t give any special preference to free
    /// containers (default)
    /// </summary>
    public bool NoPreferFreeFormats
    {
        get => _noPreferFreeFormats.Value;
        set => _noPreferFreeFormats.Value = value;
    }

    /// <summary>
    /// Make sure formats are selected only from
    /// those that are actually downloadable
    /// </summary>
    public bool CheckFormats
    {
        get => _checkFormats.Value;
        set => _checkFormats.Value = value;
    }

    /// <summary>
    /// Check all formats for whether they are
    /// actually downloadable
    /// </summary>
    public bool CheckAllFormats
    {
        get => _checkAllFormats.Value;
        set => _checkAllFormats.Value = value;
    }

    /// <summary>
    /// Do not check that the formats are actually
    /// downloadable
    /// </summary>
    public bool NoCheckFormats
    {
        get => _noCheckFormats.Value;
        set => _noCheckFormats.Value = value;
    }

    /// <summary>
    /// List available formats of each video.
    /// Simulate unless --no-simulate is used
    /// </summary>
    public bool ListFormats
    {
        get => _listFormats.Value;
        set => _listFormats.Value = value;
    }

    /// <summary>
    /// Containers that may be used when merging
    /// formats, separated by &quot;/&quot;, e.g. &quot;mp4/mkv&quot;.
    /// Ignored if no merge is required. (currently
    /// supported: avi, flv, mkv, mov, mp4, webm)
    /// </summary>
    public DownloadMergeFormat MergeOutputFormat
    {
        get => _mergeOutputFormat.Value;
        set => _mergeOutputFormat.Value = value;
    }
}
