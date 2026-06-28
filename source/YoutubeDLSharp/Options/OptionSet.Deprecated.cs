using System;

namespace YoutubeDLSharp.Options;

public partial class OptionSet
{
    private Option<bool> _getDescription = new("--get-description");
    private Option<bool> _getDuration = new("--get-duration");
    private Option<bool> _getFilename = new("--get-filename");
    private Option<bool> _getFormat = new("--get-format");
    private Option<bool> _getId = new("--get-id");
    private Option<bool> _getThumbnail = new("--get-thumbnail");
    private Option<bool> _getTitle = new("-e", "--get-title");
    private Option<bool> _getUrl = new("-g", "--get-url");
    private Option<string> _matchTitle = new("--match-title");
    private Option<string> _rejectTitle = new("--reject-title");
    private Option<long?> _minViews = new("--min-views");
    private Option<long?> _maxViews = new("--max-views");
    private Option<bool> _breakOnReject = new("--break-on-reject");
    private Option<string> _userAgent = new("--user-agent");
    private Option<string> _referer = new("--referer");
    private Option<int?> _playlistStart = new("--playlist-start");
    private Option<int?> _playlistEnd = new("--playlist-end");
    private Option<bool> _playlistReverse = new("--playlist-reverse");
    private Option<bool> _noColors = new("--no-colors");
    private Option<bool> _forceGenericExtractor = new("--force-generic-extractor");
    private Option<string> _execBeforeDownload = new("--exec-before-download");
    private Option<bool> _noExecBeforeDownload = new("--no-exec-before-download");
    private Option<bool> _allFormats = new("--all-formats");
    private Option<bool> _allSubs = new("--all-subs");
    private Option<bool> _printJson = new("--print-json");
    private Option<string> _autonumberSize = new("--autonumber-size");
    private Option<int?> _autonumberStart = new("--autonumber-start");
    private Option<bool> _id = new("--id");
    private Option<string> _metadataFromTitle = new("--metadata-from-title");
    private Option<bool> _hlsPreferNative = new("--hls-prefer-native");
    private Option<bool> _hlsPreferFfmpeg = new("--hls-prefer-ffmpeg");
    private Option<bool> _listFormatsOld = new("--list-formats-old", "--no-list-formats-as-table");
    private Option<bool> _listFormatsAsTable = new("--list-formats-as-table", "--no-list-formats-old");
    private Option<bool> _youtubeSkipDashManifest = new("--youtube-skip-dash-manifest", "--no-youtube-include-dash-manifest");
    private Option<bool> _youtubeSkipHlsManifest = new("--youtube-skip-hls-manifest", "--no-youtube-include-hls-manifest");
    private Option<bool> _geoBypass = new("--geo-bypass");
    private Option<bool> _noGeoBypass = new("--no-geo-bypass");
    private Option<string> _geoBypassCountry = new("--geo-bypass-country");
    private Option<string> _geoBypassIpBlock = new("--geo-bypass-ip-block");

