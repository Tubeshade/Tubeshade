#if DEBUG
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Tubeshade.Server.V1.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class StopController : ControllerBase
{
    private readonly IHostApplicationLifetime _lifetime;

    public StopController(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _lifetime.StopApplication();
        return NoContent();
    }
}
#endif
