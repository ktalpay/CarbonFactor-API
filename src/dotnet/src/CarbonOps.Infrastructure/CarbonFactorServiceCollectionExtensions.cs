using CarbonOps.Application.Factors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarbonOps.Infrastructure;

public static class CarbonFactorServiceCollectionExtensions
{
    public static IServiceCollection AddCarbonFactorServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var usePostgreSql = configuration?.GetValue<bool>("Persistence:UsePostgreSql") ?? false;

        if (usePostgreSql)
        {
            var options = configuration?
                .GetSection(PostgreSqlPersistenceOptions.SectionName)
                .Get<PostgreSqlPersistenceOptions>();

            if (string.IsNullOrWhiteSpace(options?.ConnectionString))
            {
                throw new InvalidOperationException(
                    $"'{PostgreSqlPersistenceOptions.SectionName}:ConnectionString' is required when PostgreSQL persistence is enabled.");
            }

            services.AddDbContext<CarbonOpsDbContext>(dbOptions => dbOptions.UseNpgsql(options.ConnectionString));
        }

        services.AddSingleton<ICarbonFactorRepository, InMemoryCarbonFactorRepository>();
        services.AddSingleton<CarbonFactorUseCases>();

        return services;
    }
}
