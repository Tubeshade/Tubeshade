using Microsoft.Extensions.Options;

namespace Tubeshade.Server.Configuration;

internal sealed class SchedulerOptionsValidator : IValidateOptions<SchedulerOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SchedulerOptions options)
    {
        var result = SchedulerOptions.Pattern.Parse(options.Period);
        return result.Success
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail($"Could not parse {nameof(SchedulerOptions.Period)}: {result.Exception.Message}");
    }
}
