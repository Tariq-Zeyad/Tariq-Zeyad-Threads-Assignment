
using Phase1.Interfaces;
using Phase2.Pricing;
using Shared.Exceptions;
using Shared.Models;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Services;

public class PricingService
{
    private readonly OrderPricer _orderPricer;
    private readonly ICouponRepository _couponRepository;

    public PricingService(
        OrderPricer orderPricer,
        ICouponRepository couponRepository)
    {
        _orderPricer = orderPricer;
        _couponRepository = couponRepository;
    }

    public async Task<decimal> CalculateTotalAsync(
        Order order,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(order.CouponCode))
        {
            var coupon = await _couponRepository.GetByCodeAsync(
                order.CouponCode,
                ct);

            if (coupon is null)
            {
                throw new InvalidCouponException(
                    $"Coupon '{order.CouponCode}' was not found.");
            }

            if (coupon.ExpiryDate.Date < DateTime.Today)
            {
                throw new InvalidCouponException(
                    $"Coupon '{order.CouponCode}' has expired.");
            }
        }

        await _orderPricer.PriceOrderAsync(order, ct);

        return order.TotalAmount;
    }
}
