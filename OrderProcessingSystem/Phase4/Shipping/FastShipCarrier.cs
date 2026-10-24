
using Shared.Exceptions;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Shipping;

public sealed class FastShipCarrier : IShippingCarrier
{
    private readonly HashSet<int> _failForOrderIds;

    public FastShipCarrier(IEnumerable<int>? failForOrderIds = null)
    {
        _failForOrderIds = (failForOrderIds ?? Array.Empty<int>()).ToHashSet();
    }

    public string Name => "FastShip";

    public async Task<ShippingQuote> GetQuoteAsync(
        int orderId,
        decimal baseShippingCost,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(150, cancellationToken);

        if (_failForOrderIds.Contains(orderId))
        {
            throw new ShippingProviderUnavailableException(
                $"{Name} unavailable for Order #{orderId}.");
        }

        await Task.Delay(Random.Shared.Next(500, 1501), cancellationToken);

        return new ShippingQuote(
            Name,
            Math.Round(baseShippingCost * 1.00m, 2, MidpointRounding.AwayFromZero));
    }
}
