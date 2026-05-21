using CarbonOps.Api;
using CarbonOps.Infrastructure;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddOptions<ApiKeyAuthenticationOptions>()
    .Bind(builder.Configuration.GetSection(ApiKeyAuthenticationOptions.SectionName));
builder.Services.AddCarbonFactorServices(builder.Configuration);
var app = builder.Build();

app.UseMiddleware<ApiErrorMappingMiddleware>();

app.MapOperationalEndpoints();
app.MapCarbonFactorEndpoints();

app.Run();

public partial class Program
{
}
