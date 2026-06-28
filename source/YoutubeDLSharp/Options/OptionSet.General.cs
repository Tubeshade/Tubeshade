namespace YoutubeDLSharp.Options;

public partial class OptionSet
{
    private Option<bool> _help = new("-h", "--help");
    private Option<bool> _version = new("--version");
    private Option<bool> _update = new("-U", "--update");
    private Option<bool> _noUpdate = new("--no-update");
    private Option<string> _updateTo = new("--update-to");
    private Option<bool> _ignoreErrors = new("-i", "--ignore-errors");
    private Option<bool> _noAbortOnError = new("--no-abort-on-error");
    private Option<bool> _abortOnError = new("--abort-on-error", "--no-ignore-errors");
    private Option<bool> _dumpUserAgent = new("--dump-user-agent");
    private Option<bool> _listExtractors = new("--list-extractors");
    private Option<bool> _extractorDescriptions = new("--extractor-descriptions");
    private Option<string> _useExtractors = new("--use-extractors", "--ies");
    private Option<string> _defaultSearch = new("--default-search");
    private Option<bool> _ignoreConfig = new("--ignore-config", "--no-config");
    private Option<bool> _noConfigLocations = new("--no-config-locations");
    private MultiOption<string> _configLocations = new("--config-locations");
    private MultiOption<string> _pluginDirs = new("--plugin-dirs");
    private Option<bool> _noPluginDirs = new("--no-plugin-dirs");
    private Option<bool> _flatPlaylist = new("--flat-playlist");
    private Option<bool> _noFlatPlaylist = new("--no-flat-playlist");
    private Option<bool> _liveFromStart = new("--live-from-start");
    private Option<bool> _noLiveFromStart = new("--no-live-from-start");
    private Option<string> _waitForVideo = new("--wait-for-video");
    private Option<bool> _noWaitForVideo = new("--no-wait-for-video");
    private Option<bool> _markWatched = new("--mark-watched");
    private Option<bool> _noMarkWatched = new("--no-mark-watched");
    private MultiOption<string> _color = new("--color");
    private Option<string> _compatOptions = new("--compat-options");
    private MultiOption<string> _alias = new("--alias");

    /// <summary>
    /// Print this help text and exit
    /// </summary>
    public bool Help
    {
        get => _help.Value;
        set => _help.Value = value;
    }

    /// <summary>
    /// Print program version and exit
    /// </summary>
    public bool Version
    {
        get => _version.Value;
        set => _version.Value = value;
    }

    /// <summary>
    /// Update this program to the latest stable
    /// version
    /// </summary>
    public bool Update
    {
        get => _update.Value;
        set => _update.Value = value;
    }

    /// <summary>
    /// Do not check for updates (default)
    /// </summary>
    public bool NoUpdate
    {
        get => _noUpdate.Value;
        set => _noUpdate.Value = value;
    }

    /// <summary>
    /// Upgrade/downgrade to a specific version.
    /// CHANNEL can be a repository as well. CHANNEL
    /// and TAG default to &quot;stable&quot; and &quot;latest&quot;
    /// respectively if omitted; See &quot;UPDATE&quot; for
    /// details. Supported channels: stable,
    /// nightly, master
    /// </summary>
    public string? UpdateTo
    {
        get => _updateTo.Value;
        set => _updateTo.Value = value;
    }

    /// <summary>
    /// Ignore download and postprocessing errors.
    /// The download will be considered successful
    /// even if the postprocessing fails
    /// </summary>
    public bool IgnoreErrors
    {
        get => _ignoreErrors.Value;
        set => _ignoreErrors.Value = value;
    }

    /// <summary>
    /// Continue with next video on download errors;
    /// e.g. to skip unavailable videos in a
    /// playlist (default)
    /// </summary>
    public bool NoAbortOnError
    {
        get => _noAbortOnError.Value;
        set => _noAbortOnError.Value = value;
    }

