using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media;

public sealed record ImageFileEntity : ModifiableEntity
{
    public required string StoragePath { get; set; }

    public required ImageType Type { get; set; }

    public required int Width { get; set; }

    public required int Height { get; set; }
}
