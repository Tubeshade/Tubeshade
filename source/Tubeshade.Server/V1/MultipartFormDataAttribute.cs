using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Tubeshade.Server.V1;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class MultipartFormDataAttribute : ActionFilterAttribute
{
    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.Request.IsMultipart())
        {
            context.Result = new StatusCodeResult(StatusCodes.Status415UnsupportedMediaType);
        }
    }
}
