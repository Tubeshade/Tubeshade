using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace Tubeshade.Server.Pages;

public sealed class HtmxPageCachingFilter : IPageFilter
{
    /// <inheritdoc />
    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }

    /// <inheritdoc />
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var vary = context.HttpContext.Response.Headers.Vary;
        context.HttpContext.Response.Headers.Vary = StringValues.Concat(vary, "HX-Request");
    }

    /// <inheritdoc />
    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }
}
