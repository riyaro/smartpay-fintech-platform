using Microsoft.EntityFrameworkCore;

namespace SmartPay.PaymentService;

public sealed class PaymentEntity
{
    public Guid Id { get; set; }
    public string CustomerWalletId { get; set; } = "";
    public string MerchantId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public string Reference { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public string Fingerprint { get; set; } = "";
}

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options)
    : DbContext(options)
{
    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var payment = modelBuilder.Entity<PaymentEntity>();

        payment.ToTable("Payments");
        payment.HasKey(p => p.Id);
        payment.Property(p => p.CustomerWalletId).HasMaxLength(100).IsRequired();
        payment.Property(p => p.MerchantId).HasMaxLength(100).IsRequired();
        payment.Property(p => p.Amount).HasPrecision(18, 2);
        payment.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        payment.Property(p => p.Reference).HasMaxLength(200).IsRequired();
        payment.Property(p => p.Status).HasMaxLength(30).IsRequired();
        payment.Property(p => p.IdempotencyKey).HasMaxLength(200).IsRequired();
        payment.Property(p => p.Fingerprint).HasMaxLength(500).IsRequired();
        payment.HasIndex(p => p.IdempotencyKey).IsUnique();
    }
}