    /// <summary>
    /// Abort downloading of further videos if an
    /// error occurs (Alias: --no-ignore-errors)
    /// </summary>
    public bool AbortOnError
    {
        get => _abortOnError.Value;
        set => _abortOnError.Value = value;
    }

    /// <summary>
    /// Display the current user-agent and exit
    /// </summary>
    public bool DumpUserAgent
    {
        get => _dumpUserAgent.Value;
        set => _dumpUserAgent.Value = value;
    }

    /// <summary>
    /// List all supported extractors and exit
    /// </summary>
    public bool ListExtractors
    {
        get => _listExtractors.Value;
        set => _listExtractors.Value = value;
    }

    /// <summary>
    /// Output descriptions of all supported
    /// extractors and exit
    /// </summary>
    public bool ExtractorDescriptions
    {
        get => _extractorDescriptions.Value;
        set => _extractorDescriptions.Value = value;
    }

    /// <summary>
    /// Extractor names to use separated by commas.
    /// You can also use regexes, &quot;all&quot;, &quot;default&quot;
    /// and &quot;end&quot; (end URL matching); e.g. --ies
    /// &quot;holodex.*,end,youtube&quot;. Prefix the name
    /// with a &quot;-&quot; to exclude it, e.g. --ies
    /// default,-generic. Use --list-extractors for
    /// a list of extractor names. (Alias: --ies)
    /// </summary>
    public string? UseExtractors
    {
        get => _useExtractors.Value;
        set => _useExtractors.Value = value;
    }

    /// <summary>
    /// Use this prefix for unqualified URLs. E.g.
    /// &quot;gvsearch2:python&quot; downloads two videos from
    /// google videos for the search term &quot;python&quot;.
    /// Use the value &quot;auto&quot; to let yt-dlp guess
    /// (&quot;auto_warning&quot; to emit a warning when
    /// guessing). &quot;error&quot; just throws an error. The
    /// default value &quot;fixup_error&quot; repairs broken
    /// URLs, but emits an error if this is not
    /// possible instead of searching
    /// </summary>
    public string? DefaultSearch
    {
        get => _defaultSearch.Value;
        set => _defaultSearch.Value = value;
    }

    /// <summary>
    /// Don&#x27;t load any more configuration files
    /// except those given to --config-locations.
    /// For backward compatibility, if this option
    /// is found inside the system configuration
    /// file, the user configuration is not loaded.
    /// (Alias: --no-config)
    /// </summary>
    public bool IgnoreConfig
    {
        get => _ignoreConfig.Value;
        set => _ignoreConfig.Value = value;
    }

    /// <summary>
    /// Do not load any custom configuration files
    /// (default). When given inside a configuration
    /// file, ignore all previous --config-locations
    /// defined in the current file
    /// </summary>
    public bool NoConfigLocations
    {
        get => _noConfigLocations.Value;
        set => _noConfigLocations.Value = value;
    }

    /// <summary>
    /// Location of the main configuration file;
    /// either the path to the config or its
    /// containing directory (&quot;-&quot; for stdin). Can be
    /// used multiple times and inside other
    /// configuration files
    /// </summary>
    public MultiValue<string>? ConfigLocations
    {
        get => _configLocations.Value;
        set => _configLocations.Value = value;
    }

    /// <summary>
    /// Path to an additional directory to search
    /// for plugins. This option can be used
    /// multiple times to add multiple directories.
    /// Use &quot;default&quot; to search the default plugin
    /// directories (default)
    /// </summary>
    public MultiValue<string>? PluginDirs
    {
        get => _pluginDirs.Value;
        set => _pluginDirs.Value = value;
    }

    /// <summary>
    /// Clear plugin directories to search,
    /// including defaults and those provided by
    /// previous --plugin-dirs
    /// </summary>
    public bool NoPluginDirs
    {
        get => _noPluginDirs.Value;
        set => _noPluginDirs.Value = value;
    }

