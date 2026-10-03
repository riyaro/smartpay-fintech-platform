namespace SmartPay.WalletService.Data;

public sealed class Wallet
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Currency { get; set; } = "SAR";
    public decimal Balance { get; set; }
    public string Status { get; set; } = "Active";
}
