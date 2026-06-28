namespace YoutubeDLSharp.Options;

public partial class OptionSet
{
    private Option<int?> _concurrentFragments = new("-N", "--concurrent-fragments");
    private Option<long?> _limitRate = new("-r", "--limit-rate");
    private Option<long?> _throttledRate = new("--throttled-rate");
    private Option<int?> _retries = new("-R", "--retries");
    private Option<int?> _fileAccessRetries = new("--file-access-retries");
    private Option<int?> _fragmentRetries = new("--fragment-retries");
    private MultiOption<string> _retrySleep = new("--retry-sleep");
    private Option<bool> _skipUnavailableFragments = new("--skip-unavailable-fragments", "--no-abort-on-unavailable-fragments");
    private Option<bool> _abortOnUnavailableFragments = new("--abort-on-unavailable-fragments", "--no-skip-unavailable-fragments");
    private Option<bool> _keepFragments = new("--keep-fragments");
    private Option<bool> _noKeepFragments = new("--no-keep-fragments");
    private Option<long?> _bufferSize = new("--buffer-size");
    private Option<bool> _resizeBuffer = new("--resize-buffer");
    private Option<bool> _noResizeBuffer = new("--no-resize-buffer");
    private Option<long?> _httpChunkSize = new("--http-chunk-size");
    private Option<bool> _playlistRandom = new("--playlist-random");
    private Option<bool> _lazyPlaylist = new("--lazy-playlist");
    private Option<bool> _noLazyPlaylist = new("--no-lazy-playlist");
    private Option<bool> _xattrSetFilesize = new("--xattr-set-filesize");
    private Option<bool> _hlsUseMpegts = new("--hls-use-mpegts");
    private Option<bool> _noHlsUseMpegts = new("--no-hls-use-mpegts");
    private MultiOption<string> _downloadSections = new("--download-sections");
    private MultiOption<string> _downloader = new("--downloader", "--external-downloader");
    private MultiOption<string> _downloaderArgs = new("--downloader-args", "--external-downloader-args");

    /// <summary>
    /// Number of fragments of a dash/hlsnative
    /// video that should be downloaded concurrently
    /// (default is 1)
    /// </summary>
    public int? ConcurrentFragments
    {
        get => _concurrentFragments.Value;
        set => _concurrentFragments.Value = value;
    }

    /// <summary>
    /// Maximum download rate in bytes per second,
    /// e.g. 50K or 4.2M
    /// </summary>
    public long? LimitRate
    {
        get => _limitRate.Value;
        set => _limitRate.Value = value;
    }

    /// <summary>
    /// Minimum download rate in bytes per second
    /// below which throttling is assumed and the
    /// video data is re-extracted, e.g. 100K
    /// </summary>
    public long? ThrottledRate
    {
        get => _throttledRate.Value;
        set => _throttledRate.Value = value;
    }

    /// <summary>
    /// Number of retries (default is 10), or
    /// &quot;infinite&quot;
    /// </summary>
    public int? Retries
    {
        get => _retries.Value;
        set => _retries.Value = value;
    }

    /// <summary>
    /// Number of times to retry on file access
    /// error (default is 3), or &quot;infinite&quot;
    /// </summary>
    public int? FileAccessRetries
    {
        get => _fileAccessRetries.Value;
        set => _fileAccessRetries.Value = value;
    }

    /// <summary>
    /// Number of retries for a fragment (default is
    /// 10), or &quot;infinite&quot; (DASH, hlsnative and ISM)
    /// </summary>
    public int? FragmentRetries
    {
        get => _fragmentRetries.Value;
        set => _fragmentRetries.Value = value;
    }

    /// <summary>
    /// Time to sleep between retries in seconds
    /// (optionally) prefixed by the type of retry
    /// (http (default), fragment, file_access,
    /// extractor) to apply the sleep to. EXPR can
    /// be a number, linear=START[:END[:STEP=1]] or
    /// exp=START[:END[:BASE=2]]. This option can be
    /// used multiple times to set the sleep for the
    /// different retry types, e.g. --retry-sleep
    /// linear=1::2 --retry-sleep fragment:exp=1:20
    /// </summary>
    public MultiValue<string>? RetrySleep
    {
        get => _retrySleep.Value;
        set => _retrySleep.Value = value;
    }

    /// <summary>
    /// Skip unavailable fragments for DASH,
    /// hlsnative and ISM downloads (default)
    /// (Alias: --no-abort-on-unavailable-fragments)
    /// </summary>
    public bool SkipUnavailableFragments
    {
        get => _skipUnavailableFragments.Value;
        set => _skipUnavailableFragments.Value = value;
    }

    /// <summary>
    /// Abort download if a fragment is unavailable
    /// (Alias: --no-skip-unavailable-fragments)
    /// </summary>
    public bool AbortOnUnavailableFragments
    {
        get => _abortOnUnavailableFragments.Value;
        set => _abortOnUnavailableFragments.Value = value;
    }

    /// <summary>
    /// Keep downloaded fragments on disk after
    /// downloading is finished
    /// </summary>
    public bool KeepFragments
    {
        get => _keepFragments.Value;
        set => _keepFragments.Value = value;
    }

