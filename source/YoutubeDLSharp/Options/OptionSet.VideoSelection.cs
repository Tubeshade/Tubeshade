using System;

namespace YoutubeDLSharp.Options;

public partial class OptionSet
{
    private Option<string> _playlistItems = new("-I", "--playlist-items");
    private Option<string> _minFilesize = new("--min-filesize");
    private Option<string> _maxFilesize = new("--max-filesize");
    private Option<DateTime> _date = new("--date");
    private Option<DateTime> _dateBefore = new("--datebefore");
    private Option<DateTime> _dateAfter = new("--dateafter");
    private MultiOption<string> _matchFilters = new("--match-filters");
    private Option<bool> _noMatchFilters = new("--no-match-filters");
    private Option<string> _breakMatchFilters = new("--break-match-filters");
    private Option<bool> _noBreakMatchFilters = new("--no-break-match-filters");
    private Option<bool> _noPlaylist = new("--no-playlist");
    private Option<bool> _yesPlaylist = new("--yes-playlist");
    private Option<byte?> _ageLimit = new("--age-limit");
    private Option<string> _downloadArchive = new("--download-archive");
    private Option<bool> _noDownloadArchive = new("--no-download-archive");
    private Option<int?> _maxDownloads = new("--max-downloads");
    private Option<bool> _breakOnExisting = new("--break-on-existing");
    private Option<bool> _noBreakOnExisting = new("--no-break-on-existing");
    private Option<bool> _breakPerInput = new("--break-per-input");
    private Option<bool> _noBreakPerInput = new("--no-break-per-input");
    private Option<int?> _skipPlaylistAfterErrors = new("--skip-playlist-after-errors");

    /// <summary>
    /// Comma separated playlist_index of the items
    /// to download. You can specify a range using
    /// &quot;[START]:[STOP][:STEP]&quot;. For backward
    /// compatibility, START-STOP is also supported.
    /// Use negative indices to count from the right
    /// and negative STEP to download in reverse
    /// order. E.g. &quot;-I 1:3,7,-5::2&quot; used on a
    /// playlist of size 15 will download the items
    /// at index 1,2,3,7,11,13,15
    /// </summary>
    public string? PlaylistItems
    {
        get => _playlistItems.Value;
        set => _playlistItems.Value = value;
    }

    /// <summary>
    /// Abort download if filesize is smaller than
    /// SIZE, e.g. 50k or 44.6M
    /// </summary>
    public string? MinFilesize
    {
        get => _minFilesize.Value;
        set => _minFilesize.Value = value;
    }

    /// <summary>
    /// Abort download if filesize is larger than
    /// SIZE, e.g. 50k or 44.6M
    /// </summary>
    public string? MaxFilesize
    {
        get => _maxFilesize.Value;
        set => _maxFilesize.Value = value;
    }

    /// <summary>
    /// Download only videos uploaded on this date.
    /// The date can be &quot;YYYYMMDD&quot; or in the format
    /// [now|today|yesterday][-
    /// N[day|week|month|year]]. E.g. &quot;--date
    /// today-2weeks&quot; downloads only videos uploaded
    /// on the same day two weeks ago
    /// </summary>
    public DateTime Date
    {
        get => _date.Value;
        set => _date.Value = value;
    }

    /// <summary>
    /// Download only videos uploaded on or before
    /// this date. The date formats accepted are the
    /// same as --date
    /// </summary>
    public DateTime DateBefore
    {
        get => _dateBefore.Value;
        set => _dateBefore.Value = value;
    }

    /// <summary>
    /// Download only videos uploaded on or after
    /// this date. The date formats accepted are the
    /// same as --date
    /// </summary>
    public DateTime DateAfter
    {
        get => _dateAfter.Value;
        set => _dateAfter.Value = value;
    }

