using Microsoft.EntityFrameworkCore;
using SmartPay.BuildingBlocks;
using SmartPay.WalletService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WalletDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("WalletDb")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/health", () =>
    Results.Ok(new HealthResponse("Wallet", "Healthy", DateTimeOffset.UtcNow)));

app.MapPost("/wallets", async (
    CreateWalletRequest request,
    WalletDbContext db) =>
{
    if (request.OwnerId == Guid.Empty)
        return Results.BadRequest(new { error = "OwnerId is required." });

    if (!string.Equals(request.Currency, "SAR", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Only SAR is supported in this demo." });

    var wallet = new Wallet
    {
        Id = Guid.NewGuid(),
        OwnerId = request.OwnerId,
        Currency = "SAR",
        Balance = 0m,
        Status = "Active"
    };

    db.Wallets.Add(wallet);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/wallets/{wallet.Id}",
        new WalletResponse(wallet.Id, wallet.OwnerId, wallet.Currency,
            wallet.Balance, wallet.Status));
});

app.MapGet("/wallets/{id:guid}", async (
    Guid id,
    WalletDbContext db) =>
{
    var wallet = await db.Wallets
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == id);

    return wallet is null
        ? Results.NotFound()
        : Results.Ok(new WalletResponse(wallet.Id, wallet.OwnerId,
            wallet.Currency, wallet.Balance, wallet.Status));
});

app.Run();

public sealed record CreateWalletRequest(Guid OwnerId, string Currency);

public sealed record WalletResponse(
    Guid Id,
    Guid OwnerId,
    string Currency,
    decimal Balance,
    string Status);
