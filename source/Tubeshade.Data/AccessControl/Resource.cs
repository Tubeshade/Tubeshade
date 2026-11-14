namespace Tubeshade.Data.AccessControl;

/// <summary>A kind of data that token scopes can grant access to.</summary>
public sealed record Resource(string Name);

public static class Resources
{
    public static readonly Resource Library = new("library");
    public static readonly Resource LibraryCookie = new("library_cookie");
    public static readonly Resource Channel = new("channel");
    public static readonly Resource Creator = new("creator");
    public static readonly Resource Playlist = new("playlist");
    public static readonly Resource Video = new("video");
    public static readonly Resource File = new("file");
    public static readonly Resource Preferences = new("preferences");
    public static readonly Resource Task = new("task");
}
