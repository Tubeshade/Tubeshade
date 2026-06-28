namespace YoutubeDLSharp.Options;

public partial class OptionSet
{
    private Option<string> _sponsorblockMark = new("--sponsorblock-mark");
    private Option<string> _sponsorblockRemove = new("--sponsorblock-remove");
    private Option<string> _sponsorblockChapterTitle = new("--sponsorblock-chapter-title");
    private Option<bool> _noSponsorblock = new("--no-sponsorblock");
    private Option<string> _sponsorblockApi = new("--sponsorblock-api");

    /// <summary>
    /// SponsorBlock categories to create chapters
    /// for, separated by commas. Available
    /// categories are sponsor, intro, outro,
    /// selfpromo, preview, filler, interaction,
    /// music_offtopic, poi_highlight, chapter, all
    /// and default (=all). You can prefix the
    /// category with a &quot;-&quot; to exclude it. See [1]
    /// for descriptions of the categories. E.g.
    /// --sponsorblock-mark all,-preview [1] https:/
    /// /wiki.sponsor.ajay.app/w/Segment_Categories
    /// </summary>
    public string? SponsorblockMark
    {
        get => _sponsorblockMark.Value;
        set => _sponsorblockMark.Value = value;
    }

    /// <summary>
    /// SponsorBlock categories to be removed from
    /// the video file, separated by commas. If a
    /// category is present in both mark and remove,
    /// remove takes precedence. The syntax and
    /// available categories are the same as for
    /// --sponsorblock-mark except that &quot;default&quot;
    /// refers to &quot;all,-filler&quot; and poi_highlight,
    /// chapter are not available
    /// </summary>
    public string? SponsorblockRemove
    {
        get => _sponsorblockRemove.Value;
        set => _sponsorblockRemove.Value = value;
    }

    /// <summary>
    /// An output template for the title of the
    /// SponsorBlock chapters created by
    /// --sponsorblock-mark. The only available
    /// fields are start_time, end_time, category,
    /// categories, name, category_names. Defaults
    /// to &quot;[SponsorBlock]: %(category_names)l&quot;
    /// </summary>
    public string? SponsorblockChapterTitle
    {
        get => _sponsorblockChapterTitle.Value;
        set => _sponsorblockChapterTitle.Value = value;
    }

    /// <summary>
    /// Disable both --sponsorblock-mark and
    /// --sponsorblock-remove
    /// </summary>
    public bool NoSponsorblock
    {
        get => _noSponsorblock.Value;
        set => _noSponsorblock.Value = value;
    }

    /// <summary>
    /// SponsorBlock API location, defaults to
    /// https://sponsor.ajay.app
    /// </summary>
    public string? SponsorblockApi
    {
        get => _sponsorblockApi.Value;
        set => _sponsorblockApi.Value = value;
    }
}