    /// <summary>
    /// Generic video filter. Any &quot;OUTPUT TEMPLATE&quot;
    /// field can be compared with a number or a
    /// string using the operators defined in
    /// &quot;Filtering Formats&quot;. You can also simply
    /// specify a field to match if the field is
    /// present, use &quot;!field&quot; to check if the field
    /// is not present, and &quot;&amp;&quot; to check multiple
    /// conditions. Use a &quot;\&quot; to escape &quot;&amp;&quot; or
    /// quotes if needed. If used multiple times,
    /// the filter matches if at least one of the
    /// conditions is met. E.g. --match-filters
    /// !is_live --match-filters &quot;like_count&gt;?100 &amp;
    /// description~=&#x27;(?i)\bcats \&amp; dogs\b&#x27;&quot; matches
    /// only videos that are not live OR those that
    /// have a like count more than 100 (or the like
    /// field is not available) and also has a
    /// description that contains the phrase &quot;cats &amp;
    /// dogs&quot; (caseless). Use &quot;--match-filters -&quot; to
    /// interactively ask whether to download each
    /// video
    /// </summary>
    public MultiValue<string>? MatchFilters
    {
        get => _matchFilters.Value;
        set => _matchFilters.Value = value;
    }

    /// <summary>
    /// Do not use any --match-filters (default)
    /// </summary>
    public bool NoMatchFilters
    {
        get => _noMatchFilters.Value;
        set => _noMatchFilters.Value = value;
    }

    /// <summary>
    /// Same as &quot;--match-filters&quot; but stops the
    /// download process when a video is rejected
    /// </summary>
    public string? BreakMatchFilters
    {
        get => _breakMatchFilters.Value;
        set => _breakMatchFilters.Value = value;
    }

    /// <summary>
    /// Do not use any --break-match-filters
    /// (default)
    /// </summary>
    public bool NoBreakMatchFilters
    {
        get => _noBreakMatchFilters.Value;
        set => _noBreakMatchFilters.Value = value;
    }

    /// <summary>
    /// Download only the video, if the URL refers
    /// to a video and a playlist
    /// </summary>
    public bool NoPlaylist
    {
        get => _noPlaylist.Value;
        set => _noPlaylist.Value = value;
    }

    /// <summary>
    /// Download the playlist, if the URL refers to
    /// a video and a playlist
    /// </summary>
    public bool YesPlaylist
    {
        get => _yesPlaylist.Value;
        set => _yesPlaylist.Value = value;
    }

    /// <summary>
    /// Download only videos suitable for the given
    /// age
    /// </summary>
    public byte? AgeLimit
    {
        get => _ageLimit.Value;
        set => _ageLimit.Value = value;
    }

    /// <summary>
    /// Download only videos not listed in the
    /// archive file. Record the IDs of all
    /// downloaded videos in it
    /// </summary>
    public string? DownloadArchive
    {
        get => _downloadArchive.Value;
        set => _downloadArchive.Value = value;
    }

    /// <summary>
    /// Do not use archive file (default)
    /// </summary>
    public bool NoDownloadArchive
    {
        get => _noDownloadArchive.Value;
        set => _noDownloadArchive.Value = value;
    }

    /// <summary>
    /// Abort after downloading NUMBER files
    /// </summary>
    public int? MaxDownloads
    {
        get => _maxDownloads.Value;
        set => _maxDownloads.Value = value;
    }

    /// <summary>
    /// Stop the download process when encountering
    /// a file that is in the archive supplied with
    /// the --download-archive option
    /// </summary>
    public bool BreakOnExisting
    {
        get => _breakOnExisting.Value;
        set => _breakOnExisting.Value = value;
    }

    /// <summary>
    /// Do not stop the download process when
    /// encountering a file that is in the archive
    /// (default)
    /// </summary>
    public bool NoBreakOnExisting
    {
        get => _noBreakOnExisting.Value;
        set => _noBreakOnExisting.Value = value;
    }

    /// <summary>
    /// Alters --max-downloads, --break-on-existing,
    /// --break-match-filters, and autonumber to
    /// reset per input URL
    /// </summary>
    public bool BreakPerInput
    {
        get => _breakPerInput.Value;
        set => _breakPerInput.Value = value;
    }

    /// <summary>
    /// --break-on-existing and similar options
    /// terminates the entire download queue
    /// </summary>
    public bool NoBreakPerInput
    {
        get => _noBreakPerInput.Value;
        set => _noBreakPerInput.Value = value;
    }

    /// <summary>
    /// Number of allowed failures until the rest of
    /// the playlist is skipped
    /// </summary>
    public int? SkipPlaylistAfterErrors
    {
        get => _skipPlaylistAfterErrors.Value;
        set => _skipPlaylistAfterErrors.Value = value;
    }
}
