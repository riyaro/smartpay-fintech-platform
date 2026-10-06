namespace SmartPay.PaymentService;

public static class PaymentStateMachine
{
    public static bool TryTransition(
        string currentStatus,
        string newStatus,
        out string error)
    {
        error = string.Empty;

        if (string.Equals(currentStatus, newStatus, StringComparison.OrdinalIgnoreCase))
        {
            error = "Payment is already in this state.";
            return false;
        }

        var allowed = currentStatus switch
        {
            "Pending" => newStatus is "Processing" or "Failed",
            "Processing" => newStatus is "Completed" or "Failed",
            "Completed" => false,
            "Failed" => false,
            _ => false
        };

        if (!allowed)
        {
            error = $"Invalid payment transition: {currentStatus} -> {newStatus}.";
            return false;
        }

        return true;
    }
}
