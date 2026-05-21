using CarbonOps.Application.Factors;
using CarbonOps.Domain;
using Microsoft.EntityFrameworkCore;

namespace CarbonOps.Infrastructure;

public sealed class EfCoreCarbonFactorRepository(CarbonOpsDbContext dbContext) : ICarbonFactorRepository
{
    public IReadOnlyCollection<CarbonFactor> ListCarbonFactors()
    {
        return dbContext.CarbonFactors
            .AsNoTracking()
            .ToList();
    }

    public CarbonFactor? GetCarbonFactorById(string factorId)
    {
        return dbContext.CarbonFactors
            .AsNoTracking()
            .SingleOrDefault(factor => factor.Id == factorId);
    }
}
