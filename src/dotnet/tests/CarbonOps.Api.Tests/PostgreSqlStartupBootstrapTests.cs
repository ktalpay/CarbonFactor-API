using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CarbonOps.Api.Tests;

public sealed class PostgreSqlStartupBootstrapTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> sourceFactory;

    public PostgreSqlStartupBootstrapTests(WebApplicationFactory<Program> factory)
    {
        sourceFactory = factory;
    }

    [Fact]
    public async Task UsePostgreSqlFalseDoesNotInvokeBootstrapConfiguration()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Persistence:UsePostgreSql"] = "false",
            ["Persistence:PostgreSql:BootstrapOnStartup"] = "true",
            ["Persistence:PostgreSql:BootstrapMode"] = "DestroyEverything"
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private WebApplicationFactory<Program> CreateFactory(Dictionary<string, string?> configurationValues)
    {
        return sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(configurationValues);
            });
        });
    }

}
