using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Pages.Libraries;

public abstract class LibraryPageBase : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid LibraryId { get; set; }

    public LibraryEntity Library { get; protected set; } = null!;
}
