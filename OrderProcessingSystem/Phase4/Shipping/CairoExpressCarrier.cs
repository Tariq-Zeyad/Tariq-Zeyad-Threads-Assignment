
using Shared.Exceptions;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Shipping;

public sealed class CairoExpressCarrier : IShippingCarrier
{
    private readonly HashSet<int> _failForOrderIds;

    public CairoExpressCarrier(IEnumerable<int>? failForOrderIds = null)
    {
        _failForOrderIds = (failForOrderIds ?? Array.Empty<int>()).ToHashSet();
    }

    public string Name => "CairoExpress";

    public async Task<ShippingQuote> GetQuoteAsync(
        int orderId,
        decimal baseShippingCost,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(150, cancellationToken);

        if (orderId % 2 == 0 || _failForOrderIds.Contains(orderId))
        {
            throw new ShippingProviderUnavailableException(
                $"{Name} unavailable for Order #{orderId}.");
        }

        await Task.Delay(Random.Shared.Next(300, 2001), cancellationToken);

        return new ShippingQuote(
            Name,
            Math.Round(baseShippingCost * 0.95m, 2, MidpointRounding.AwayFromZero));
    }
}
