
using System.Diagnostics;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Fraud;

public sealed class FraudScoringRunner
{
    private readonly FraudScoringService _service;

    public FraudScoringRunner(FraudScoringService service)
    {
        _service = service;
    }

    public Task RunAsync(CancellationToken ct = default)
    {
        Console.WriteLine("\n=== Task 10.4A: Fraud Scoring ===");
        Console.WriteLine($"Orders: {FraudScoringService.DefaultOrderCount:N0}");
        Console.WriteLine($"Logical processors: {Environment.ProcessorCount}");

        var stopwatch = Stopwatch.StartNew();
        var sequential = _service.ScoreSequential(cancellationToken: ct);
        stopwatch.Stop();
        var sequentialTime = stopwatch.Elapsed;

        Console.WriteLine(
            $"Sequential: {sequentialTime.TotalSeconds:F2}s; " +
            $"flagged={sequential.Count}");

        stopwatch.Restart();
        var parallel = _service.ScoreParallel(cancellationToken: ct);
        stopwatch.Stop();

        Console.WriteLine(
            $"Parallel: {stopwatch.Elapsed.TotalSeconds:F2}s; " +
            $"flagged={parallel.Count}");

        Console.WriteLine(
            $"Results identical: {sequential.SequenceEqual(parallel)}");

        Console.WriteLine(
            $"First flagged IDs: {string.Join(", ", parallel.Take(5))}");

        if (parallel.Count != 1005)
        {
            Console.WriteLine(
                $"WARNING: expected 1005 flagged orders, got {parallel.Count}.");
        }

        return Task.CompletedTask;
    }
}
