using Microsoft.EntityFrameworkCore;
using SmartPay.BuildingBlocks;
using SmartPay.PaymentService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PaymentDbContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("PaymentDatabase")
        ?? Environment.GetEnvironmentVariable("PAYMENT_DB_CONNECTION")
        ?? "Host=localhost;Port=5435;Database=payment;Username=smartpay;Password=local_dev_only";

    options.UseNpgsql(connectionString);
});

builder.Services.AddHttpClient("RiskService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["RiskService:BaseUrl"] ?? "http://localhost:5104");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient("LedgerService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["LedgerService:BaseUrl"] ?? "http://localhost:5105");
});

builder.Services.AddHostedService<OutboxPublisher>();
var app = builder.Build();

// For this initial local-development step. We'll replace this with EF migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/health", () =>
    Results.Ok(new HealthResponse("Payment", "Healthy", DateTimeOffset.UtcNow)));

app.MapPost("/payments", async (
    HttpRequest http,
    CreatePaymentRequest request,
    PaymentDbContext db,
    CancellationToken cancellationToken) =>
{
    var key = http.Headers["Idempotency-Key"].FirstOrDefault();

    if (string.IsNullOrWhiteSpace(key))
        return Results.BadRequest(new { error = "Idempotency-Key header is required." });

    if (key.Length > 200)
        return Results.BadRequest(new { error = "Idempotency-Key must be 200 characters or fewer." });

    if (request.Amount <= 0)
        return Results.BadRequest(new { error = "Amount must be greater than zero." });

    if (!string.Equals(request.Currency, "SAR", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Only SAR is supported in this demo." });

    if (string.IsNullOrWhiteSpace(request.CustomerWalletId) ||
        string.IsNullOrWhiteSpace(request.MerchantId) ||
        string.IsNullOrWhiteSpace(request.Reference))
        return Results.BadRequest(new { error = "Wallet ID, merchant ID and reference are required." });

    var fingerprint =
        $"{request.CustomerWalletId}|{request.MerchantId}|{request.Amount:0.00}|SAR|{request.Reference}";

    var existing = await db.Payments
        .AsNoTracking()
        .SingleOrDefaultAsync(p => p.IdempotencyKey == key, cancellationToken);

    if (existing is not null)
    {
        if (existing.Fingerprint != fingerprint)
            return Results.Conflict(new { error = "Idempotency key was already used with a different request." });

        return Results.Ok(ToResponse(existing));
    }

    var payment = new PaymentEntity
    {
        Id = Guid.NewGuid(),
        CustomerWalletId = request.CustomerWalletId,
        MerchantId = request.MerchantId,
        Amount = request.Amount,
        Currency = "SAR",
        Reference = request.Reference,
        Status = "Pending",
        CreatedAtUtc = DateTimeOffset.UtcNow,
        IdempotencyKey = key,
        Fingerprint = fingerprint
    };

    db.Payments.Add(payment);

    var outboxMessage = new OutboxMessage
    {
        Id = Guid.NewGuid(),
        Type = "PaymentCreated",
        Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                payment.Id,
                payment.CustomerWalletId,
                payment.MerchantId,
                payment.Amount,
                payment.Currency,
                payment.Reference,
                payment.Status,
                payment.CreatedAtUtc
            }),
        OccurredAtUtc = DateTimeOffset.UtcNow,
        RetryCount = 0
    };

    db.OutboxMessages.Add(outboxMessage);

    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/payments/{payment.Id}", ToResponse(payment));
});


