using System.Text.Json.Serialization;

namespace Tubeshade.Server.Services.Ffmpeg;

public sealed class Disposition
{
    [JsonPropertyName("default")]
    public required int Default { get; set; }

    [JsonPropertyName("dub")]
    public required int Dub { get; set; }

    [JsonPropertyName("original")]
    public required int Original { get; set; }

    [JsonPropertyName("comment")]
    public required int Comment { get; set; }

    [JsonPropertyName("lyrics")]
    public required int Lyrics { get; set; }

    [JsonPropertyName("karaoke")]
    public required int Karaoke { get; set; }

    [JsonPropertyName("forced")]
    public required int Forced { get; set; }

    [JsonPropertyName("hearing_impaired")]
    public required int HearingImpaired { get; set; }

    [JsonPropertyName("visual_impaired")]
    public required int VisualImpaired { get; set; }

    [JsonPropertyName("clean_effects")]
    public required int CleanEffects { get; set; }

    [JsonPropertyName("attached_pic")]
    public required int AttachedPic { get; set; }

    [JsonPropertyName("timed_thumbnails")]
    public required int TimedThumbnails { get; set; }
}
