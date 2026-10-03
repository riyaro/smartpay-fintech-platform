using SmartPay.BuildingBlocks;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<PaymentStore>();
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new HealthResponse("Payment", "Healthy", DateTimeOffset.UtcNow)));

app.MapPost("/payments", (HttpRequest http, CreatePaymentRequest request, PaymentStore store) => {
    var key = http.Headers["Idempotency-Key"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(key)) return Results.BadRequest(new { error = "Idempotency-Key header is required." });
    if (request.Amount <= 0) return Results.BadRequest(new { error = "Amount must be greater than zero." });
    if (!string.Equals(request.Currency, "SAR", StringComparison.OrdinalIgnoreCase)) return Results.BadRequest(new { error = "Only SAR is supported in this demo." });
    var fingerprint = $"{request.CustomerWalletId}|{request.MerchantId}|{request.Amount}|{request.Currency}|{request.Reference}";
    if (store.Idempotency.TryGetValue(key, out var existing)) {
        if (existing.Fingerprint != fingerprint) return Results.Conflict(new { error = "Idempotency key was already used with a different request." });
        return Results.Ok(existing.Payment);
    }
    var payment = new PaymentResponse(Guid.NewGuid(), request.CustomerWalletId, request.MerchantId, request.Amount, "SAR", request.Reference, "Pending", DateTimeOffset.UtcNow);
    store.Payments[payment.Id] = payment;
    store.Idempotency[key] = new IdempotencyRecord(fingerprint, payment);
    return Results.Created($"/payments/{payment.Id}", payment);
});
app.MapGet("/payments/{id:guid}", (Guid id, PaymentStore store) => store.Payments.TryGetValue(id, out var item) ? Results.Ok(item) : Results.NotFound());
app.MapGet("/payments", (PaymentStore store) => Results.Ok(store.Payments.Values));

app.Run();

public sealed record CreatePaymentRequest(string CustomerWalletId, string MerchantId, decimal Amount, string Currency, string Reference);
public sealed record PaymentResponse(Guid Id, string CustomerWalletId, string MerchantId, decimal Amount, string Currency, string Reference, string Status, DateTimeOffset CreatedAtUtc);
public sealed record IdempotencyRecord(string Fingerprint, PaymentResponse Payment);
public sealed class PaymentStore { public Dictionary<Guid, PaymentResponse> Payments { get; } = new(); public Dictionary<string, IdempotencyRecord> Idempotency { get; } = new(StringComparer.Ordinal); }
