using Microsoft.EntityFrameworkCore;

namespace SmartPay.WalletService.Data;

public sealed class WalletDbContext(DbContextOptions<WalletDbContext> options)
    : DbContext(options)
{
    public DbSet<Wallet> Wallets => Set<Wallet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var wallet = modelBuilder.Entity<Wallet>();

        wallet.HasKey(x => x.Id);
        wallet.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        wallet.Property(x => x.Balance).HasPrecision(18, 2);
        wallet.Property(x => x.Status).HasMaxLength(20).IsRequired();

        wallet.HasIndex(x => x.OwnerId);
    }
}
