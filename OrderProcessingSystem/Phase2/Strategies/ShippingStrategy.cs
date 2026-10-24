using Phase2.Strategies.Interfaces;

namespace Phase2.Strategies;

public class ShippingStrategy : IShippingStrategy
{
    private const decimal FreeShippingMinimum = 1000m;
    private const decimal BaseShippingCost = 5.00m;
    private const decimal CostPerItem = 1.50m;
    private const decimal CostPerKilogram = 2.00m;
    private const int MoneyDecimalPlaces = 2;

    public decimal Calculate(
        decimal subTotal,
        int totalQuantity,
        decimal totalWeight)
    {
        if (subTotal >= FreeShippingMinimum)
        {
            return 0m;
        }

        decimal shippingCost =
            BaseShippingCost +
            CostPerItem * totalQuantity +
            CostPerKilogram * totalWeight;

        return Math.Round(
            shippingCost,
            MoneyDecimalPlaces,
            MidpointRounding.AwayFromZero);
    }
}