    /// <summary>
    /// Deprecated in favor of: --print description.
    /// </summary>
    [Obsolete("Deprecated in favor of: --print description.")]
    public bool GetDescription
    {
        get => _getDescription.Value;
        set => _getDescription.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --print duration_string.
    /// </summary>
    [Obsolete("Deprecated in favor of: --print duration_string.")]
    public bool GetDuration
    {
        get => _getDuration.Value;
        set => _getDuration.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --print filename.
    /// </summary>
    [Obsolete("Deprecated in favor of: --print filename.")]
    public bool GetFilename
    {
        get => _getFilename.Value;
        set => _getFilename.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --print format.
    /// </summary>
    [Obsolete("Deprecated in favor of: --print format.")]
    public bool GetFormat
    {
        get => _getFormat.Value;
        set => _getFormat.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --print id.
    /// </summary>
    [Obsolete("Deprecated in favor of: --print id.")]
    public bool GetId
    {
        get => _getId.Value;
        set => _getId.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --print thumbnail.
    /// </summary>
    [Obsolete("Deprecated in favor of: --print thumbnail.")]
    public bool GetThumbnail
    {
        get => _getThumbnail.Value;
        set => _getThumbnail.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --print title.
    /// </summary>
    [Obsolete("Deprecated in favor of: --print title.")]
    public bool GetTitle
    {
        get => _getTitle.Value;
        set => _getTitle.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --print urls.
    /// </summary>
    [Obsolete("Deprecated in favor of: --print urls.")]
    public bool GetUrl
    {
        get => _getUrl.Value;
        set => _getUrl.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --match-filter &quot;title ~= (?i)REGEX&quot;.
    /// </summary>
    [Obsolete("Deprecated in favor of: --match-filter \"title ~= (?i)REGEX\".")]
    public string? MatchTitle
    {
        get => _matchTitle.Value;
        set => _matchTitle.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --match-filter &quot;title !~= (?i)REGEX&quot;.
    /// </summary>
    [Obsolete("Deprecated in favor of: --match-filter \"title !~= (?i)REGEX\".")]
    public string? RejectTitle
    {
        get => _rejectTitle.Value;
        set => _rejectTitle.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --match-filter &quot;view_count &gt;=? COUNT&quot;.
    /// </summary>
    [Obsolete("Deprecated in favor of: --match-filter \"view_count >=? COUNT\".")]
    public long? MinViews
    {
        get => _minViews.Value;
        set => _minViews.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --match-filter &quot;view_count &lt;=? COUNT&quot;.
    /// </summary>
    [Obsolete("Deprecated in favor of: --match-filter \"view_count <=? COUNT\".")]
    public long? MaxViews
    {
        get => _maxViews.Value;
        set => _maxViews.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: Use --break-match-filter.
    /// </summary>
    [Obsolete("Deprecated in favor of: Use --break-match-filter.")]
    public bool BreakOnReject
    {
        get => _breakOnReject.Value;
        set => _breakOnReject.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --add-header &quot;User-Agent:UA&quot;.
    /// </summary>
    [Obsolete("Deprecated in favor of: --add-header \"User-Agent:UA\".")]
    public string? UserAgent
    {
        get => _userAgent.Value;
        set => _userAgent.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --add-header &quot;Referer:URL&quot;.
    /// </summary>
    [Obsolete("Deprecated in favor of: --add-header \"Referer:URL\".")]
    public string? Referer
    {
        get => _referer.Value;
        set => _referer.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: -I NUMBER:.
    /// </summary>
    [Obsolete("Deprecated in favor of: -I NUMBER:.")]
    public int? PlaylistStart
    {
        get => _playlistStart.Value;
        set => _playlistStart.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: -I :NUMBER.
    /// </summary>
    [Obsolete("Deprecated in favor of: -I :NUMBER.")]
    public int? PlaylistEnd
    {
        get => _playlistEnd.Value;
        set => _playlistEnd.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: -I ::-1.
    /// </summary>
    [Obsolete("Deprecated in favor of: -I ::-1.")]
    public bool PlaylistReverse
    {
        get => _playlistReverse.Value;
        set => _playlistReverse.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --color no_color.
    /// </summary>
    [Obsolete("Deprecated in favor of: --color no_color.")]
    public bool NoColors
    {
        get => _noColors.Value;
        set => _noColors.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --ies generic,default.
    /// </summary>
    [Obsolete("Deprecated in favor of: --ies generic,default.")]
    public bool ForceGenericExtractor
    {
        get => _forceGenericExtractor.Value;
        set => _forceGenericExtractor.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --exec &quot;before_dl:CMD&quot;.
    /// </summary>
    [Obsolete("Deprecated in favor of: --exec \"before_dl:CMD\".")]
    public string? ExecBeforeDownload
    {
        get => _execBeforeDownload.Value;
        set => _execBeforeDownload.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --no-exec.
    /// </summary>
    [Obsolete("Deprecated in favor of: --no-exec.")]
    public bool NoExecBeforeDownload
    {
        get => _noExecBeforeDownload.Value;
        set => _noExecBeforeDownload.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: -f all.
    /// </summary>
    [Obsolete("Deprecated in favor of: -f all.")]
    public bool AllFormats
    {
        get => _allFormats.Value;
        set => _allFormats.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --sub-langs all --write-subs.
    /// </summary>
    [Obsolete("Deprecated in favor of: --sub-langs all --write-subs.")]
    public bool AllSubs
    {
        get => _allSubs.Value;
        set => _allSubs.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: -j --no-simulate.
    /// </summary>
    [Obsolete("Deprecated in favor of: -j --no-simulate.")]
    public bool PrintJson
    {
        get => _printJson.Value;
        set => _printJson.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: Use string formatting, e.g. %(autonumber)03d.
    /// </summary>
    [Obsolete("Deprecated in favor of: Use string formatting, e.g. %(autonumber)03d.")]
    public string? AutonumberSize
    {
        get => _autonumberSize.Value;
        set => _autonumberSize.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: Use internal field formatting like %(autonumber+NUMBER)s.
    /// </summary>
    [Obsolete("Deprecated in favor of: Use internal field formatting like %(autonumber+NUMBER)s.")]
    public int? AutonumberStart
    {
        get => _autonumberStart.Value;
        set => _autonumberStart.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: -o &quot;%(id)s.%(ext)s&quot;.
    /// </summary>
    [Obsolete("Deprecated in favor of: -o \"%(id)s.%(ext)s\".")]
    public bool Id
    {
        get => _id.Value;
        set => _id.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --parse-metadata &quot;%(title)s:FORMAT&quot;.
    /// </summary>
    [Obsolete("Deprecated in favor of: --parse-metadata \"%(title)s:FORMAT\".")]
    public string? MetadataFromTitle
    {
        get => _metadataFromTitle.Value;
        set => _metadataFromTitle.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --downloader &quot;m3u8:native&quot;.
    /// </summary>
    [Obsolete("Deprecated in favor of: --downloader \"m3u8:native\".")]
    public bool HlsPreferNative
    {
        get => _hlsPreferNative.Value;
        set => _hlsPreferNative.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --downloader &quot;m3u8:ffmpeg&quot;.
    /// </summary>
    [Obsolete("Deprecated in favor of: --downloader \"m3u8:ffmpeg\".")]
    public bool HlsPreferFfmpeg
    {
        get => _hlsPreferFfmpeg.Value;
        set => _hlsPreferFfmpeg.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --compat-options list-formats (Alias: --no-list-formats-as-table).
    /// </summary>
    [Obsolete("Deprecated in favor of: --compat-options list-formats (Alias: --no-list-formats-as-table).")]
    public bool ListFormatsOld
    {
        get => _listFormatsOld.Value;
        set => _listFormatsOld.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --compat-options -list-formats [Default] (Alias: --no-list-formats-old).
    /// </summary>
    [Obsolete("Deprecated in favor of: --compat-options -list-formats [Default] (Alias: --no-list-formats-old).")]
    public bool ListFormatsAsTable
    {
        get => _listFormatsAsTable.Value;
        set => _listFormatsAsTable.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --extractor-args &quot;youtube:skip=dash&quot; (Alias: --no-youtube-include-dash-manifest).
    /// </summary>
    [Obsolete("Deprecated in favor of: --extractor-args \"youtube:skip=dash\" (Alias: --no-youtube-include-dash-manifest).")]
    public bool YoutubeSkipDashManifest
    {
        get => _youtubeSkipDashManifest.Value;
        set => _youtubeSkipDashManifest.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --extractor-args &quot;youtube:skip=hls&quot; (Alias: --no-youtube-include-hls-manifest).
    /// </summary>
    [Obsolete("Deprecated in favor of: --extractor-args \"youtube:skip=hls\" (Alias: --no-youtube-include-hls-manifest).")]
    public bool YoutubeSkipHlsManifest
    {
        get => _youtubeSkipHlsManifest.Value;
        set => _youtubeSkipHlsManifest.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --xff &quot;default&quot;.
    /// </summary>
    [Obsolete("Deprecated in favor of: --xff \"default\".")]
    public bool GeoBypass
    {
        get => _geoBypass.Value;
        set => _geoBypass.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --xff &quot;never&quot;.
    /// </summary>
    [Obsolete("Deprecated in favor of: --xff \"never\".")]
    public bool NoGeoBypass
    {
        get => _noGeoBypass.Value;
        set => _noGeoBypass.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --xff CODE.
    /// </summary>
    [Obsolete("Deprecated in favor of: --xff CODE.")]
    public string? GeoBypassCountry
    {
        get => _geoBypassCountry.Value;
        set => _geoBypassCountry.Value = value;
    }

    /// <summary>
    /// Deprecated in favor of: --xff IP_BLOCK.
    /// </summary>
    [Obsolete("Deprecated in favor of: --xff IP_BLOCK.")]
    public string? GeoBypassIpBlock
    {
        get => _geoBypassIpBlock.Value;
        set => _geoBypassIpBlock.Value = value;
    }
}
