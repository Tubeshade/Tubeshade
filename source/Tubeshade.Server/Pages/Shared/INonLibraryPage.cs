using System.Collections.Generic;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Pages.Shared;

/// <summary>A page that is not associated with a specific library.</summary>
public interface INonLibraryPage
{
    /// <summary>Gets a collection of all available libraries.</summary>
    IEnumerable<LibraryEntity> Libraries { get; }
}
