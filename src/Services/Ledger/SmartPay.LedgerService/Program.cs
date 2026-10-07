using Microsoft.EntityFrameworkCore;
using SmartPay.BuildingBlocks;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("LedgerDb")
    ?? "Host=localhost;Port=5437;Database=ledger;Username=smartpay;Password=local_dev_only";

builder.Services.AddDbContext<LedgerDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.MapGet("/health", async (LedgerDbContext db) =>
{
    var healthy = await db.Database.CanConnectAsync();

    return healthy
        ? Results.Ok(new HealthResponse(
            "Ledger",
            "Healthy",
            DateTimeOffset.UtcNow))
        : Results.Json(
            new HealthResponse(
                "Ledger",
                "Unhealthy",
                DateTimeOffset.UtcNow),
            statusCode: 503);
});

app.MapGet("/ledger/entries", async (LedgerDbContext db) =>
{
    var entries = await db.LedgerEntries
        .AsNoTracking()
        .OrderBy(x => x.CreatedAtUtc)
        .ToListAsync();

    return Results.Ok(entries);
});

app.MapPost("/ledger/entry", async (
    DemoLedgerRequest request,
    LedgerDbContext db) =>
{
    if (request.Amount <= 0)
    {
        return Results.BadRequest(new
        {
            error = "Amount must be greater than zero."
        });
    }

    if (string.IsNullOrWhiteSpace(request.Currency))
    {
        return Results.BadRequest(new
        {
            error = "Currency is required."
        });
    }

    if (string.Equals(
        request.AccountName,
        request.CounterpartyAccountName,
        StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new
        {
            error = "Debit and credit accounts must be different."
        });
    }

    var alreadyExists = await db.LedgerEntries
        .AnyAsync(x => x.TransactionId == request.TransactionId);

    if (alreadyExists)
    {
        return Results.Conflict(new
        {
            error = "Transaction has already been posted.",
            transactionId = request.TransactionId
        });
    }

    var now = DateTimeOffset.UtcNow;

    var debit = new LedgerEntry(
        Guid.NewGuid(),
        request.TransactionId,
        request.AccountName,
        "Debit",
        request.Amount,
        request.Currency,
        now);

    var credit = new LedgerEntry(
        Guid.NewGuid(),
        request.TransactionId,
        request.CounterpartyAccountName,
        "Credit",
        request.Amount,
        request.Currency,
        now);

    await using var transaction =
        await db.Database.BeginTransactionAsync();

    db.LedgerEntries.AddRange(debit, credit);

    await db.SaveChangesAsync();

    await transaction.CommitAsync();

    return Results.Created(
        $"/ledger/entries/{request.TransactionId}",
        new
        {
            transactionId = request.TransactionId,
            entries = new[] { debit, credit }
        });
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

public sealed record DemoLedgerRequest(
    Guid TransactionId,
    string AccountName,
    string CounterpartyAccountName,
    decimal Amount,
    string Currency);

public sealed record LedgerEntry(
    Guid Id,
    Guid TransactionId,
    string AccountName,
    string EntryType,
    decimal Amount,
    string Currency,
    DateTimeOffset CreatedAtUtc);
