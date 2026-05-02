using System.Net.Mime;
using System.Text.Json;
using CarbonFactor.Api.Domain;
using CarbonFactor.Api.Endpoints;
using CarbonFactor.Api.Errors;
using CarbonFactor.Api.Options;
using CarbonFactor.Api.Storage;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICarbonFactorStore, InMemoryCarbonFactorStore>();
builder.Services.AddSingleton<CarbonFactorNormalizer>();
builder.Services.AddSingleton<CarbonFactorValidator>();
builder.Services.AddSingleton<IValidateOptions<CarbonFactorApiOptions>, CarbonFactorApiOptionsValidator>();
builder.Services
    .AddOptions<CarbonFactorApiOptions>()
    .Bind(builder.Configuration.GetSection(CarbonFactorApiOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace('+', '.') ?? type.Name);
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();
var apiOptions = app.Services.GetRequiredService<IOptions<CarbonFactorApiOptions>>().Value;

if (app.Environment.IsDevelopment() || apiOptions.EnableSwaggerInProduction)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CarbonFactor API v1");
    });
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var status = exception is BadHttpRequestException
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

        var response = status == StatusCodes.Status400BadRequest
            ? ApiErrorResponse.Create(
                status,
                "invalid_request",
                "Invalid request.",
                "The request payload could not be read.",
                context.TraceIdentifier,
                [new ApiValidationError("body", "malformed_json", "Request body must be valid JSON.")])
            : ApiErrorResponse.Create(
                status,
                "internal_error",
                "Unexpected error.",
                "The API encountered an unexpected error.",
                context.TraceIdentifier);

        context.Response.StatusCode = status;
        context.Response.ContentType = MediaTypeNames.Application.Json;
        await context.Response.WriteAsJsonAsync(response);
    });
});

app.MapGet("/", () => Results.Redirect("/health"))
    .ExcludeFromDescription();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("GetHealth")
    .WithTags("Health")
    .WithSummary("Returns API health status.");

app.MapCarbonFactorEndpoints();

app.Run();

public partial class Program;
