namespace YoutubeDLSharp.Options;

public partial class OptionSet
{
    private Option<bool> _writeThumbnail = new("--write-thumbnail");
    private Option<bool> _noWriteThumbnail = new("--no-write-thumbnail");
    private Option<bool> _writeAllThumbnails = new("--write-all-thumbnails");
    private Option<bool> _listThumbnails = new("--list-thumbnails");

    /// <summary>
    /// Write thumbnail image to disk
    /// </summary>
    public bool WriteThumbnail
    {
        get => _writeThumbnail.Value;
        set => _writeThumbnail.Value = value;
    }

    /// <summary>
    /// Do not write thumbnail image to disk
    /// (default)
    /// </summary>
    public bool NoWriteThumbnail
    {
        get => _noWriteThumbnail.Value;
        set => _noWriteThumbnail.Value = value;
    }

    /// <summary>
    /// Write all thumbnail image formats to disk
    /// </summary>
    public bool WriteAllThumbnails
    {
        get => _writeAllThumbnails.Value;
        set => _writeAllThumbnails.Value = value;
    }

    /// <summary>
    /// List available thumbnails of each video.
    /// Simulate unless --no-simulate is used
    /// </summary>
    public bool ListThumbnails
    {
        get => _listThumbnails.Value;
        set => _listThumbnails.Value = value;
    }
}
