using Microsoft.EntityFrameworkCore;
using SmartPay.BuildingBlocks;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Payments")
    ?? "Host=localhost;Port=5435;Database=payment;Username=smartpay;Password=local_dev_only";

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
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

    if (request.Amount <= 0)
        return Results.BadRequest(new { error = "Amount must be greater than zero." });

    if (!string.Equals(request.Currency, "SAR", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Only SAR is supported in this demo." });

    if (string.IsNullOrWhiteSpace(request.CustomerWalletId) ||
        string.IsNullOrWhiteSpace(request.MerchantId) ||
        string.IsNullOrWhiteSpace(request.Reference))
        return Results.BadRequest(new { error = "Wallet, merchant and reference are required." });

    var fingerprint =
        $"{request.CustomerWalletId}|{request.MerchantId}|{request.Amount}|SAR|{request.Reference}";

    var existing = await db.IdempotencyRecords
        .AsNoTracking()
        .Include(x => x.Payment)
        .SingleOrDefaultAsync(x => x.Key == key, cancellationToken);

    if (existing is not null)
    {
        if (existing.Fingerprint != fingerprint)
            return Results.Conflict(new
            {
                error = "This idempotency key was already used with a different request."
            });

        return Results.Ok(existing.Payment);
    }

    var payment = new PaymentRecord
    {
        Id = Guid.NewGuid(),
        CustomerWalletId = request.CustomerWalletId,
        MerchantId = request.MerchantId,
        Amount = request.Amount,
        Currency = "SAR",
        Reference = request.Reference,
        Status = "Pending",
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    db.Payments.Add(payment);
    db.IdempotencyRecords.Add(new IdempotencyRecord
    {
        Key = key,
        Fingerprint = fingerprint,
        Payment = payment
    });

    try
    {
        await db.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException)
    {
        // Another request may have used this key concurrently.
        db.ChangeTracker.Clear();

        var concurrent = await db.IdempotencyRecords
            .AsNoTracking()
            .Include(x => x.Payment)
            .SingleOrDefaultAsync(x => x.Key == key, cancellationToken);

        if (concurrent is not null)
        {
            if (concurrent.Fingerprint != fingerprint)
                return Results.Conflict(new
                {
                    error = "This idempotency key was already used with a different request."
                });

            return Results.Ok(concurrent.Payment);
        }

        throw;
    }

    return Results.Created($"/payments/{payment.Id}", payment);
});

app.MapGet("/payments/{id:guid}", async (
    Guid id,
    PaymentDbContext db,
    CancellationToken cancellationToken) =>
{
    var payment = await db.Payments.AsNoTracking()
        .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    return payment is null ? Results.NotFound() : Results.Ok(payment);
});

app.MapGet("/payments", async (
    PaymentDbContext db,
    CancellationToken cancellationToken) =>
{
    var payments = await db.Payments.AsNoTracking()
        .OrderByDescending(x => x.CreatedAtUtc)
        .ToListAsync(cancellationToken);

    return Results.Ok(payments);
});

app.Run();

public sealed record CreatePaymentRequest(
    string CustomerWalletId,
    string MerchantId,
    decimal Amount,
    string Currency,
    string Reference);

public sealed class PaymentRecord
{
    public Guid Id { get; set; }
    public string CustomerWalletId { get; set; } = "";
    public string MerchantId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public string Reference { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class IdempotencyRecord
{
    public string Key { get; set; } = "";
    public string Fingerprint { get; set; } = "";
    public Guid PaymentId { get; set; }
    public PaymentRecord Payment { get; set; } = null!;
}

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options)
    : DbContext(options)
{
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Reference).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CustomerWalletId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.MerchantId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).HasMaxLength(200);
            entity.Property(x => x.Fingerprint).HasMaxLength(1000).IsRequired();
            entity.HasOne(x => x.Payment)
                .WithMany()
                .HasForeignKey(x => x.PaymentId)
                .IsRequired();
        });
    }
}
