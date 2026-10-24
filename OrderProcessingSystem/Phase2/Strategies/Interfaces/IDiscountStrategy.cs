using Shared.Models;

namespace Phase2.Strategies.Interfaces;

public interface IDiscountStrategy
{
    decimal Calculate(
        decimal subTotal,
        Customer customer,
        Coupon? coupon);
}