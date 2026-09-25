using System.Globalization;

namespace TransactionProcessor;

public static class TransactionValidator
{
    public static List<string> Validate(Transaction transaction)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(transaction.TransactionId))
            errors.Add("TransactionId is required.");

        if (string.IsNullOrWhiteSpace(transaction.CustomerId))
            errors.Add("CustomerId is required.");

        if (transaction.Amount is null || transaction.Amount <= 0)
            errors.Add("Amount must be greater than zero.");

        if (!DateOnly.TryParseExact(transaction.TransactionDate, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            errors.Add("TransactionDate must be a valid date in yyyy-MM-dd format.");

        if (string.IsNullOrWhiteSpace(transaction.Status))
            errors.Add("Status is required.");

        return errors;
    }
}