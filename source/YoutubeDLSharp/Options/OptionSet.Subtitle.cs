namespace YoutubeDLSharp.Options;

public partial class OptionSet
{
    private Option<bool> _writeSubs = new("--write-subs");
    private Option<bool> _noWriteSubs = new("--no-write-subs");
    private Option<bool> _writeAutoSubs = new("--write-auto-subs", "--write-automatic-subs");
    private Option<bool> _noWriteAutoSubs = new("--no-write-auto-subs", "--no-write-automatic-subs");
    private Option<bool> _listSubs = new("--list-subs");
    private Option<string> _subFormat = new("--sub-format");
    private Option<string> _subLangs = new("--sub-langs");

    /// <summary>
    /// Write subtitle file
    /// </summary>
    public bool WriteSubs
    {
        get => _writeSubs.Value;
        set => _writeSubs.Value = value;
    }

    /// <summary>
    /// Do not write subtitle file (default)
    /// </summary>
    public bool NoWriteSubs
    {
        get => _noWriteSubs.Value;
        set => _noWriteSubs.Value = value;
    }

    /// <summary>
    /// Write automatically generated subtitle file
    /// (Alias: --write-automatic-subs)
    /// </summary>
    public bool WriteAutoSubs
    {
        get => _writeAutoSubs.Value;
        set => _writeAutoSubs.Value = value;
    }

    /// <summary>
    /// Do not write auto-generated subtitles
    /// (default) (Alias: --no-write-automatic-subs)
    /// </summary>
    public bool NoWriteAutoSubs
    {
        get => _noWriteAutoSubs.Value;
        set => _noWriteAutoSubs.Value = value;
    }

    /// <summary>
    /// List available subtitles of each video.
    /// Simulate unless --no-simulate is used
    /// </summary>
    public bool ListSubs
    {
        get => _listSubs.Value;
        set => _listSubs.Value = value;
    }

    /// <summary>
    /// Subtitle format; accepts formats preference
    /// separated by &quot;/&quot;, e.g. &quot;srt&quot; or
    /// &quot;ass/srt/best&quot;
    /// </summary>
    public string? SubFormat
    {
        get => _subFormat.Value;
        set => _subFormat.Value = value;
    }

    /// <summary>
    /// Languages of the subtitles to download (can
    /// be regex) or &quot;all&quot; separated by commas, e.g.
    /// --sub-langs &quot;en.*,ja&quot; (where &quot;en.*&quot; is a
    /// regex pattern that matches &quot;en&quot; followed by
    /// 0 or more of any character). You can prefix
    /// the language code with a &quot;-&quot; to exclude it
    /// from the requested languages, e.g. --sub-
    /// langs all,-live_chat. Use --list-subs for a
    /// list of available language tags
    /// </summary>
    public string? SubLangs
    {
        get => _subLangs.Value;
        set => _subLangs.Value = value;
    }
}
