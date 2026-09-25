namespace TransactionProcessor;

public sealed class Transaction
{
    public string? TransactionId { get; set; }
    public string? CustomerId { get; set; }
    public decimal? Amount { get; set; }
    
    // Keep the original text so an invalid date can be rejected per record.
    public string? TransactionDate { get; set; }
    public string? Status { get; set; }
}