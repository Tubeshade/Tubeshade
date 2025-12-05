using System.Globalization;
using System.Linq;
using Asp.Versioning;
using Asp.Versioning.Conventions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using PubSubHubbub;
using SponsorBlock;
using Tubeshade.Data;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Identity.Stores;
using Tubeshade.Server.Areas.Identity;
using Tubeshade.Server.Configuration;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Configuration.Startup;
using Tubeshade.Server.Services;
using Tubeshade.Server.Services.Background;
using Tubeshade.Server.Services.Ffmpeg;

namespace Tubeshade.Server;

internal static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddSingleton<IPostConfigureOptions<YtdlpOptions>, ExecutableDetector>()
            .AddSingleton<IValidateOptions<YtdlpOptions>, ExecutableDetector>()
            .AddOptions<YtdlpOptions>()
            .BindConfiguration(YtdlpOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services
            .AddTransient<IValidateOptions<SchedulerOptions>, SchedulerOptionsValidator>()
            .AddOptions<SchedulerOptions>()
            .BindConfiguration(SchedulerOptions.SectionName)
            .ValidateOnStart();

        builder.Services.AddRazorPages();

        builder.Services.AddAuthenticationAndAuthorization(builder.Configuration);

        builder.Services
            .AddIdentityCore<UserEntity>(options =>
            {
                options.Password.RequiredLength = 16;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = false;

                options.Tokens.AuthenticatorIssuer = "Tubeshade";
            })
            .AddSignInManager<SignInManager>()
            .AddDefaultTokenProviders();

        builder.Services
            .AddHttpContextAccessor()
            .AddScoped<UserOnlyStore>()
            .AddScoped<IUserStore<UserEntity>, UserOnlyStore>()
            .AddScoped<ISecurityStampValidator, SecurityStampValidator<UserEntity>>()
            .AddScoped<ITwoFactorSecurityStampValidator, TwoFactorSecurityStampValidator<UserEntity>>();

        builder.Services
            .AddDatabase()
            .AddSingleton<IClock>(SystemClock.Instance)
            .AddSingleton(DateTimeZoneProviders.Tzdb);

        builder.Services
            .AddMvc(options =>
            {
                options.ModelBinderProviders.Insert(0, new NodaTimeBindingProvider());
                options.CacheProfiles.AddTubeshadeProfiles();
            })
            .AddXmlSerializerFormatters()
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

        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
        });

        builder.Services.AddRequestLocalization(options =>
        {
            var cultures = new CultureInfo[]
            {
                new("en-US"),
                new("en"),
                new("lv-lv"),
                new("lv"),
            };

            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
            options.SetDefaultCulture(cultures[0].Name);

            options.ApplyCurrentCultureToResponseHeaders = true;
        });

        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Append("text/vtt");
        });

        builder.Services
            .AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(nameof(DatabaseHealthCheck));

        builder.Services
            .AddSingleton<TaskListenerService>()
            .AddHostedService<TaskListenerService>(provider => provider.GetRequiredService<TaskListenerService>())
            .AddHostedService<TaskCancellationService>()
            .AddSingleton<BackgroundWorkerService>()
            .AddHostedService<BackgroundWorkerService>(provider => provider.GetRequiredService<BackgroundWorkerService>())
            .AddHostedService<SchedulerService>()
            .AddScoped<TaskService>()
            .AddScoped<SponsorBlockService>()
            .AddScoped<WebVideoTextTracksService>()
            .AddScoped<IYtdlpWrapper, YtdlpWrapper>()
            .AddScoped<YoutubeService>()
            .AddSponsorBlockClient()
            .AddScoped<SubscriptionsService>()
            .AddScoped<FileUploadService>()
            .AddSingleton<FfmpegService>()
            .AddSingleton<FileSystemService>()
            .AddPubSubHubbubClient();

        builder.Services
            .AddSingleton<DatabaseMigrationStartupFilter>()
            .AddSingleton<IStartupFilter, DatabaseMigrationStartupFilter>(provider => provider.GetRequiredService<DatabaseMigrationStartupFilter>());

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
        app.UseResponseCompression();

        app.MapRazorPages().RequireAuthorization(Policies.Identity);
        app.MapControllers().RequireAuthorization(Policies.User);

        app.MapHealthChecks("/healthz");

        app.Run();
    }
}
