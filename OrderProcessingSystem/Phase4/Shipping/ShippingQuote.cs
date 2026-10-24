
namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Shipping;

public sealed record ShippingQuote(
    string CarrierName,
    decimal Cost,
    bool IsFallback = false);
