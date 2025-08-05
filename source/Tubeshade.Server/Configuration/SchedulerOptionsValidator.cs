using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Tubeshade.Server.Configuration;

internal sealed class SchedulerOptionsValidator : IValidateOptions<SchedulerOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SchedulerOptions options)
    {
        var failures = new List<string>();

        if (options.WorkerCount <= 0)
        {
            failures.Add($"{nameof(SchedulerOptions.WorkerCount)} must be at least 1");
        }

        var result = SchedulerOptions.Pattern.Parse(options.Period);
        if (!result.Success)
        {
            failures.Add($"Could not parse {nameof(SchedulerOptions.Period)}: {result.Exception.Message}");
        }

        return failures is []
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
