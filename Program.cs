using System.Text.Json;

namespace TransactionProcessor;

public static class Program
{
    private const string Usage = "Usage: dotnet run -- [path-to-transactions.json]";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static int Main(string[] args)
    {
        if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h"))
        {
            Console.WriteLine(Usage);
            Console.WriteLine("Without a path, the bundled samples/transactions.json is used.");
            return 0;
        }

        if (args.Length > 1)
        {
            Console.Error.WriteLine(Usage);
            return 2;
        }

        string inputPath = args.Length == 1
            ? args[0]
            : Path.Combine(AppContext.BaseDirectory, "samples", "transactions.json");

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(inputPath));
        }
        catch (JsonException)
        {
            Console.Error.WriteLine("Input error: the file is not valid JSON. No records were processed.");
            return 2;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                   or ArgumentException or NotSupportedException)
        {
            Console.Error.WriteLine($"Input error: unable to read the file. {ex.Message}");
            return 2;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                Console.Error.WriteLine("Input error: the JSON root must be an array. No records were processed.");
                return 2;
            }

            return ProcessRecords(document.RootElement);
        }
    }
    private static int ProcessRecords(JsonElement records)
    {
        int successful = 0;
        int invalid = 0;
        int failed = 0;
        int recordNumber = 0;

        foreach (JsonElement element in records.EnumerateArray())
        {
            recordNumber++;
            if (element.ValueKind != JsonValueKind.Object)
            {
                invalid++;
                Console.WriteLine($"[INVALID] Record {recordNumber}: each record must be a JSON object.");
                continue;
            }

            Transaction? transaction;
            try
            {
                // Deserializing one object at a time isolates field-type errors.
                transaction = element.Deserialize<Transaction>(JsonOptions);
            }
            catch (JsonException ex)
            {
                invalid++;
                Console.WriteLine($"[INVALID] Record {recordNumber}: invalid field type or value at {ex.Path ?? "$"}.");
                continue;
            }

            if (transaction is null)
            {
                invalid++;
                Console.WriteLine($"[INVALID] Record {recordNumber}: the transaction is missing.");
                continue;
            }

            string label = $"Record {recordNumber} ({JsonSerializer.Serialize(transaction.TransactionId)})";
            List<string> errors = TransactionValidator.Validate(transaction);
            if (errors.Count > 0)
            {
                invalid++;
                Console.WriteLine($"[INVALID] {label}: {string.Join(" ", errors)}");
                continue;
            }

            try
            {
                string reference = MockTransactionApi.Send(transaction);
                successful++;
                Console.WriteLine($"[SUCCESS] {label}: processed (reference: {JsonSerializer.Serialize(reference)}).");
            }
            catch (HttpRequestException ex)
            {
                failed++;
                Console.WriteLine($"[FAILED] {label}: {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Summary");
        Console.WriteLine($"Total records: {recordNumber}");
        Console.WriteLine($"Successful: {successful}");
        Console.WriteLine($"Invalid: {invalid}");
        Console.WriteLine($"Failed: {failed}");
        return invalid == 0 && failed == 0 ? 0 : 1;
    }
}