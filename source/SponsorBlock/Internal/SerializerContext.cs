using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;

namespace SponsorBlock.Internal;

[JsonSourceGenerationOptions(
    Converters =
    [
        typeof(SmartEnumNameConverter<SegmentCategory, int>),
        typeof(SmartEnumNameConverter<SegmentAction, int>),
    ])]
[JsonSerializable(typeof(VideoModel[]))]
internal sealed partial class SerializerContext : JsonSerializerContext;
