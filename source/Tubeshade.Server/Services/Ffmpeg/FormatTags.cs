using System.Text.Json.Serialization;

namespace Tubeshade.Server.Services.Ffmpeg;

public sealed class FormatTags
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("major_brand")]
    public string? MajorBrand { get; set; }

    [JsonPropertyName("minor_version")]
    public string? MinorVersion { get; set; }

    [JsonPropertyName("compatible_brands")]
    public string? CompatibleBrands { get; set; }

    [JsonPropertyName("encoder")]
    public string? Encoder { get; set; }
}
