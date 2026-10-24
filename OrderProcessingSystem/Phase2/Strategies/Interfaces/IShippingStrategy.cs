using Shared.Models;

namespace Phase2.Strategies.Interfaces;

public interface IShippingStrategy
{
    decimal Calculate(
        decimal subTotal,
        int totalQuantity,
        decimal totalWeight);
}