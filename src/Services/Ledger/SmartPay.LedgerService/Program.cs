using SmartPay.BuildingBlocks;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<LedgerStore>();
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new HealthResponse("Ledger", "Healthy", DateTimeOffset.UtcNow)));

app.MapGet("/ledger/entries", (LedgerStore store) => Results.Ok(store.Entries));
app.MapPost("/ledger/demo-entry", (DemoLedgerRequest request, LedgerStore store) => {
    if (request.Amount <= 0) return Results.BadRequest(new { error = "Amount must be greater than zero." });
    var transactionId = Guid.NewGuid();
    store.Entries.Add(new LedgerEntry(Guid.NewGuid(), transactionId, request.AccountName, "Debit", request.Amount, "SAR", DateTimeOffset.UtcNow));
    store.Entries.Add(new LedgerEntry(Guid.NewGuid(), transactionId, request.CounterpartyAccountName, "Credit", request.Amount, "SAR", DateTimeOffset.UtcNow));
    return Results.Created($"/ledger/entries/{transactionId}", new { transactionId, entries = store.Entries.Where(x => x.TransactionId == transactionId) });
});

app.Run();

public sealed record DemoLedgerRequest(string AccountName, string CounterpartyAccountName, decimal Amount);
public sealed record LedgerEntry(Guid Id, Guid TransactionId, string AccountName, string EntryType, decimal Amount, string Currency, DateTimeOffset CreatedAtUtc);
public sealed class LedgerStore { public List<LedgerEntry> Entries { get; } = new(); }
