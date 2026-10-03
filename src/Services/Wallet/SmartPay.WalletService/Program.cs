using SmartPay.BuildingBlocks;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<WalletStore>();
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new HealthResponse("Wallet", "Healthy", DateTimeOffset.UtcNow)));

app.MapPost("/wallets", (CreateWalletRequest request, WalletStore store) => {
    if (request.OwnerId == Guid.Empty) return Results.BadRequest(new { error = "OwnerId is required." });
    if (!string.Equals(request.Currency, "SAR", StringComparison.OrdinalIgnoreCase)) return Results.BadRequest(new { error = "Only SAR is supported in this demo." });
    var wallet = new WalletResponse(Guid.NewGuid(), request.OwnerId, "SAR", 0m, "Active");
    store.Wallets[wallet.Id] = wallet;
    return Results.Created($"/wallets/{wallet.Id}", wallet);
});
app.MapGet("/wallets/{id:guid}", (Guid id, WalletStore store) => store.Wallets.TryGetValue(id, out var item) ? Results.Ok(item) : Results.NotFound());

app.Run();

public sealed record CreateWalletRequest(Guid OwnerId, string Currency);
public sealed record WalletResponse(Guid Id, Guid OwnerId, string Currency, decimal Balance, string Status);
public sealed class WalletStore { public Dictionary<Guid, WalletResponse> Wallets { get; } = new(); }
