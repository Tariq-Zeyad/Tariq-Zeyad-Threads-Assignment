
using Shared.Exceptions;
using Shared.Models;
using Shared.Storage;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Shipping;

public sealed class ShippingQuoteService
{
    private static readonly TimeSpan QuoteTimeout =
        TimeSpan.FromMilliseconds(1200);

    private readonly IShippingCarrier[] _carriers;

    public ShippingQuoteService(IEnumerable<IShippingCarrier> carriers)
    {
        ArgumentNullException.ThrowIfNull(carriers);

        _carriers = carriers.ToArray();

        if (_carriers.Length == 0)
        {
            throw new ArgumentException(
                "At least one shipping carrier is required.",
                nameof(carriers));
        }
    }

    public async Task<ShippingQuote> GetQuoteAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        cancellationToken.ThrowIfCancellationRequested();

        var products = await JsonFileStore.ReadAsync<Product>(
            DataFiles.Products,
            cancellationToken);

        var productsById = products.ToDictionary(product => product.Id);

        int totalQuantity = order.Items.Sum(item => item.Quantity);

        decimal totalWeight = order.Items.Sum(item =>
        {
            if (!productsById.TryGetValue(item.ProductId, out var product))
            {
                throw new InvalidOperationException(
                    $"Product #{item.ProductId} not found for shipping.");
            }

            return product.WeightKg * item.Quantity;
        });

        decimal baseCost = order.SubTotal >= 1000m
            ? 0m
            : Math.Round(
                5.00m + 1.50m * totalQuantity + 2.00m * totalWeight,
                2,
                MidpointRounding.AwayFromZero);

        using var raceCts =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var pending = _carriers
            .Select(carrier => carrier.GetQuoteAsync(
                order.Id,
                baseCost,
                raceCts.Token))
            .ToList();

        var timeoutTask = Task.Delay(QuoteTimeout, cancellationToken);
        var errors = new List<Exception>();

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Task completed = await Task.WhenAny(
                pending.Cast<Task>().Append(timeoutTask));

            if (completed == timeoutTask)
            {
                if (pending.All(task => task.IsCompleted))
                {
                    break;
                }

                raceCts.Cancel();
                _ = ObserveRemainingTasksAsync(pending);

                return new ShippingQuote("Flat-rate", 25.00m, true);
            }

            var quoteTask = (Task<ShippingQuote>)completed;
            pending.Remove(quoteTask);

            try
            {
                var quote = await quoteTask;

                raceCts.Cancel();
                _ = ObserveRemainingTasksAsync(pending);

                return quote;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        }

        raceCts.Cancel();
        _ = ObserveRemainingTasksAsync(pending);

        if (errors.Count == _carriers.Length)
        {
            throw new ShippingProviderUnavailableException(
                "All carriers failed.");
        }

        return new ShippingQuote("Flat-rate", 25.00m, true);
    }

    private static async Task ObserveRemainingTasksAsync(
        IEnumerable<Task<ShippingQuote>> tasks)
    {
        foreach (var task in tasks)
        {
            try
            {
                await task;
            }
            catch
            {
                // Expected when losing carrier tasks are cancelled.
            }
        }
    }
}
