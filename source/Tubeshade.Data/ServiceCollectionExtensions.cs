using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using SponsorBlock;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Configuration;
using Tubeshade.Data.Dapper;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Data.Migrations;
using Tubeshade.Data.Preferences;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<Access>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<TaskType>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<RunState>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<TaskSource>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<TaskResult>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<ExternalAvailability>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<ImageType>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<VideoType>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<VideoContainerType>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<SegmentCategory>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<SegmentAction>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<PlayerClient>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<DownloadMethod>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<DownloadVideos>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<SubscriptionStatus>());

        SqlMapper.AddTypeHandler(new LocalDateTypeHandler());
        SqlMapper.AddTypeHandler(new PeriodTypeHandler());
        SqlMapper.AddTypeHandler(new NullableInstantTypeHandler());
        SqlMapper.AddTypeHandler(new UnsignedIntegerTypeHandler());
        SqlMapper.AddTypeHandler(new NullableStructArrayTypeHandler<short>(NpgsqlDbType.Smallint));
        SqlMapper.AddTypeHandler(new NullableStructArrayTypeHandler<long>(NpgsqlDbType.Smallint));
        SqlMapper.AddTypeHandler(new NullableStructArrayTypeHandler<decimal>(NpgsqlDbType.Numeric));

        SqlMapper.RemoveTypeMap(typeof(uint));
        SqlMapper.RemoveTypeMap(typeof(uint?));

        services
            .AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .ValidateOnStart();

        return services
            .AddTransient<DatabaseMigrationService>()
            .AddScoped<OwnerRepository>()
            .AddScoped<OwnershipRepository>()
            .AddScoped<UserRepository>()
            .AddScoped<UserLoginRepository>()
            .AddScoped<IRepository<UserEntity>, UserRepository>()
            .AddScoped<INamedRepository<UserEntity>, UserRepository>()
            .AddScoped<ClaimRepository>()
            .AddScoped<IRepository<ClaimEntity>, ClaimRepository>()
            .AddScoped<TaskRepository>()
            .AddScoped<ScheduleRepository>()
            .AddScoped<LibraryRepository>()
            .AddScoped<ChannelRepository>()
            .AddScoped<VideoRepository>()
            .AddScoped<VideoFileRepository>()
            .AddScoped<ImageFileRepository>()
            .AddScoped<PreferencesRepository>()
            .AddScoped<SponsorBlockSegmentRepository>()
            .AddScoped<ChannelSubscriptionRepository>()
            .AddNpgsql();
    }

    private static IServiceCollection AddNpgsql(this IServiceCollection services)
    {
        services.TryAddSingleton<NpgsqlMultiHostDataSource>(provider =>
        {
            var options = provider.GetRequiredService<IOptionsMonitor<DatabaseOptions>>().CurrentValue;
            var builder = new NpgsqlSlimDataSourceBuilder(options.ConnectionString);

            builder.EnableTransportSecurity();
            builder.EnableIntegratedSecurity();

            builder.EnableArrays();
            builder.UseLoggerFactory(provider.GetRequiredService<ILoggerFactory>());
            builder.UseNodaTime();

            return builder.BuildMultiHost();
        });

        services.TryAddSingleton<NpgsqlDataSource>(provider =>
            provider.GetRequiredService<NpgsqlMultiHostDataSource>());
        services.TryAddScoped<NpgsqlConnection>(provider =>
            provider.GetRequiredService<NpgsqlDataSource>().CreateConnection());

        return services;
    }
}
