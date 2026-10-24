
using System.Diagnostics;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Processors;
using Shared.Models;
using Shared.Storage;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Checkout;

public sealed class LiveCheckoutRunner
{
    private readonly OrderProcessor _orderProcessor;

    public LiveCheckoutRunner(OrderProcessor orderProcessor)
    {
        _orderProcessor = orderProcessor;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("\n=== Task 10.1: Live Checkout ===");

        await DataSeeder.ResetAsync(cancellationToken);

        var orders = await JsonFileStore.ReadAsync<Order>(
            DataFiles.IncomingOrders,
            cancellationToken);

        if (orders.Count == 0)
        {
            Console.WriteLine("No incoming orders found.");
            return;
        }

        var pending = new List<Task<CheckoutCompletion>>();

        foreach (var order in orders)
        {
            pending.Add(ProcessAndMeasureAsync(order, cancellationToken));
        }

        int finished = 0;
        var finalStatuses = new List<OrderStatus>();

        while (pending.Count > 0)
        {
            var completedTask = await Task.WhenAny(pending);
            pending.Remove(completedTask);

            var completion = await completedTask;
            finished++;

            finalStatuses.Add(completion.Status);

            Console.WriteLine(
                $"[{finished,2}/{orders.Count}] Order #{completion.OrderId} -> " +
                $"{completion.Status} after {completion.Elapsed.TotalSeconds:F2}s" +
                (string.IsNullOrWhiteSpace(completion.Error)
                    ? string.Empty
                    : $" | {completion.Error}"));
        }

        Console.WriteLine("\nCheckout summary:");
        Console.WriteLine($"Total orders: {orders.Count}");

        foreach (var status in Enum.GetValues<OrderStatus>())
        {
            Console.WriteLine(
                $"{status}: {finalStatuses.Count(s => s == status)}");
        }

        Console.WriteLine(
            $"All order tasks observed: {finished == orders.Count}");
    }

    private async Task<CheckoutCompletion> ProcessAndMeasureAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _orderProcessor.ProcessOrderAsync(order, cancellationToken);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new CheckoutCompletion(
                order.Id,
                order.Status,
                stopwatch.Elapsed,
                ex.Message);
        }

        stopwatch.Stop();

        return new CheckoutCompletion(
            order.Id,
            order.Status,
            stopwatch.Elapsed,
            order.FailureReason);
    }

    private sealed record CheckoutCompletion(
        int OrderId,
        OrderStatus Status,
        TimeSpan Elapsed,
        string? Error);
}
