using Microsoft.EntityFrameworkCore;

public sealed class LedgerDbContext : DbContext
{
    public LedgerDbContext(DbContextOptions<LedgerDbContext> options)
        : base(options)
    {
    }

    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entry = modelBuilder.Entity<LedgerEntry>();

        entry.ToTable("LedgerEntries");

        entry.HasKey(x => x.Id);

        entry.Property(x => x.AccountName)
            .HasMaxLength(200)
            .IsRequired();

        entry.Property(x => x.EntryType)
            .HasMaxLength(20)
            .IsRequired();

        entry.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        entry.Property(x => x.Amount)
            .HasPrecision(18, 2);

        entry.HasIndex(x => x.TransactionId);
    }
}
