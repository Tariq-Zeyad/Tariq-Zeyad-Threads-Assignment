using Phase2.Services;
using Shared.Models;

namespace Phase2.Pricing;

public class OrderPricer
{
    private const int MoneyDecimalPlaces = 2;

    private readonly PricingService _pricingService;

    public OrderPricer(PricingService pricingService)
    {
        _pricingService = pricingService;
    }

    public async Task PriceOrderAsync(
        Order order,
        CancellationToken ct = default)
    {
        order.SubTotal = await _pricingService.CalculateSubTotalAsync(
            order,
            ct);

        Task<decimal> discountTask =
            _pricingService.GetDiscountAsync(order, ct);

        Task<decimal> shippingTask =
            _pricingService.CalculateShippingCostAsync(order, ct);

        await Task.WhenAll(discountTask, shippingTask);

        order.Discount = await discountTask;
        order.ShippingCost = await shippingTask;

        CalculateTotalAmount(order);
    }

    public async Task PriceOrderSequentialAsync(
        Order order,
        CancellationToken ct = default)
    {
        order.SubTotal = await _pricingService.CalculateSubTotalAsync(
            order,
            ct);

        order.Discount = await _pricingService.GetDiscountAsync(
            order,
            ct);

        order.ShippingCost =
            await _pricingService.CalculateShippingCostAsync(
                order,
                ct);

        CalculateTotalAmount(order);
    }

    private static void CalculateTotalAmount(Order order)
    {
        decimal totalAmount =
            order.SubTotal - order.Discount + order.ShippingCost;

        order.TotalAmount = Math.Round(
            totalAmount,
            MoneyDecimalPlaces,
            MidpointRounding.AwayFromZero);
    }
}
