using System.Net;

namespace TransactionProcessor;

public static class MockTransactionApi
{
    public static string Send(Transaction transaction)
    {
        // A repeatable failure case without network access or random results.
        if (transaction.TransactionId?.StartsWith("FAIL-", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new HttpRequestException("Simulated API failure (HTTP 503).",
                null, HttpStatusCode.ServiceUnavailable);
        }

        return $"SIM-{transaction.TransactionId}";
    }
}