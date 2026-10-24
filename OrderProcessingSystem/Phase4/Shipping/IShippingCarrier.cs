
namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Shipping;

public interface IShippingCarrier
{
    string Name { get; }

    Task<ShippingQuote> GetQuoteAsync(
        int orderId,
        decimal baseShippingCost,
        CancellationToken cancellationToken = default);
}
