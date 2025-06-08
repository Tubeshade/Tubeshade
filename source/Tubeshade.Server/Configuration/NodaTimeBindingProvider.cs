using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NodaTime;
using NodaTime.Text;


namespace Tubeshade.Server.Configuration;

internal sealed class NodaTimeBindingProvider : IModelBinderProvider
{
    private static readonly LocalDateTimeModelBinder LocalDateTimeBinder = new();

    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var modelType = context.Metadata.ModelType;
        if (modelType == typeof(LocalDateTime) || modelType == typeof(LocalDateTime?))
        {
            // return new BinderTypeModelBinder(typeof(LocalDateTimeModelBinder));
            return LocalDateTimeBinder;
        }

        return null;
    }

    private sealed class LocalDateTimeModelBinder : IModelBinder
    {
        private static readonly IPattern<LocalDateTime> Pattern = LocalDateTimePattern.VariablePrecisionIso;

        /// <inheritdoc />
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var modelValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, modelValue);

            var firstValue = modelValue.FirstValue;
            if (firstValue is null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

            var parseResult = Pattern.Parse(firstValue);
            bindingContext.Result = parseResult.TryGetValue(LocalDateTime.MinIsoValue, out var localDateTime)
                ? ModelBindingResult.Success(localDateTime)
                : ModelBindingResult.Failed();

            return Task.CompletedTask;
        }
    }
}
