
using System.Collections.Concurrent;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Interfaces;
using Shared.Models;
using Shared.Storage;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase4.EmailResend;

public sealed class EmailResendService
{
    private const int EmailCount = 30;
    private const int MaxConcurrency = 4;

    private readonly IEmailService _emailService;

    public EmailResendService(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<EmailResendResult> SendBatchAsync(
        CancellationToken cancellationToken = default)
    {
        var customer = new Customer
        {
            Id = -10,
            Name = "Phase 4 Test Customer",
            Email = "phase4-test@example.com",
            IsActive = true
        };

        var orders = Enumerable.Range(1001, EmailCount)
            .Select(id => new Order
            {
                Id = id,
                CustomerId = customer.Id,
                Items = new List<OrderItem>(),
                TotalAmount = 25m,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        int active = 0;
        int highestConcurrent = 0;
        var successfulOrderIds = new ConcurrentBag<int>();
        var failures = new ConcurrentBag<string>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxConcurrency,
            CancellationToken = cancellationToken
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await Parallel.ForEachAsync(
            orders,
            options,
            async (order, ct) =>
            {
                int nowActive = Interlocked.Increment(ref active);
                UpdateMaximum(ref highestConcurrent, nowActive);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);

                    await _emailService.SendConfirmationAsync(
                        order,
                        customer,
                        ct);

                    successfulOrderIds.Add(order.Id);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failures.Add($"Order #{order.Id}: {ex.Message}");
                }
                finally
                {
                    Interlocked.Decrement(ref active);
                }
            });

        stopwatch.Stop();

        var savedEmails = await JsonFileStore.ReadAsync<EmailMessage>(
            DataFiles.Emails, cancellationToken);

        var testEmails = savedEmails
            .Where(e => e.OrderId >= 1001 && e.OrderId <= 1030)
            .ToList();

        var duplicateIds = testEmails
            .GroupBy(e => e.OrderId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(id => id)
            .ToList();

        return new EmailResendResult(
            successfulOrderIds.Count,
            testEmails.Count,
            highestConcurrent,
            stopwatch.Elapsed,
            failures.OrderBy(message => message).ToList(),
            duplicateIds);
    }

    private static void UpdateMaximum(ref int maximum, int candidate)
    {
        int observed;

        do
        {
            observed = Volatile.Read(ref maximum);

            if (candidate <= observed)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(
                   ref maximum, candidate, observed) != observed);
    }
}

public sealed record EmailResendResult(
    int SuccessfulSends,
    int SavedEmailCount,
    int HighestConcurrent,
    TimeSpan Elapsed,
    List<string> Failures,
    List<int> DuplicateOrderIds);
