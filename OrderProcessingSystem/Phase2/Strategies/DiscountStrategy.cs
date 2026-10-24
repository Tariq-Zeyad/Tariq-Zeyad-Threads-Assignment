using ECommerceOrderProcessing.OrderProcessingSystem.Shared.Models;
using Phase2.Strategies.Interfaces;
using Shared.Models;

namespace Phase2.Strategies;

public class DiscountStrategy : IDiscountStrategy
{
    private const int BronzeDiscountPercent = 0;
    private const int SilverDiscountPercent = 5;
    private const int GoldDiscountPercent = 10;
    private const int MaximumDiscountPercent = 30;
    private const int MoneyDecimalPlaces = 2;
    private const int PercentageDivisor = 100;

    public decimal Calculate(
        decimal subTotal,
        Customer customer,
        Coupon? coupon)
    {
        int loyaltyDiscount = customer.LoyaltyTier switch
        {
            LoyaltyTier.Bronze => BronzeDiscountPercent,
            LoyaltyTier.Silver => SilverDiscountPercent,
            LoyaltyTier.Gold => GoldDiscountPercent,
            _ => BronzeDiscountPercent
        };

        int couponDiscount = coupon?.DiscountPercent ?? 0;

        int totalDiscountPercent = Math.Min(
            loyaltyDiscount + couponDiscount,
            MaximumDiscountPercent);

        decimal discount =
            subTotal * totalDiscountPercent / PercentageDivisor;

        return Math.Round(
            discount,
            MoneyDecimalPlaces,
            MidpointRounding.AwayFromZero);
    }
}
