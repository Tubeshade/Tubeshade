using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
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
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<Access, int>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<TaskType, int>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<TaskResult, int>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<ExternalAvailability, int>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<ImageType, int>());
        SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<VideoContainerType, int>());

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
            .AddScoped<LibraryRepository>()
            .AddScoped<ChannelRepository>()
            .AddScoped<VideoRepository>()
            .AddScoped<VideoFileRepository>()
            .AddScoped<ImageFileRepository>()
            .AddScoped<PreferencesRepository>()
            .AddNpgsql();
    }

    private static IServiceCollection AddNpgsql(this IServiceCollection services)
    {
        services.TryAddSingleton<NpgsqlMultiHostDataSource>(provider =>
        {
            var options = provider.GetRequiredService<IOptionsMonitor<DatabaseOptions>>().CurrentValue;
            var builder = new NpgsqlSlimDataSourceBuilder(options.ConnectionString);

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
