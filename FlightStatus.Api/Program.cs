using FlightStatus.Api.Models;
using FlightStatus.Api.Providers;
using FlightStatus.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Register providers — endpoint only depends on IFlightStatusProvider, never on concrete types
builder.Services.AddSingleton<IFlightStatusProvider, AeroTrackStubProvider>();
builder.Services.AddSingleton<IFlightStatusProvider, QuickFlightStubProvider>();
builder.Services.AddSingleton<FlightStatusNormaliser>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapGet("/flights/status", async (
    string? flightNumber,
    string? date,
    IEnumerable<IFlightStatusProvider> providers,
    FlightStatusNormaliser normaliser,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(flightNumber))
        return Results.BadRequest(new { error = "flightNumber is required." });

    if (string.IsNullOrWhiteSpace(date) || !DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsedDate))
        return Results.BadRequest(new { error = "date is required and must be in yyyy-MM-dd format." });

    logger.LogInformation("Flight status request: {FlightNumber} on {Date}", flightNumber, parsedDate);

    var providerList = providers.ToList();
    var tasks        = providerList.Select(p => FetchSafe(p, flightNumber, parsedDate, logger));
    var results      = await Task.WhenAll(tasks);

    var aeroTrack  = results.FirstOrDefault(r => r?.ProviderName == "AeroTrack");
    var quickFlight = results.FirstOrDefault(r => r?.ProviderName == "QuickFlight");

    var response = normaliser.Normalise(flightNumber, parsedDate, aeroTrack, quickFlight);

    logger.LogInformation(
        "Returning status {Status} from provider {Source} for {FlightNumber}",
        response.Status, response.ProviderSource, flightNumber);

    return Results.Ok(response);
})
.WithName("GetFlightStatus")
.WithOpenApi();

app.Run();

static async Task<ProviderResult?> FetchSafe(
    IFlightStatusProvider provider,
    string flightNumber,
    DateOnly date,
    ILogger logger)
{
    try
    {
        return await provider.GetStatusAsync(flightNumber, date);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Provider {Provider} threw an exception for {FlightNumber}", provider.ProviderName, flightNumber);
        return null;
    }
}
