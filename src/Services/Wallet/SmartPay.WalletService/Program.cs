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


app.MapPost("/wallets/{id:guid}/transactions", async (
    Guid id,
    WalletTransactionRequest request,
    WalletDbContext db) =>
{
    if (request.Amount <= 0)
        return Results.BadRequest(new { error = "Amount must be greater than zero." });

    if (!string.Equals(request.Type, "Credit", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(request.Type, "Debit", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Transaction type must be Credit or Debit." });

    if (string.IsNullOrWhiteSpace(request.Reference))
        return Results.BadRequest(new { error = "Reference is required." });

    var wallet = await db.Wallets.FirstOrDefaultAsync(x => x.Id == id);

    if (wallet is null)
        return Results.NotFound();

    if (!string.Equals(wallet.Status, "Active", StringComparison.OrdinalIgnoreCase))
        return Results.Conflict(new { error = "Wallet is not active." });

    if (!string.Equals(wallet.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Transaction currency does not match wallet currency." });

    var isCredit = string.Equals(
        request.Type,
        "Credit",
        StringComparison.OrdinalIgnoreCase);

    if (!isCredit && wallet.Balance < request.Amount)
        return Results.Conflict(new
        {
            error = "Insufficient wallet balance.",
            balance = wallet.Balance,
            requestedAmount = request.Amount
        });

    var now = DateTimeOffset.UtcNow;

    wallet.Balance = isCredit
        ? wallet.Balance + request.Amount
        : wallet.Balance - request.Amount;

    var transaction = new WalletTransaction
    {
        Id = Guid.NewGuid(),
        WalletId = wallet.Id,
        Type = isCredit ? "Credit" : "Debit",
        Amount = request.Amount,
        Currency = wallet.Currency,
        Reference = request.Reference,
        CreatedAtUtc = now
    };

    await using var databaseTransaction = await db.Database.BeginTransactionAsync();

    db.WalletTransactions.Add(transaction);
    await db.SaveChangesAsync();
    await databaseTransaction.CommitAsync();

    return Results.Ok(new WalletTransactionResponse(
        transaction.Id,
        wallet.Id,
        transaction.Type,
        transaction.Amount,
        transaction.Currency,
        transaction.Reference,
        wallet.Balance,
        transaction.CreatedAtUtc));
});

app.Run();

public sealed record CreateWalletRequest(Guid OwnerId, string Currency);

public sealed record WalletTransactionRequest(
    string Type,
    decimal Amount,
    string Currency,
    string Reference);

public sealed record WalletTransactionResponse(
    Guid TransactionId,
    Guid WalletId,
    string Type,
    decimal Amount,
    string Currency,
    string Reference,
    decimal Balance,
    DateTimeOffset CreatedAtUtc);

public sealed record WalletResponse(
    Guid Id,
    Guid OwnerId,
    string Currency,
    decimal Balance,
    string Status);
