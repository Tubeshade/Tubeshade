using System.Text.Json;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using NodaTime.Text;

namespace Ytdlp;

public sealed class DurationConverter : NodaConverterBase<Duration?>
{
    private static readonly IPattern<Duration> Pattern = new CompositePatternBuilder<Duration>
    {
        { DurationPattern.CreateWithInvariantCulture("h:mm:ss"), duration => duration.Days is 0 },
        { DurationPattern.CreateWithInvariantCulture("M:ss"), duration => duration.Hours is 0 }
    }.Build();

    /// <inheritdoc />
    protected override Duration? ReadJsonImpl(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (text is null)
        {
            return null;
        }

        return Pattern.Parse(text).Value;
    }

    /// <inheritdoc />
    protected override void WriteJsonImpl(Utf8JsonWriter writer, Duration? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteStringValue((string?)null);
        }
        else
        {
            var text = Pattern.Format(value.Value);
            writer.WriteStringValue(text);
        }
    }
}
