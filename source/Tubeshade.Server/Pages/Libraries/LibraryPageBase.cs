using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Tubeshade.Server.Pages.Libraries;

public abstract class LibraryPageBase : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid LibraryId { get; set; }
}