app.MapPost("/payments/{id:guid}/status", async (
    Guid id,
    UpdatePaymentStatusRequest request,
    PaymentDbContext db,
    CancellationToken cancellationToken) =>
{
    var payment = await db.Payments
        .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    if (payment is null)
        return Results.NotFound(new { error = "Payment not found." });

    if (!PaymentStateMachine.TryTransition(
            payment.Status,
            request.Status,
            out var error))
    {
        return Results.Conflict(new { error });
    }

    if (request.Status == "Processing")
    {
        var riskClient = app.Services
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("RiskService");

        var riskRequest = new
        {
            amount = payment.Amount,
            currency = payment.Currency,
            customerWalletId = payment.CustomerWalletId,
            merchantId = payment.MerchantId
        };

        var riskResponse = await riskClient.PostAsJsonAsync(
            "/risk/evaluate",
            riskRequest,
            cancellationToken);

        if (!riskResponse.IsSuccessStatusCode)
        {
            return Results.StatusCode(503);
        }

        var riskResult =
            await riskResponse.Content.ReadFromJsonAsync<RiskResponse>(
                cancellationToken);

        if (riskResult is null)
        {
            return Results.StatusCode(503);
        }

        if (riskResult.Decision == "Rejected")
        {
            return Results.Conflict(new
            {
                error = "Payment rejected by risk service.",
                reason = riskResult.Reason
            });
        }

        if (riskResult.Decision == "Review")
        {
            return Results.Conflict(new
            {
                error = "Payment requires risk review.",
                reason = riskResult.Reason
            });
        }
    }

    var previousStatus = payment.Status;
    payment.Status = request.Status;
    if (request.Status == "Completed")
    {
        var ledgerClient = app.Services
                .GetRequiredService<IHttpClientFactory>()
                        .CreateClient("LedgerService");

                            var ledgerRequest = new
                                {
                                        transactionId = payment.Id,
                                                accountName = payment.CustomerWalletId,
                                                        counterpartyAccountName = payment.MerchantId,
                                                                amount = payment.Amount,
                                                                        currency = payment.Currency
                                                                            };

                                                                                var ledgerResponse = await ledgerClient.PostAsJsonAsync(
                                                                                        "/ledger/entry",
                                                                                                ledgerRequest,
                                                                                                        cancellationToken);

                                                                                                            if (!ledgerResponse.IsSuccessStatusCode &&
                                                                                                                    (int)ledgerResponse.StatusCode != 409)
                                                                                                                        {
                                                                                                                                return Results.StatusCode(503);
                                                                                                                                    }
                                                                                                                                    }


    var outboxMessage = new OutboxMessage
    {
        Id = Guid.NewGuid(),
        Type = "PaymentStatusChanged",
        Payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            payment.Id,
            payment.CustomerWalletId,
            payment.MerchantId,
            payment.Amount,
            payment.Currency,
            payment.Reference,
            PreviousStatus = previousStatus,
            NewStatus = payment.Status,
            ChangedAtUtc = DateTimeOffset.UtcNow
        }),
        OccurredAtUtc = DateTimeOffset.UtcNow,
        RetryCount = 0
    };

    db.OutboxMessages.Add(outboxMessage);

    await db.SaveChangesAsync(cancellationToken);

    return Results.Ok(ToResponse(payment));
});

app.MapGet("/payments/{id:guid}", async (
    Guid id,
    PaymentDbContext db,
    CancellationToken cancellationToken) =>
{
    var payment = await db.Payments
        .AsNoTracking()
        .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    return payment is null
        ? Results.NotFound()
        : Results.Ok(ToResponse(payment));
});

app.MapGet("/payments", async (
    PaymentDbContext db,
    CancellationToken cancellationToken) =>
{
    var payments = await db.Payments
        .AsNoTracking()
        .OrderByDescending(p => p.CreatedAtUtc)
        .Take(100)
        .ToListAsync(cancellationToken);

    return Results.Ok(payments.Select(ToResponse));
});

app.Run();

static PaymentResponse ToResponse(PaymentEntity payment) =>
    new(
        payment.Id,
        payment.CustomerWalletId,
        payment.MerchantId,
        payment.Amount,
        payment.Currency,
        payment.Reference,
        payment.Status,
        payment.CreatedAtUtc);

public sealed record UpdatePaymentStatusRequest(string Status);

public sealed record RiskResponse(
    string Decision,
    string Reason);

public sealed record CreatePaymentRequest(
    string CustomerWalletId,
    string MerchantId,
    decimal Amount,
    string Currency,
    string Reference);

public sealed record PaymentResponse(
    Guid Id,
    string CustomerWalletId,
    string MerchantId,
    decimal Amount,
    string Currency,
    string Reference,
    string Status,
    DateTimeOffset CreatedAtUtc);
