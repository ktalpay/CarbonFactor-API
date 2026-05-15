namespace CarbonOps.Api;

public sealed record EndpointExample(
    string Name,
    string RequestPath,
    int StatusCode,
    string ResponseBody);

public static class CarbonFactorEndpointExamples
{
    public static EndpointExample ListFactorsSuccess { get; } = new(
        "list-factors-success",
        "/carbon-factors",
        StatusCodes.Status200OK,
        """
        {
          "factors": [
            {
              "id": "f-001",
              "source": "synthetic",
              "category": "electricity",
              "activity": "grid electricity",
              "factor_value": 0.42,
              "factor_unit": "kgCO2e/kWh",
              "region": "US-WEST",
              "year": 2024,
              "notes": "grid sample"
            },
            {
              "id": "f-002",
              "source": "synthetic",
              "category": "transport",
              "activity": "passenger vehicle",
              "factor_value": 0.19,
              "factor_unit": "kgCO2e/km",
              "region": "US",
              "year": 2024,
              "notes": "vehicle sample"
            },
            {
              "id": "f-003",
              "source": "synthetic",
              "category": "electricity",
              "activity": "onsite solar",
              "factor_value": 0.05,
              "factor_unit": "kgCO2e/kWh",
              "region": "TR",
              "year": 2023,
              "notes": "low-carbon sample"
            }
          ],
          "total": 3
        }
        """);

    public static EndpointExample SearchFactorsSuccess { get; } = new(
        "search-factors-success",
        "/carbon-factors/search?category=electricity&activity=grid%20electricity&region=US-WEST&year=2024",
        StatusCodes.Status200OK,
        """
        {
          "factors": [
            {
              "id": "f-001",
              "source": "synthetic",
              "category": "electricity",
              "activity": "grid electricity",
              "factor_value": 0.42,
              "factor_unit": "kgCO2e/kWh",
              "region": "US-WEST",
              "year": 2024,
              "notes": "grid sample"
            }
          ],
          "total": 1
        }
        """);

    public static EndpointExample SearchFactorsInvalidQuery { get; } = new(
        "search-factors-invalid-query",
        "/carbon-factors/search?year=two-thousand-twenty-four",
        StatusCodes.Status400BadRequest,
        """
        {
          "code": "invalid_query",
          "message": "Invalid query",
          "details": {
            "reason": "year must be an integer"
          }
        }
        """);

    public static EndpointExample GetFactorByIdSuccess { get; } = new(
        "get-factor-success",
        "/carbon-factors/f-002",
        StatusCodes.Status200OK,
        """
        {
          "factor": {
            "id": "f-002",
            "source": "synthetic",
            "category": "transport",
            "activity": "passenger vehicle",
            "factor_value": 0.19,
            "factor_unit": "kgCO2e/km",
            "region": "US",
            "year": 2024,
            "notes": "vehicle sample"
          }
        }
        """);

    public static EndpointExample GetFactorByIdNotFound { get; } = new(
        "get-factor-not-found",
        "/carbon-factors/missing-factor",
        StatusCodes.Status404NotFound,
        """
        {
          "code": "not_found",
          "message": "factor not found",
          "details": {
            "id": "missing-factor"
          }
        }
        """);
}
