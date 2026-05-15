using CarbonOps.Application.Factors;
using Microsoft.Extensions.DependencyInjection;

namespace CarbonOps.Infrastructure;

public static class CarbonFactorServiceCollectionExtensions
{
    public static IServiceCollection AddCarbonFactorServices(this IServiceCollection services)
    {
        services.AddSingleton<ICarbonFactorRepository, InMemoryCarbonFactorRepository>();
        services.AddSingleton<CarbonFactorUseCases>();

        return services;
    }
}
