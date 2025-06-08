using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Tubeshade.Server.Pages;

public sealed class IndexModel : PageModel
{
    public IActionResult OnGet(CancellationToken cancellationToken)
    {
        return RedirectToPage("/Libraries/Index");
    }
}
