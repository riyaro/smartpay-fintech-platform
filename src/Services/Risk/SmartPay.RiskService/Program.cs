using SmartPay.BuildingBlocks;
using SmartPay.RiskService;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<RiskStore>();
var app = builder.Build();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? Guid.NewGuid().ToString();

    context.Response.Headers["X-Correlation-ID"] = correlationId;

    using (app.Logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId
    }))
    {
        app.Logger.LogInformation(
            "Request started: {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        await next();

        app.Logger.LogInformation(
            "Request completed: {Method} {Path} {StatusCode}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode);
    }
} );

app.MapGet("/health", () => Results.Ok(new HealthResponse("Risk", "Healthy", DateTimeOffset.UtcNow)));

app.MapPost("/risk/evaluate", (RiskRequest request) => {
    if (request.Amount <= 0) return Results.Ok(new RiskResponse("Rejected", "Amount must be positive."));
    if (request.Amount > 10000m) return Results.Ok(new RiskResponse("Review", "Amount exceeds the demo review threshold."));
    return Results.Ok(new RiskResponse("Approved", "Passed demo risk rules."));
});

app.Run();

public sealed record RiskRequest(decimal Amount, string Currency, string CustomerWalletId, string MerchantId);
public sealed record RiskResponse(string Decision, string Reason);
