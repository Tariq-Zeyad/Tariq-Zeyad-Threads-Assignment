using Phase2.Strategies.Interfaces;
using Shared.Models;

namespace Phase2.Strategies;

public class SubtotalStrategy : ISubtotalStrategy
{
    private const int MoneyDecimalPlaces = 2;

    public decimal Calculate(Order order)
    {
        decimal subTotal = order.Items.Sum(
            item => item.UnitPrice * item.Quantity);

        return Math.Round(
            subTotal,
            MoneyDecimalPlaces,
            MidpointRounding.AwayFromZero);
    }
}
