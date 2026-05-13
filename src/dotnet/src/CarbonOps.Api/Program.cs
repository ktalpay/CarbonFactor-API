using CarbonOps.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCarbonFactorServices();
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program
{
}
