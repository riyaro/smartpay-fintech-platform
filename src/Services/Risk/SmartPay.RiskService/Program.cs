using SmartPay.BuildingBlocks;
using SmartPay.RiskService;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<RiskStore>();
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new HealthResponse("Risk", "Healthy", DateTimeOffset.UtcNow)));

app.MapPost("/risk/evaluate", (RiskRequest request) => {
    if (request.Amount <= 0) return Results.Ok(new RiskResponse("Rejected", "Amount must be positive."));
    if (request.Amount > 10000m) return Results.Ok(new RiskResponse("Review", "Amount exceeds the demo review threshold."));
    return Results.Ok(new RiskResponse("Approved", "Passed demo risk rules."));
});

app.Run();

public sealed record RiskRequest(decimal Amount, string Currency, string CustomerWalletId, string MerchantId);
public sealed record RiskResponse(string Decision, string Reason);
