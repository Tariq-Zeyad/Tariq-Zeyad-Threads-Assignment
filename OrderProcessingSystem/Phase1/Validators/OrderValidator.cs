
using Phase1.Interfaces;
using Shared.Logging;
using Shared.Models;

namespace Phase1.Validators;

public class OrderValidator
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly IOrderRepository _orderRepository;

    public OrderValidator(
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        ICouponRepository couponRepository,
        IOrderRepository orderRepository)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _couponRepository = couponRepository;
        _orderRepository = orderRepository;
    }

    public async Task ValidateOrderAsync(
        Order order,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        Log.Info(
            "OrderValidator",
            $"Validating Order #{order.Id}...");

        try
        {
            ct.ThrowIfCancellationRequested();

            var customer = await _customerRepository.GetByIdAsync(
                order.CustomerId, ct);

            if (customer is null)
            {
                await RejectAsync(
                    order,
                    $"Customer #{order.CustomerId} not found",
                    ct);
                return;
            }

            if (!customer.IsActive)
            {
                await RejectAsync(
                    order,
                    "Customer is inactive",
                    ct);
                return;
            }

            foreach (OrderItem item in order.Items)
            {
                ct.ThrowIfCancellationRequested();

                var product = await _productRepository.GetByIdAsync(
                    item.ProductId, ct);

                if (product is null)
                {
                    await RejectAsync(
                        order,
                        $"Product #{item.ProductId} not found",
                        ct);
                    return;
                }

                if (product.StockQuantity < item.Quantity)
                {
                    await RejectAsync(
                        order,
                        $"Insufficient stock for '{product.Name}'",
                        ct);
                    return;
                }
            }

            if (order.CouponCode is not null)
            {
                var coupon = await _couponRepository.GetByCodeAsync(
                    order.CouponCode, ct);

                if (coupon is null)
                {
                    await RejectAsync(
                        order,
                        $"Coupon '{order.CouponCode}' does not exist",
                        ct);
                    return;
                }

                if (coupon.ExpiryDate.Date < DateTime.Today)
                {
                    await RejectAsync(
                        order,
                        $"Coupon '{order.CouponCode}' has expired",
                        ct);
                    return;
                }
            }

            // All checks passed.
            order.Status = OrderStatus.Validated;
            order.FailureReason = null;

            await _orderRepository.SaveAsync(order, ct);

            Log.Success(
                "OrderValidator",
                $"Order #{order.Id} validated successfully.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    private async Task RejectAsync(
        Order order,
        string reason,
        CancellationToken ct)
    {
        order.Status = OrderStatus.Rejected;
        order.FailureReason = reason;

        Log.Error(
            "OrderValidator",
            $"Order #{order.Id} rejected: {reason}");

        await _orderRepository.SaveAsync(order, ct);
    }
}
