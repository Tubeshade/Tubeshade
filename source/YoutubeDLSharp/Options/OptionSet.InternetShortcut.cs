namespace YoutubeDLSharp.Options;

public partial class OptionSet
{
    private Option<bool> _writeLink = new("--write-link");
    private Option<bool> _writeUrlLink = new("--write-url-link");
    private Option<bool> _writeWeblocLink = new("--write-webloc-link");
    private Option<bool> _writeDesktopLink = new("--write-desktop-link");

    /// <summary>
    /// Write an internet shortcut file, depending
    /// on the current platform (.url, .webloc or
    /// .desktop). The URL may be cached by the OS
    /// </summary>
    public bool WriteLink
    {
        get => _writeLink.Value;
        set => _writeLink.Value = value;
    }

    /// <summary>
    /// Write a .url Windows internet shortcut. The
    /// OS caches the URL based on the file path
    /// </summary>
    public bool WriteUrlLink
    {
        get => _writeUrlLink.Value;
        set => _writeUrlLink.Value = value;
    }

    /// <summary>
    /// Write a .webloc macOS internet shortcut
    /// </summary>
    public bool WriteWeblocLink
    {
        get => _writeWeblocLink.Value;
        set => _writeWeblocLink.Value = value;
    }

    /// <summary>
    /// Write a .desktop Linux internet shortcut
    /// </summary>
    public bool WriteDesktopLink
    {
        get => _writeDesktopLink.Value;
        set => _writeDesktopLink.Value = value;
    }
}
