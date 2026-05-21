using CarbonOps.Application.Factors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarbonOps.Infrastructure;

public static class CarbonFactorServiceCollectionExtensions
{
    public static IServiceCollection AddCarbonFactorServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddSingleton<IPostgreSqlSchemaScriptCatalog, PostgreSqlSchemaScriptCatalog>();
        services.AddSingleton<PostgreSqlSchemaSafetyValidator>();
        services.AddSingleton<IPostgreSqlSchemaBootstrapPlanner, PostgreSqlSchemaBootstrapPlanner>();

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
            services.AddScoped<ICarbonFactorRepository, EfCoreCarbonFactorRepository>();
            services.AddScoped<ICarbonOpsTransactionBoundary, EfCoreCarbonOpsTransactionBoundary>();
        }
        else
        {
            services.AddSingleton<ICarbonFactorRepository, InMemoryCarbonFactorRepository>();
            services.AddSingleton<ICarbonOpsTransactionBoundary, NoOpCarbonOpsTransactionBoundary>();
        }
        services.AddScoped<CarbonFactorUseCases>();

        return services;
    }
}
