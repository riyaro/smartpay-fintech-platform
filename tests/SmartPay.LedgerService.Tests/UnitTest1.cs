using Microsoft.EntityFrameworkCore;

public class LedgerTests
{
    private static LedgerDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LedgerDbContext(options);
    }

    [Fact]
    public async Task Transaction_ShouldHave_EqualDebitAndCredit()
    {
        await using var db = CreateDb();

        var transactionId = Guid.NewGuid();

        db.LedgerEntries.AddRange(
            new LedgerEntry(
                Guid.NewGuid(),
                transactionId,
                "CustomerWallet",
                "Debit",
                125.50m,
                "SAR",
                DateTimeOffset.UtcNow),

            new LedgerEntry(
                Guid.NewGuid(),
                transactionId,
                "MerchantAccount",
                "Credit",
                125.50m,
                "SAR",
                DateTimeOffset.UtcNow));

        await db.SaveChangesAsync();

        var entries = await db.LedgerEntries
            .Where(x => x.TransactionId == transactionId)
            .ToListAsync();

        var debitTotal = entries
            .Where(x => x.EntryType == "Debit")
            .Sum(x => x.Amount);

        var creditTotal = entries
            .Where(x => x.EntryType == "Credit")
            .Sum(x => x.Amount);

        Assert.Equal(2, entries.Count);
        Assert.Equal(debitTotal, creditTotal);
        Assert.Equal(125.50m, debitTotal);
    }

    [Fact]
    public async Task Transaction_ShouldContain_BothDebitAndCredit()
    {
        await using var db = CreateDb();

        var transactionId = Guid.NewGuid();

        db.LedgerEntries.AddRange(
            new LedgerEntry(
                Guid.NewGuid(),
                transactionId,
                "CustomerWallet",
                "Debit",
                50m,
                "SAR",
                DateTimeOffset.UtcNow),

            new LedgerEntry(
                Guid.NewGuid(),
                transactionId,
                "MerchantAccount",
                "Credit",
                50m,
                "SAR",
                DateTimeOffset.UtcNow));

        await db.SaveChangesAsync();

        var entries = await db.LedgerEntries
            .Where(x => x.TransactionId == transactionId)
            .ToListAsync();

        Assert.Contains(entries, x => x.EntryType == "Debit");
        Assert.Contains(entries, x => x.EntryType == "Credit");
    }

    [Fact]
    public async Task LedgerEntries_ShouldBePersisted()
    {
        await using var db = CreateDb();

        var transactionId = Guid.NewGuid();

        db.LedgerEntries.Add(
            new LedgerEntry(
                Guid.NewGuid(),
                transactionId,
                "CustomerWallet",
                "Debit",
                100m,
                "SAR",
                DateTimeOffset.UtcNow));

        await db.SaveChangesAsync();

        var entry = await db.LedgerEntries
            .SingleAsync(x => x.TransactionId == transactionId);

        Assert.Equal(100m, entry.Amount);
        Assert.Equal("SAR", entry.Currency);
        Assert.Equal("Debit", entry.EntryType);
    }

    [Fact]
    public async Task Transaction_ShouldUse_SameTransactionId_ForBothEntries()
    {
        await using var db = CreateDb();

        var transactionId = Guid.NewGuid();

        db.LedgerEntries.AddRange(
            new LedgerEntry(
                Guid.NewGuid(),
                transactionId,
                "CustomerWallet",
                "Debit",
                75m,
                "SAR",
                DateTimeOffset.UtcNow),

            new LedgerEntry(
                Guid.NewGuid(),
                transactionId,
                "MerchantAccount",
                "Credit",
                75m,
                "SAR",
                DateTimeOffset.UtcNow));

        await db.SaveChangesAsync();

        var entries = await db.LedgerEntries
            .Where(x => x.TransactionId == transactionId)
            .ToListAsync();

        Assert.Equal(2, entries.Count);
        Assert.All(entries, entry =>
            Assert.Equal(transactionId, entry.TransactionId));
    }
}
