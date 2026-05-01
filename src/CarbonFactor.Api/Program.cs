using System.Net.Mime;
using System.Text.Json;
using CarbonFactor.Api.Endpoints;
using CarbonFactor.Api.Errors;
using CarbonFactor.Api.Storage;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICarbonFactorStore, InMemoryCarbonFactorStore>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

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
