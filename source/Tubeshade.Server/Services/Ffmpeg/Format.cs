using System.Text.Json.Serialization;

namespace Tubeshade.Server.Services.Ffmpeg;

public sealed class Format
{
    [JsonPropertyName("filename")]
    public required string Filename { get; set; }

    [JsonPropertyName("nb_streams")]
    public required int NbStreams { get; set; }

    [JsonPropertyName("nb_programs")]
    public required int NbPrograms { get; set; }

    [JsonPropertyName("format_name")]
    public required string FormatName { get; set; }

    [JsonPropertyName("format_long_name")]
    public required string FormatLongName { get; set; }

    [JsonPropertyName("start_time")]
    public required decimal StartTime { get; set; }

    [JsonPropertyName("duration")]
    public required decimal Duration { get; set; }

    [JsonPropertyName("size")]
    public required long Size { get; set; }

    [JsonPropertyName("bit_rate")]
    public required long BitRate { get; set; }

    [JsonPropertyName("probe_score")]
    public required int ProbeScore { get; set; }

    [JsonPropertyName("tags")]
    public required FormatTags Tags { get; set; }
}
