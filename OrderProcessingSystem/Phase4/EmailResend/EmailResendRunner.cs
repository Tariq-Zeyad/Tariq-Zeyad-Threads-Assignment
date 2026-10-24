
namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase4.EmailResend;

public sealed class EmailResendRunner
{
    private readonly EmailResendService _service;

    public EmailResendRunner(EmailResendService service)
    {
        _service = service;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        Console.WriteLine("\n=== Task 10.4B: Parallel Email Resend ===");

        var result = await _service.SendBatchAsync(ct);

        Console.WriteLine($"Successful sends: {result.SuccessfulSends}/30");
        Console.WriteLine($"Saved test emails: {result.SavedEmailCount}/30");
        Console.WriteLine($"Highest concurrency: {result.HighestConcurrent}");
        Console.WriteLine($"Elapsed: {result.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine(
            $"Duplicate order IDs: {(result.DuplicateOrderIds.Count == 0 ? "none" : string.Join(", ", result.DuplicateOrderIds))}");

        foreach (var failure in result.Failures)
        {
            Console.WriteLine($"ERROR: {failure}");
        }
    }
}
