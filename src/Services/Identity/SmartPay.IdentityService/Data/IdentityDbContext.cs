using Microsoft.EntityFrameworkCore;

namespace SmartPay.IdentityService.Data;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var customer = modelBuilder.Entity<Customer>();

        customer.HasKey(x => x.Id);
        customer.Property(x => x.Email)
            .HasMaxLength(320)
            .IsRequired();

        customer.Property(x => x.KycStatus)
            .HasMaxLength(30)
            .IsRequired();

        customer.HasIndex(x => x.Email).IsUnique();
    }
}
