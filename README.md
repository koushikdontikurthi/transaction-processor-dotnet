# Transaction Processor

A small C#/.NET 10 console app that reads a batch of transactions from a JSON file, validates each record, sends valid records to a mock API, and prints a summary of successful, invalid, and failed records.

## How to run

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
    dotnet build                            #complie and check for erros
    dotnet run                              # uses samples/transactions.json
    dotnet run -- samples/valid.json        # run any other file
    dotnet run -- --help

The default sample should end with:

    Summary
    Total records: 10
    Successful: 3
    Invalid: 5
    Failed: 2

Exit codes: `0` = every record succeeded (or the batch was empty), `1` = the batch finished but some records were invalid or failed, `2` = the file couldn't be processed at all (missing, malformed JSON, or not an array).

The `samples/` folder has one file per scenario: all valid, all invalid, API failures, empty, malformed, wrong root, and edge cases.

## How it works

The code is split by responsibility:

- `Transaction.cs` is the data model. Every property is nullable so a missing field reaches validation instead of silently becoming a default value.
- `TransactionValidator.cs` holds the rules and returns a list of every problem with a record, not just the first one.
- `MockTransactionApi.cs` simulates the external API.
- `Program.cs` reads the file, runs each record through the pipeline, and counts outcomes.

The main design decision is in how the JSON is read. The file is first parsed as a whole with `JsonDocument` (which only checks syntax), and then each record is deserialized on its own inside a try/catch. If I deserialized the whole array into a `List<Transaction>` in one step, a single record with a wrong type (like `"amount": "abc"`) would throw and lose the entire batch.

I kept invalid and failed records separate because they need different fixes: an invalid record has bad data and won't succeed on retry, while a failed record was fine but hit an API problem, so retrying later might work.

## Assumptions

- Property names match case-insensitively (`transactionId` or `TransactionId`).
- Dates must be real calendar dates in `yyyy-MM-dd` format, so `2026-02-30` is rejected and `2024-02-29` is accepted.
- Amount must be a JSON number greater than zero. I didn't add a currency or decimal-place rule since none was specified.
- Status is required. This is my own addition; the exercise only listed the IDs, amount, and date as required.
- Duplicate transaction IDs are processed independently.
- The mock API fails any transaction whose ID starts with `FAIL-` (simulating an HTTP 503). I made it deterministic rather than random so results are repeatable.
- If the file itself is broken, the program stops with an error instead of guessing at partial data.

## If this were going to production

- Replace the mock with a real `HttpClient` implementation behind an `ITransactionApi` interface, made async, with timeouts and cancellation.
- Retry only transient failures (timeouts, 503s) with backoff, and send an idempotency key so a retry can't create a duplicate transaction.
- Write invalid and failed records to an output file or queue so they can be corrected and replayed, instead of only printing them.
- Add structured logging, keeping customer data out of the logs.
- Stream large files instead of loading them fully into memory, and consider processing records with bounded concurrency.
- Add unit tests (xUnit) for the validator and the processing loop, and run them in CI.
- Confirm the real business rules: currency precision, allowed statuses, and how duplicates should be handled.

## What I'd do next

Unit tests. The sample files already cover each scenario, so the next step would be turning them into xUnit tests that check exact counts and exit codes.

## Tools and resources

- **Microsoft Learn documentation:** System.Text.Json deserialization and the JSON DOM (`JsonDocument`), `DateOnly.TryParseExact`, nullable reference types, and exception filters.
- **AI tools:** I used ChatGPT to draft an initial version of the solution, and Claude as a tutor while I rebuilt it file by file. I typed and ran each piece myself and used the AI explanations and the docs to understand the C# idioms involved, since C#/.NET is new to me professionally.
- **Verification:** I ran every sample file and checked the summary counts and exit codes against what I expected.