    /// <summary>
    /// Delete downloaded fragments after
    /// downloading is finished (default)
    /// </summary>
    public bool NoKeepFragments
    {
        get => _noKeepFragments.Value;
        set => _noKeepFragments.Value = value;
    }

    /// <summary>
    /// Size of download buffer, e.g. 1024 or 16K
    /// (default is 1024)
    /// </summary>
    public long? BufferSize
    {
        get => _bufferSize.Value;
        set => _bufferSize.Value = value;
    }

    /// <summary>
    /// The buffer size is automatically resized
    /// from an initial value of --buffer-size
    /// (default)
    /// </summary>
    public bool ResizeBuffer
    {
        get => _resizeBuffer.Value;
        set => _resizeBuffer.Value = value;
    }

    /// <summary>
    /// Do not automatically adjust the buffer size
    /// </summary>
    public bool NoResizeBuffer
    {
        get => _noResizeBuffer.Value;
        set => _noResizeBuffer.Value = value;
    }

    /// <summary>
    /// Size of a chunk for chunk-based HTTP
    /// downloading, e.g. 10485760 or 10M (default
    /// is disabled). May be useful for bypassing
    /// bandwidth throttling imposed by a webserver
    /// (experimental)
    /// </summary>
    public long? HttpChunkSize
    {
        get => _httpChunkSize.Value;
        set => _httpChunkSize.Value = value;
    }

    /// <summary>
    /// Download playlist videos in random order
    /// </summary>
    public bool PlaylistRandom
    {
        get => _playlistRandom.Value;
        set => _playlistRandom.Value = value;
    }

    /// <summary>
    /// Process entries in the playlist as they are
    /// received. This disables n_entries,
    /// --playlist-random and --playlist-reverse
    /// </summary>
    public bool LazyPlaylist
    {
        get => _lazyPlaylist.Value;
        set => _lazyPlaylist.Value = value;
    }

    /// <summary>
    /// Process videos in the playlist only after
    /// the entire playlist is parsed (default)
    /// </summary>
    public bool NoLazyPlaylist
    {
        get => _noLazyPlaylist.Value;
        set => _noLazyPlaylist.Value = value;
    }

    /// <summary>
    /// Set file xattribute ytdl.filesize with
    /// expected file size
    /// </summary>
    public bool XattrSetFilesize
    {
        get => _xattrSetFilesize.Value;
        set => _xattrSetFilesize.Value = value;
    }

    /// <summary>
    /// Use the mpegts container for HLS videos;
    /// allowing some players to play the video
    /// while downloading, and reducing the chance
    /// of file corruption if download is
    /// interrupted. This is enabled by default for
    /// live streams
    /// </summary>
    public bool HlsUseMpegts
    {
        get => _hlsUseMpegts.Value;
        set => _hlsUseMpegts.Value = value;
    }

    /// <summary>
    /// Do not use the mpegts container for HLS
    /// videos. This is default when not downloading
    /// live streams
    /// </summary>
    public bool NoHlsUseMpegts
    {
        get => _noHlsUseMpegts.Value;
        set => _noHlsUseMpegts.Value = value;
    }

    /// <summary>
    /// Download only chapters that match the
    /// regular expression. A &quot;*&quot; prefix denotes
    /// time-range instead of chapter. Negative
    /// timestamps are calculated from the end.
    /// &quot;*from-url&quot; can be used to download between
    /// the &quot;start_time&quot; and &quot;end_time&quot; extracted
    /// from the URL. Needs ffmpeg. This option can
    /// be used multiple times to download multiple
    /// sections, e.g. --download-sections
    /// &quot;*10:15-inf&quot; --download-sections &quot;intro&quot;
    /// </summary>
    public MultiValue<string>? DownloadSections
    {
        get => _downloadSections.Value;
        set => _downloadSections.Value = value;
    }

    /// <summary>
    /// Name or path of the external downloader to
    /// use (optionally) prefixed by the protocols
    /// (http, ftp, m3u8, dash, rstp, rtmp, mms) to
    /// use it for. Currently supports native,
    /// aria2c, avconv, axel, curl, ffmpeg, httpie,
    /// wget. You can use this option multiple times
    /// to set different downloaders for different
    /// protocols. E.g. --downloader aria2c
    /// --downloader &quot;dash,m3u8:native&quot; will use
    /// aria2c for http/ftp downloads, and the
    /// native downloader for dash/m3u8 downloads
    /// (Alias: --external-downloader)
    /// </summary>
    public MultiValue<string>? Downloader
    {
        get => _downloader.Value;
        set => _downloader.Value = value;
    }

    /// <summary>
    /// Give these arguments to the external
    /// downloader. Specify the downloader name and
    /// the arguments separated by a colon &quot;:&quot;. For
    /// ffmpeg, arguments can be passed to different
    /// positions using the same syntax as
    /// --postprocessor-args. You can use this
    /// option multiple times to give different
    /// arguments to different downloaders (Alias:
    /// --external-downloader-args)
    /// </summary>
    public MultiValue<string>? DownloaderArgs
    {
        get => _downloaderArgs.Value;
        set => _downloaderArgs.Value = value;
    }
}
