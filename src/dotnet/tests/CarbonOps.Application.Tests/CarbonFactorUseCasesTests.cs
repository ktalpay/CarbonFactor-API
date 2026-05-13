using CarbonOps.Application.Factors;
using CarbonOps.Contracts;
using CarbonOps.Domain;

namespace CarbonOps.Application.Tests;

public sealed class CarbonFactorUseCasesTests
{
    [Fact]
    public void ListCarbonFactorsReturnsFactorsSortedById()
    {
        var useCases = new CarbonFactorUseCases(new FakeCarbonFactorRepository(SampleFactors()));

        var response = useCases.ListCarbonFactors();

        Assert.Equal(3, response.Total);
        Assert.Equal(["f-001", "f-002", "f-003"], response.Factors.Select(factor => factor.Id));
    }

    [Fact]
    public void GetCarbonFactorByIdReturnsMatchingFactor()
    {
        var useCases = new CarbonFactorUseCases(new FakeCarbonFactorRepository(SampleFactors()));

        var result = useCases.GetCarbonFactorById("f-002");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal("transport", result.Value!.Factor.Category);
        Assert.Equal("US", result.Value.Factor.Region);
    }

    [Fact]
    public void GetCarbonFactorByIdReturnsNotFoundErrorForMissingFactor()
    {
        var useCases = new CarbonFactorUseCases(new FakeCarbonFactorRepository(SampleFactors()));

        var result = useCases.GetCarbonFactorById("missing-factor");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal("not_found", result.Error!.Code);
        Assert.Equal("factor not found", result.Error.Message);
        Assert.Equal("missing-factor", result.Error.Details["id"]);
    }

    [Fact]
    public void GetCarbonFactorByIdReturnsInvalidQueryForBlankId()
    {
        var repository = new FakeCarbonFactorRepository(SampleFactors());
        var useCases = new CarbonFactorUseCases(repository);

        var result = useCases.GetCarbonFactorById("   ");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal("invalid_query", result.Error!.Code);
        Assert.Equal("Invalid query", result.Error.Message);
        Assert.Equal("factor id is required", result.Error.Details["reason"]);
        Assert.False(repository.GetCarbonFactorByIdWasCalled);
    }

    [Fact]
    public void SearchCarbonFactorsAppliesSupportedFilters()
    {
        var useCases = new CarbonFactorUseCases(new FakeCarbonFactorRepository(SampleFactors()));
        var query = new FactorQuery(
            Category: "electricity",
            Activity: "grid electricity",
            Region: "US-WEST",
            Year: 2024);

        var result = useCases.SearchCarbonFactors(query);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(1, result.Value!.Total);
        Assert.Equal("f-001", result.Value.Factors.Single().Id);
    }

    [Fact]
    public void SearchCarbonFactorsReturnsInvalidQueryForNonPositiveYear()
    {
        var useCases = new CarbonFactorUseCases(new FakeCarbonFactorRepository(SampleFactors()));

        var result = useCases.SearchCarbonFactors(new FactorQuery(Year: 0));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal("invalid_query", result.Error!.Code);
        Assert.Equal("Invalid query", result.Error.Message);
        Assert.Equal("year must be positive", result.Error.Details["reason"]);
    }

    [Fact]
    public void SearchCarbonFactorsReturnsInvalidQueryForUnsupportedFilters()
    {
        var useCases = new CarbonFactorUseCases(new FakeCarbonFactorRepository(SampleFactors()));
        var requestedFilterNames = new[] { "zeta", "category", "alpha" };

        var result = useCases.SearchCarbonFactors(new FactorQuery(), requestedFilterNames);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal("invalid_query", result.Error!.Code);
        Assert.Equal("unsupported filters: alpha, zeta", result.Error.Details["reason"]);
    }

    private static IReadOnlyCollection<CarbonFactor> SampleFactors()
    {
        return
        [
            new CarbonFactor(
                "f-003",
                "synthetic",
                "electricity",
                "onsite solar",
                0.05m,
                "kgCO2e/kWh",
                "TR",
                2023,
                "low-carbon sample"),
            new CarbonFactor(
                "f-001",
                "synthetic",
                "electricity",
                "grid electricity",
                0.42m,
                "kgCO2e/kWh",
                "US-WEST",
                2024,
                "grid sample"),
            new CarbonFactor(
                "f-002",
                "synthetic",
                "transport",
                "passenger vehicle",
                0.19m,
                "kgCO2e/km",
                "US",
                2024,
                "vehicle sample")
        ];
    }

    private sealed class FakeCarbonFactorRepository : ICarbonFactorRepository
    {
        private readonly IReadOnlyCollection<CarbonFactor> factors;

        public FakeCarbonFactorRepository(IReadOnlyCollection<CarbonFactor> factors)
        {
            this.factors = factors;
        }

        public IReadOnlyCollection<CarbonFactor> ListCarbonFactors()
        {
            return factors;
        }

        public bool GetCarbonFactorByIdWasCalled { get; private set; }

        public CarbonFactor? GetCarbonFactorById(string factorId)
        {
            GetCarbonFactorByIdWasCalled = true;

            return factors.SingleOrDefault(factor => factor.Id == factorId);
        }
    }
}
