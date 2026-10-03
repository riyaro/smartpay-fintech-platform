using Microsoft.EntityFrameworkCore;
using SmartPay.BuildingBlocks;
using SmartPay.PaymentService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PaymentDb")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/health", () =>
    Results.Ok(new HealthResponse("Payment", "Healthy", DateTimeOffset.UtcNow)));

app.MapPost("/payments", async (
    HttpRequest httpRequest,
    CreatePaymentRequest request,
    PaymentDbContext db) =>
{
    var key = httpRequest.Headers["Idempotency-Key"].FirstOrDefault()?.Trim();

    if (string.IsNullOrWhiteSpace(key))
        return Results.BadRequest(new { error = "Idempotency-Key header is required." });

    if (key.Length > 200)
        return Results.BadRequest(new { error = "Idempotency-Key must not exceed 200 characters." });

    if (request.Amount <= 0)
        return Results.BadRequest(new { error = "Amount must be greater than zero." });

    if (request.Amount > 9999999999999999.99m)
        return Results.BadRequest(new { error = "Amount is too large." });

    if (!string.Equals(request.Currency, "SAR", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Only SAR is supported in this demo." });

    if (string.IsNullOrWhiteSpace(request.CustomerWalletId) ||
        string.IsNullOrWhiteSpace(request.MerchantId))
        return Results.BadRequest(new { error = "CustomerWalletId and MerchantId are required." });

    if (string.IsNullOrWhiteSpace(request.Reference))
        return Results.BadRequest(new { error = "Reference is required." });

    var fingerprint =
        $"{request.CustomerWalletId}|{request.MerchantId}|{request.Amount}|{request.Currency}|{request.Reference}";

    var existing = await db.Payments
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.IdempotencyKey == key);

    if (existing is not null)
    {
        if (existing.RequestFingerprint != fingerprint)
            return Results.Conflict(new
            {
                error = "Idempotency key was already used with a different request."
            });

        return Results.Ok(ToResponse(existing));
    }

    var payment = new Payment
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
        RequestFingerprint = fingerprint
    };

    db.Payments.Add(payment);

    try
    {
        await db.SaveChangesAsync();
        return Results.Created($"/payments/{payment.Id}", ToResponse(payment));
    }
    catch (DbUpdateException)
    {
        // Another request may have inserted the same key concurrently.
        db.ChangeTracker.Clear();

        var concurrentPayment = await db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == key);

        if (concurrentPayment is null)
            throw;

        if (concurrentPayment.RequestFingerprint != fingerprint)
            return Results.Conflict(new
            {
                error = "Idempotency key was already used with a different request."
            });

        return Results.Ok(ToResponse(concurrentPayment));
    }
});

app.MapGet("/payments/{id:guid}", async (
    Guid id,
    PaymentDbContext db) =>
{
    var payment = await db.Payments
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == id);

    return payment is null
        ? Results.NotFound()
        : Results.Ok(ToResponse(payment));
});

app.MapGet("/payments", async (PaymentDbContext db) =>
{
    var payments = await db.Payments
        .AsNoTracking()
        .OrderByDescending(x => x.CreatedAtUtc)
        .Select(x => new PaymentResponse(
            x.Id,
            x.CustomerWalletId,
            x.MerchantId,
            x.Amount,
            x.Currency,
            x.Reference,
            x.Status,
            x.CreatedAtUtc))
        .ToListAsync();

    return Results.Ok(payments);
});

app.Run();

static PaymentResponse ToResponse(Payment payment) =>
    new(
        payment.Id,
        payment.CustomerWalletId,
        payment.MerchantId,
        payment.Amount,
        payment.Currency,
        payment.Reference,
        payment.Status,
        payment.CreatedAtUtc);

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
