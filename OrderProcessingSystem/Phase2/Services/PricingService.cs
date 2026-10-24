using Phase1.Interfaces;
using Shared.Exceptions;
using Phase2.Strategies.Interfaces;
using Shared.Models;
using Shared.Storage;

namespace Phase2.Services;

public class PricingService
{
    private const int ConnectionDelayMilliseconds = 200;
    private const int MinimumQueryDelayMilliseconds = 300;
    private const int MaximumQueryDelayMilliseconds = 600;
    private const int ShippingHandshakeDelayMilliseconds = 200;

    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly ISubtotalStrategy _subtotalStrategy;
    private readonly IDiscountStrategy _discountStrategy;
    private readonly IShippingStrategy _shippingStrategy;
    private readonly HashSet<int> _failShippingForOrderIds;

    public PricingService(
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        ICouponRepository couponRepository,
        ISubtotalStrategy subtotalStrategy,
        IDiscountStrategy discountStrategy,
        IShippingStrategy shippingStrategy,
        IEnumerable<int> failShippingForOrderIds)
    {
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _couponRepository = couponRepository;
        _subtotalStrategy = subtotalStrategy;
        _discountStrategy = discountStrategy;
        _shippingStrategy = shippingStrategy;
        _failShippingForOrderIds = failShippingForOrderIds.ToHashSet();
    }

    public async Task<decimal> CalculateSubTotalAsync(
        Order order,
        CancellationToken ct = default)
    {
        await SimulateQueryAsync(ct);

        List<Product> products =
            await JsonFileStore.ReadAsync<Product>(DataFiles.Products, ct);

        foreach (OrderItem item in order.Items)
        {
            Product product = products.First(
                product => product.Id == item.ProductId);

            item.UnitPrice = product.Price;
        }

        return _subtotalStrategy.Calculate(order);
    }

    public async Task<decimal> GetDiscountAsync(
        Order order,
        CancellationToken ct = default)
    {
        await SimulateQueryAsync(ct);

        Customer? customer =
            await _customerRepository.GetByIdAsync(order.CustomerId, ct);

        if (customer is null)
        {
            throw new InvalidOperationException(
                $"Customer #{order.CustomerId} was not found.");
        }

        Coupon? coupon = null;

        if (!string.IsNullOrWhiteSpace(order.CouponCode))
        {
            coupon = await _couponRepository.GetByCodeAsync(
                order.CouponCode,
                ct);

            if (coupon is not null && coupon.ExpiryDate.Date < DateTime.Today)
            {
                coupon = null;
            }
        }

        return _discountStrategy.Calculate(
            order.SubTotal,
            customer,
            coupon);
    }

    public async Task<decimal> CalculateShippingCostAsync(
        Order order,
        CancellationToken ct = default)
    {
        await Task.Delay(ShippingHandshakeDelayMilliseconds, ct);

        if (_failShippingForOrderIds.Contains(order.Id))
        {
            throw new ShippingProviderUnavailableException(
                "Shipping provider unavailable");
        }

        await SimulateQueryAsync(ct);

        List<Product> products =
            await JsonFileStore.ReadAsync<Product>(DataFiles.Products, ct);

        int totalQuantity = 0;
        decimal totalWeight = 0m;

        foreach (OrderItem item in order.Items)
        {
            Product product = products.First(
                product => product.Id == item.ProductId);

            totalQuantity += item.Quantity;
            totalWeight += product.WeightKg * item.Quantity;
        }

        return _shippingStrategy.Calculate(
            order.SubTotal,
            totalQuantity,
            totalWeight);
    }

    private static async Task SimulateQueryAsync(
        CancellationToken ct)
    {
        await Task.Delay(ConnectionDelayMilliseconds, ct);

        await Task.Delay(
            Random.Shared.Next(
                MinimumQueryDelayMilliseconds,
                MaximumQueryDelayMilliseconds + 1),
            ct);
    }
}
