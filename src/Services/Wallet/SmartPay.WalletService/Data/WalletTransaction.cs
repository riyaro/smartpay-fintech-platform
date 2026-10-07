namespace SmartPay.WalletService.Data;

public sealed class WalletTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WalletId { get; set; }

    public string Type { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "SAR";

    public string Reference { get; set; } = string.Empty;

    public string? IdempotencyKey { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
