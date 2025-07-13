using System.Globalization;
using Asp.Versioning;
using Asp.Versioning.Conventions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Tubeshade.Data;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Identity.Stores;
using Tubeshade.Server.Areas.Identity;
using Tubeshade.Server.Configuration;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Configuration.Startup;
using Tubeshade.Server.Services;

namespace Tubeshade.Server;

internal static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddTransient<IConfigureOptions<YtdlpOptions>, ExecutableDetector>()
            .AddOptions<YtdlpOptions>()
            .BindConfiguration(YtdlpOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddRazorPages();

        builder.Services.AddAuthenticationAndAuthorization(builder.Configuration);

        builder.Services
            .AddIdentityCore<UserEntity>(options =>
            {
                options.Stores.MaxLengthForKeys = 128;
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services
            .AddTransient<IEmailSender, NoOpEmailSender>()
            .AddScoped<UserOnlyStore>()
            .AddScoped<IUserStore<UserEntity>, UserOnlyStore>()
            .AddScoped<IUserEmailStore<UserEntity>, UserOnlyStore>()
            .AddHttpContextAccessor()
            .AddScoped<ISecurityStampValidator, SecurityStampValidator<UserEntity>>()
            .AddScoped<ITwoFactorSecurityStampValidator, TwoFactorSecurityStampValidator<UserEntity>>()
            .AddScoped<SignInManager<UserEntity>, SignInManager>()
            .AddScoped<SignInManager>();

        builder.Services
            .AddDatabase()
            .AddSingleton<IClock>(SystemClock.Instance)
            .AddSingleton(DateTimeZoneProviders.Tzdb);

        builder.Services
            .AddMvc(options => options.ModelBinderProviders.Insert(0, new NodaTimeBindingProvider()))
            .AddViewOptions(options =>
            {
                // This removed an extra input for each form parameter to specify the culture of that input
                // https://github.com/dotnet/aspnetcore/issues/47593
                options.HtmlHelperOptions.FormInputRenderMode = FormInputRenderMode.AlwaysUseCurrentCulture;
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
                options.JsonSerializerOptions.Converters.Add(NodaConverters.InstantConverter);

                options.JsonSerializerOptions.TypeInfoResolverChain.Add(V1.Models.SerializerContext.Default);
            });

        builder.Services
            .AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc(options => options.Conventions.Add(new VersionByNamespaceConvention()))
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        builder.Services.AddRequestLocalization(options =>
        {
            var cultures = new CultureInfo[] { new("en-US"), new("en"), new("lv"), };

            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
            options.SetDefaultCulture(cultures[0].Name);

            options.ApplyCurrentCultureToResponseHeaders = true;
        });

        builder.Services
            .AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(nameof(DatabaseHealthCheck));

        builder.Services
            .AddScoped<YoutubeService>()
            .AddHostedService<TaskBackgroundService>()
            .AddHostedService<IndexBackgroundService>()
            .AddHostedService<DownloadBackgroundService>();

        builder.Services.AddTransient<IStartupFilter, DatabaseMigrationStartupFilter>();

        builder.Services.AddCors(options => options.AddDefaultPolicy(cors =>
            cors.AllowAnyMethod()
                .AllowCredentials()
                .AllowAnyHeader()
                .SetIsOriginAllowed(_ => true)));

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();
        app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseRequestLocalization();

        app.MapRazorPages().RequireAuthorization(Policies.Identity);
        app.MapControllers().RequireAuthorization(Policies.User);

        app.MapHealthChecks("/healthz");

        app.Run();
    }
}
