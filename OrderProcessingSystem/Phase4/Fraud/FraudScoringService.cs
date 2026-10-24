
using System.Collections.Concurrent;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Fraud;

public sealed class FraudScoringService
{
    public const int DefaultOrderCount = 10_000;

    public static int CalculateScore(int orderId)
    {
        long hash = orderId;

        for (int i = 0; i < 100_000; i++)
        {
            hash = (hash * 31 + i) % 1_000_000_007;
        }

        return (int)(hash % 100);
    }

    public static bool IsFlagged(int orderId) =>
        CalculateScore(orderId) >= 90;

    public List<int> ScoreSequential(
        int orderCount = DefaultOrderCount,
        CancellationToken cancellationToken = default)
    {
        var flagged = new List<int>();

        for (int id = 1; id <= orderCount; id++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsFlagged(id))
            {
                flagged.Add(id);
            }
        }

        return flagged;
    }

    public List<int> ScoreParallel(
        int orderCount = DefaultOrderCount,
        CancellationToken cancellationToken = default)
    {
        var flagged = new ConcurrentBag<int>();

        Parallel.For(
            1,
            orderCount + 1,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            },
            orderId =>
            {
                if (IsFlagged(orderId))
                {
                    flagged.Add(orderId);
                }
            });

        return flagged.OrderBy(id => id).ToList();
    }
}
