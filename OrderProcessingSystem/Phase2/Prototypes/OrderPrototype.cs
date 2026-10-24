using Phase2.Prototypes.Interfaces;
using Shared.Models;

namespace Phase2.Prototypes;

public class OrderPrototype : IOrderPrototype
{
    public Order Clone(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new Order
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Items = order.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList(),
            CouponCode = order.CouponCode,
            SubTotal = order.SubTotal,
            Discount = order.Discount,
            ShippingCost = order.ShippingCost,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            FailureReason = order.FailureReason,
            TransactionId = order.TransactionId
        };
    }
}