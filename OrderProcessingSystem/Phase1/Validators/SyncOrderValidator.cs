
using Phase1.Repositories;
using Shared.Logging;
using Shared.Models;

namespace Phase1.Validators;

public class SyncOrderValidator
{
    private readonly SyncCustomerRepository _customerRepository;
    private readonly SyncProductRepository _productRepository;
    private readonly SyncCouponRepository _couponRepository;

    public SyncOrderValidator(
        SyncCustomerRepository customerRepository,
        SyncProductRepository productRepository,
        SyncCouponRepository couponRepository)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _couponRepository = couponRepository;
    }

    public void ValidateOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        Log.Info("SyncOrderValidator", $"Validating Order #{order.Id}...");

        Customer? customer =
            _customerRepository.GetById(order.CustomerId);

        if (customer is null)
        {
            Reject(order, $"Customer #{order.CustomerId} not found");
            return;
        }

        if (!customer.IsActive)
        {
            Reject(order, "Customer is inactive");
            return;
        }

        foreach (OrderItem item in order.Items)
        {
            Product? product =
                _productRepository.GetById(item.ProductId);

            if (product is null)
            {
                Reject(order, $"Product #{item.ProductId} not found");
                return;
            }

            if (product.StockQuantity < item.Quantity)
            {
                Reject(order, $"Insufficient stock for '{product.Name}'");
                return;
            }
        }

        if (order.CouponCode is not null)
        {
            Coupon? coupon =
                _couponRepository.GetByCode(order.CouponCode);

            if (coupon is null)
            {
                Reject(order, $"Coupon '{order.CouponCode}' does not exist");
                return;
            }

            if (coupon.ExpiryDate.Date < DateTime.Today)
            {
                Reject(order, $"Coupon '{order.CouponCode}' has expired");
                return;
            }
        }

        order.Status = OrderStatus.Validated;
        order.FailureReason = null;

        Log.Success(
            "SyncOrderValidator",
            $"Order #{order.Id} validated successfully.");
    }

    private void Reject(Order order, string reason)
    {
        order.Status = OrderStatus.Rejected;
        order.FailureReason = reason;

        Log.Error(
            "SyncOrderValidator",
            $"Order #{order.Id} rejected: {reason}");
    }
}
