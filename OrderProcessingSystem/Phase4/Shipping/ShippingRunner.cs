
using Shared.Models;
using Shared.Storage;
using Shared.Exceptions;
namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Shipping;

public sealed class ShippingRunner
{
    private readonly ShippingQuoteService _service;

    public ShippingRunner(ShippingQuoteService service)
    {
        _service = service;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        Console.WriteLine("\n=== Task 10.3: Shipping Quote Race ===");

        var orders = await JsonFileStore.ReadAsync<Order>(
            DataFiles.IncomingOrders, ct);

        if (orders.Count == 0)
        {
            Console.WriteLine("No incoming orders found.");
            return;
        }

        foreach (var order in orders.Take(6))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var quote = await _service.GetQuoteAsync(order, ct);

                Console.WriteLine(
                    $"Order #{order.Id}: {quote.CarrierName} -> {quote.Cost:F2}" +
                    (quote.IsFallback ? " (fallback)" : ""));
            }
            catch (ShippingProviderUnavailableException ex)
            {
                Console.WriteLine($"Order #{order.Id}: FAILED - {ex.Message}");
            }
        }
    }
}
