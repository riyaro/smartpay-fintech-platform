using Microsoft.EntityFrameworkCore;

namespace SmartPay.WalletService.Data;

public sealed class WalletDbContext(DbContextOptions<WalletDbContext> options)
    : DbContext(options)
{
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var wallet = modelBuilder.Entity<Wallet>();

        wallet.ToTable("Wallets");
        wallet.HasKey(x => x.Id);
        wallet.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        wallet.Property(x => x.Balance).HasPrecision(18, 2);
        wallet.Property(x => x.Status).HasMaxLength(20).IsRequired();
        wallet.HasIndex(x => x.OwnerId);

        var transaction = modelBuilder.Entity<WalletTransaction>();

        transaction.ToTable("WalletTransactions");
        transaction.HasKey(x => x.Id);
        transaction.Property(x => x.Type).HasMaxLength(20).IsRequired();
        transaction.Property(x => x.Amount).HasPrecision(18, 2);
        transaction.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        transaction.Property(x => x.Reference).HasMaxLength(200).IsRequired();
        transaction.HasIndex(x => x.WalletId);
        transaction.HasIndex(x => x.CreatedAtUtc);
    }
}
