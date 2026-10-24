
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Processors;
using ECommerceOrderProcessing.OrderProcessingSystem.Shared.Models;
using Shared.Models;
using Shared.Storage;
using System.Diagnostics;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Runners;

public class Phase3Runner
{
    private readonly OrderProcessor _orderProcessor;

    public Phase3Runner(OrderProcessor orderProcessor)
    {
        _orderProcessor = orderProcessor;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("       PHASE 3 - CHECKOUT WORKFLOW");
        Console.WriteLine("======================================");
        Console.WriteLine();

        await DataSeeder.ResetAsync(ct);

        List<Order> orders =
            await JsonFileStore.ReadAsync<Order>(
                DataFiles.IncomingOrders, ct);

        if (orders.Count == 0)
        {
            Console.WriteLine("No incoming orders were found.");
            return;
        }

        await JsonFileStore.WriteAsync(
            DataFiles.Orders, orders, ct);

        Console.WriteLine($"Orders loaded: {orders.Count}");
        Console.WriteLine("Starting concurrent processing...");
        Console.WriteLine();

        var stopwatch = Stopwatch.StartNew();

        Task<bool>[] tasks = orders
            .Select(order => ProcessSafelyAsync(order, ct))
            .ToArray();

        bool[] results = await Task.WhenAll(tasks);

        stopwatch.Stop();

        List<Order> finalOrders =
            await JsonFileStore.ReadAsync<Order>(
                DataFiles.Orders, ct);

        int completed = finalOrders.Count(
            order => order.Status == OrderStatus.Completed);

        int rejected = finalOrders.Count(
            order => order.Status == OrderStatus.Rejected);

        int paymentFailed = finalOrders.Count(
            order => order.Status == OrderStatus.PaymentFailed);

        int failed = finalOrders.Count(
            order => order.Status == OrderStatus.Failed);

        decimal revenue = finalOrders
            .Where(order => order.Status == OrderStatus.Completed)
            .Sum(order => order.TotalAmount);

        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("             FINAL RESULTS");
        Console.WriteLine("======================================");
        Console.WriteLine($"Orders loaded:   {orders.Count}");
        Console.WriteLine($"Tasks succeeded: {results.Count(result => result)}");
        Console.WriteLine($"Completed:       {completed}");
        Console.WriteLine($"Rejected:        {rejected}");
        Console.WriteLine($"Payment failed:  {paymentFailed}");
        Console.WriteLine($"Failed:          {failed}");
        Console.WriteLine($"Total revenue:   {revenue:F2}");
        Console.WriteLine(
            $"Elapsed time:    {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine();

        Console.WriteLine(
            $"{"ID",-6} {"Customer",-10} {"Status",-16} {"Total",-12} Failure reason");

        Console.WriteLine(new string('-', 90));

        foreach (Order order in finalOrders.OrderBy(order => order.Id))
        {
            Console.WriteLine(
                $"{order.Id,-6} " +
                $"{order.CustomerId,-10} " +
                $"{order.Status,-16} " +
                $"{order.TotalAmount,-12:F2} " +
                $"{order.FailureReason ?? "-"}");
        }

        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("          FILE VALIDATION");
        Console.WriteLine("======================================");

        List<PaymentTransaction> payments =
            await JsonFileStore.ReadAsync<PaymentTransaction>(
                DataFiles.Payments, ct);

        List<EmailMessage> emails =
            await JsonFileStore.ReadAsync<EmailMessage>(
                DataFiles.Emails, ct);

        List<EmailMessage> retryEmails =
            await JsonFileStore.ReadAsync<EmailMessage>(
                DataFiles.EmailRetry, ct);

        Console.WriteLine($"Orders in orders.json:       {finalOrders.Count}");
        Console.WriteLine($"Payment records:              {payments.Count}");
        Console.WriteLine($"Captured payments:            {payments.Count(p => p.Status == PaymentStatus.Captured)}");
        Console.WriteLine($"Declined payments:            {payments.Count(p => p.Status == PaymentStatus.Declined)}");
        Console.WriteLine($"Refunded payments:            {payments.Count(p => p.Status == PaymentStatus.Refunded)}");
        Console.WriteLine($"Emails sent:                  {emails.Count}");
        Console.WriteLine($"Emails queued for retry:      {retryEmails.Count}");

        Console.WriteLine();
        Console.WriteLine("Phase 3 run finished.");
    }

    private async Task<bool> ProcessSafelyAsync(
        Order order,
        CancellationToken ct)
    {
        try
        {
            return await _orderProcessor.ProcessOrderAsync(order, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Order #{order.Id} terminated unexpectedly: {ex.Message}");

            return false;
        }
    }
}
