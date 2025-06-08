using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Preferences;

public sealed record PreferencesEntity : ModifiableEntity
{
    public decimal? PlaybackSpeed { get; set; }
}