    /// <summary>
    /// Do not extract a playlist&#x27;s URL result
    /// entries; some entry metadata may be missing
    /// and downloading may be bypassed
    /// </summary>
    public bool FlatPlaylist
    {
        get => _flatPlaylist.Value;
        set => _flatPlaylist.Value = value;
    }

    /// <summary>
    /// Fully extract the videos of a playlist
    /// (default)
    /// </summary>
    public bool NoFlatPlaylist
    {
        get => _noFlatPlaylist.Value;
        set => _noFlatPlaylist.Value = value;
    }

    /// <summary>
    /// Download livestreams from the start.
    /// Currently only supported for YouTube
    /// (Experimental)
    /// </summary>
    public bool LiveFromStart
    {
        get => _liveFromStart.Value;
        set => _liveFromStart.Value = value;
    }

    /// <summary>
    /// Download livestreams from the current time
    /// (default)
    /// </summary>
    public bool NoLiveFromStart
    {
        get => _noLiveFromStart.Value;
        set => _noLiveFromStart.Value = value;
    }

    /// <summary>
    /// Wait for scheduled streams to become
    /// available. Pass the minimum number of
    /// seconds (or range) to wait between retries
    /// </summary>
    public string? WaitForVideo
    {
        get => _waitForVideo.Value;
        set => _waitForVideo.Value = value;
    }

    /// <summary>
    /// Do not wait for scheduled streams (default)
    /// </summary>
    public bool NoWaitForVideo
    {
        get => _noWaitForVideo.Value;
        set => _noWaitForVideo.Value = value;
    }

    /// <summary>
    /// Mark videos watched (even with --simulate)
    /// </summary>
    public bool MarkWatched
    {
        get => _markWatched.Value;
        set => _markWatched.Value = value;
    }

    /// <summary>
    /// Do not mark videos watched (default)
    /// </summary>
    public bool NoMarkWatched
    {
        get => _noMarkWatched.Value;
        set => _noMarkWatched.Value = value;
    }

    /// <summary>
    /// Whether to emit color codes in output,
    /// optionally prefixed by the STREAM (stdout or
    /// stderr) to apply the setting to. Can be one
    /// of &quot;always&quot;, &quot;auto&quot; (default), &quot;never&quot;, or
    /// &quot;no_color&quot; (use non color terminal
    /// sequences). Use &quot;auto-tty&quot; or &quot;no_color-tty&quot;
    /// to decide based on terminal support only.
    /// Can be used multiple times
    /// </summary>
    public MultiValue<string>? Color
    {
        get => _color.Value;
        set => _color.Value = value;
    }

    /// <summary>
    /// Options that can help keep compatibility
    /// with youtube-dl or youtube-dlc
    /// configurations by reverting some of the
    /// changes made in yt-dlp. See &quot;Differences in
    /// default behavior&quot; for details
    /// </summary>
    public string? CompatOptions
    {
        get => _compatOptions.Value;
        set => _compatOptions.Value = value;
    }

    /// <summary>
    /// Create aliases for an option string. Unless
    /// an alias starts with a dash &quot;-&quot;, it is
    /// prefixed with &quot;--&quot;. Arguments are parsed
    /// according to the Python string formatting
    /// mini-language. E.g. --alias get-audio,-X
    /// &quot;-S=aext:{0},abr -x --audio-format {0}&quot;
    /// creates options &quot;--get-audio&quot; and &quot;-X&quot; that
    /// takes an argument (ARG0) and expands to
    /// &quot;-S=aext:ARG0,abr -x --audio-format ARG0&quot;.
    /// All defined aliases are listed in the --help
    /// output. Alias options can trigger more
    /// aliases; so be careful to avoid defining
    /// recursive options. As a safety measure, each
    /// alias may be triggered a maximum of 100
    /// times. This option can be used multiple
    /// times
    /// </summary>
    public MultiValue<string>? Alias
    {
        get => _alias.Value;
        set => _alias.Value = value;
    }
}
