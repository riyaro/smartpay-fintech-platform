using Microsoft.EntityFrameworkCore;

namespace SmartPay.PaymentService.Data;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options)
    : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var payment = modelBuilder.Entity<Payment>();

        payment.HasKey(x => x.Id);
        payment.Property(x => x.CustomerWalletId).HasMaxLength(100).IsRequired();
        payment.Property(x => x.MerchantId).HasMaxLength(100).IsRequired();
        payment.Property(x => x.Amount).HasPrecision(18, 2);
        payment.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        payment.Property(x => x.Reference).HasMaxLength(200).IsRequired();
        payment.Property(x => x.Status).HasMaxLength(30).IsRequired();
        payment.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        payment.Property(x => x.RequestFingerprint).HasMaxLength(1000).IsRequired();

        payment.HasIndex(x => x.IdempotencyKey).IsUnique();
        payment.HasIndex(x => x.CreatedAtUtc);
    }
}
