using CarbonOps.Api;
using CarbonOps.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCarbonFactorServices(builder.Configuration);
var app = builder.Build();

app.UseMiddleware<ApiErrorMappingMiddleware>();

app.MapOperationalEndpoints();
app.MapCarbonFactorEndpoints();

app.Run();

public partial class Program
{
}
