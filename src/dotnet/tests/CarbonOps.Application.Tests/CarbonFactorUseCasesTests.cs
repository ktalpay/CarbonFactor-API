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
    public void ListCarbonFactorsAppliesOffsetAndLimit()
    {
        var useCases = new CarbonFactorUseCases(new FakeCarbonFactorRepository(SampleFactors()));

        var result = useCases.ListCarbonFactors(
            new FactorPaginationQuery(Offset: 1, Limit: 1),
            ["offset", "limit"]);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(3, result.Value!.Total);
        Assert.Equal(["f-002"], result.Value.Factors.Select(factor => factor.Id));
    }

    [Fact]
    public void ListCarbonFactorsUsesDeterministicTieBreakersWhenIdsMatch()
    {
        var useCases = new CarbonFactorUseCases(new FakeCarbonFactorRepository(DuplicateIdFactors()));

        var response = useCases.ListCarbonFactors();

        Assert.Equal(4, response.Total);
        Assert.Equal(
            ["source-a/electricity/alpha", "source-a/electricity/beta", "source-b/electricity/alpha", "source-c/transport/zeta"],
            response.Factors.Select(factor => $"{factor.Source}/{factor.Category}/{factor.Activity}"));
    }

    [Fact]
    public void ListCarbonFactorsReturnsInvalidQueryForNegativeOffset()
    {
        var useCases = new CarbonFactorUseCases(new FakeCarbonFactorRepository(SampleFactors()));

        var result = useCases.ListCarbonFactors(new FactorPaginationQuery(Offset: -1), ["offset"]);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal("offset must be zero or positive", result.Error!.Details["reason"]);
    }

    [Fact]
    public void ListCarbonFactorsReturnsInvalidQueryForUnsupportedFilters()
    {
        var useCases = new CarbonFactorUseCases(new FakeCarbonFactorRepository(SampleFactors()));

        var result = useCases.ListCarbonFactors(new FactorPaginationQuery(), ["limit", "category"]);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal("unsupported filters: category", result.Error!.Details["reason"]);
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
    public void SearchCarbonFactorsAppliesPaginationAfterFiltering()
    {
        var useCases = new CarbonFactorUseCases(new FakeCarbonFactorRepository(SampleFactors()));

        var result = useCases.SearchCarbonFactors(
            new FactorQuery(Year: 2024),
            new FactorPaginationQuery(Offset: 1, Limit: 1),
            ["year", "offset", "limit"]);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(2, result.Value!.Total);
        Assert.Equal(["f-002"], result.Value.Factors.Select(factor => factor.Id));
    }

    [Fact]
    public void SearchCarbonFactorsAppliesDeterministicSortingBeforePaginationWhenIdsMatch()
    {
        var useCases = new CarbonFactorUseCases(new FakeCarbonFactorRepository(DuplicateIdFactors()));

        var result = useCases.SearchCarbonFactors(
            new FactorQuery(Year: 2024),
            new FactorPaginationQuery(Offset: 1, Limit: 2),
            ["year", "offset", "limit"]);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(4, result.Value!.Total);
        Assert.Equal(
            ["source-a/electricity/beta", "source-b/electricity/alpha"],
            result.Value.Factors.Select(factor => $"{factor.Source}/{factor.Category}/{factor.Activity}"));
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

        var result = useCases.SearchCarbonFactors(
            new FactorQuery(),
            requestedFilterNames: requestedFilterNames);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal("invalid_query", result.Error!.Code);
        Assert.Equal("unsupported filters: alpha, zeta", result.Error.Details["reason"]);
    }

    [Fact]
    public void SearchCarbonFactorsReturnsInvalidQueryForNonPositiveLimit()
    {
        var useCases = new CarbonFactorUseCases(new FakeCarbonFactorRepository(SampleFactors()));

        var result = useCases.SearchCarbonFactors(
            new FactorQuery(),
            new FactorPaginationQuery(Limit: 0),
            ["limit"]);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal("limit must be positive", result.Error!.Details["reason"]);
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

    private static IReadOnlyCollection<CarbonFactor> DuplicateIdFactors()
    {
        return
        [
            new CarbonFactor(
                "f-001",
                "source-c",
                "transport",
                "zeta",
                0.30m,
                "kgCO2e/km",
                "US",
                2024,
                "note-c"),
            new CarbonFactor(
                "f-001",
                "source-b",
                "electricity",
                "alpha",
                0.20m,
                "kgCO2e/kWh",
                "TR",
                2024,
                "note-b"),
            new CarbonFactor(
                "f-001",
                "source-a",
                "electricity",
                "beta",
                0.10m,
                "kgCO2e/kWh",
                "US",
                2024,
                "note-a2"),
            new CarbonFactor(
                "f-001",
                "source-a",
                "electricity",
                "alpha",
                0.10m,
                "kgCO2e/kWh",
                "US",
                2024,
                "note-a1")
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
