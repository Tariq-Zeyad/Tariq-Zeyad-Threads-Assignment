
using ECommerceOrderProcessing.OrderProcessingSystem.Shared.Models;
using Shared.Models;
using Shared.Storage;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Processors;

public class SyncOrderProcessor
{
    private static readonly object StockLock = new();
    private static readonly object OrdersLock = new();
    private static readonly object PaymentsLock = new();
    private static readonly object EmailsLock = new();

    public bool ProcessOrder(Order order)
    {
        Customer? customer = null;
        bool stockReserved = false;

        try
        {
            Thread.Sleep(200);

            customer = JsonFileStore.Read<Customer>(
                DataFiles.Customers)
                .FirstOrDefault(c => c.Id == order.CustomerId);

            if (customer is null)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason =
                    $"Customer #{order.CustomerId} not found";

                SaveOrder(order);
                return false;
            }

            if (!customer.IsActive)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = "Customer is inactive";

                SaveOrder(order);
                return false;
            }

            Thread.Sleep(100);

            lock (StockLock)
            {
                List<Product> products =
                    JsonFileStore.Read<Product>(DataFiles.Products);

                foreach (OrderItem item in order.Items)
                {
                    Product? product = products.FirstOrDefault(
                        p => p.Id == item.ProductId);

                    if (product is null ||
                        product.StockQuantity < item.Quantity)
                    {
                        order.Status = OrderStatus.Rejected;
                        order.FailureReason =
                            $"Insufficient stock for '{product?.Name ?? $"Product #{item.ProductId}"}'";

                        SaveOrder(order);
                        return false;
                    }
                }

                foreach (OrderItem item in order.Items)
                {
                    Product product = products.First(
                        p => p.Id == item.ProductId);

                    product.StockQuantity -= item.Quantity;
                    item.UnitPrice = product.Price;
                }

                JsonFileStore.Write(DataFiles.Products, products);
                stockReserved = true;
            }

            Thread.Sleep(100);

            List<Coupon> coupons =
                JsonFileStore.Read<Coupon>(DataFiles.Coupons);

            Coupon? coupon = null;

            if (!string.IsNullOrWhiteSpace(order.CouponCode))
            {
                coupon = coupons.FirstOrDefault(
                    c => c.Code == order.CouponCode);

                if (coupon is null ||
                    coupon.ExpiryDate.Date < DateTime.Today)
                {
                    ReleaseStock(order);

                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = coupon is null
                        ? $"Coupon '{order.CouponCode}' was not found."
                        : $"Coupon '{order.CouponCode}' has expired.";

                    SaveOrder(order);
                    return false;
                }
            }

            order.SubTotal = Math.Round(
                order.Items.Sum(i => i.UnitPrice * i.Quantity),
                2,
                MidpointRounding.AwayFromZero);

            int loyaltyDiscount = customer.LoyaltyTier switch
            {
                LoyaltyTier.Bronze => 0,
                LoyaltyTier.Silver => 5,
                LoyaltyTier.Gold => 10,
                _ => 0
            };

            int couponDiscount = coupon?.DiscountPercent ?? 0;

            int discountPercent = Math.Min(
                loyaltyDiscount + couponDiscount, 30);

            order.Discount = Math.Round(
                order.SubTotal * discountPercent / 100m,
                2,
                MidpointRounding.AwayFromZero);

            int totalQuantity = order.Items.Sum(i => i.Quantity);

            List<Product> currentProducts =
                JsonFileStore.Read<Product>(DataFiles.Products);

            decimal totalWeight = order.Items.Sum(item =>
            {
                Product product = currentProducts.First(
                    p => p.Id == item.ProductId);

                return product.WeightKg * item.Quantity;
            });

            order.ShippingCost = order.SubTotal >= 1000m
                ? 0m
                : Math.Round(
                    5m + 1.5m * totalQuantity + 2m * totalWeight,
                    2,
                    MidpointRounding.AwayFromZero);

            order.TotalAmount =
                order.SubTotal - order.Discount + order.ShippingCost;

            Thread.Sleep(700);

            string transactionId =
                "TXN-" + Guid.NewGuid().ToString("N")[..12]
                    .ToUpperInvariant();

            var payment = new PaymentTransaction
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Status = PaymentStatus.Captured,
                TransactionId = transactionId,
                CreatedAt = DateTime.UtcNow
            };

            lock (PaymentsLock)
            {
                List<PaymentTransaction> payments =
                    JsonFileStore.Read<PaymentTransaction>(
                        DataFiles.Payments);

                payments.Add(payment);

                JsonFileStore.Write(DataFiles.Payments, payments);
            }

            order.TransactionId = transactionId;
            order.Status = OrderStatus.Completed;
            order.FailureReason = null;

            SaveOrder(order);

            Thread.Sleep(200);

            var email = new EmailMessage
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                To = customer.Email,
                Subject = $"Order #{order.Id} Confirmation",
                Body =
                    $"Your order #{order.Id} has been completed. Total: {order.TotalAmount:F2}",
                SentAt = DateTime.UtcNow,
                Attempts = 1,
                LastError = null
            };

            lock (EmailsLock)
            {
                List<EmailMessage> emails =
                    JsonFileStore.Read<EmailMessage>(DataFiles.Emails);

                emails.Add(email);

                JsonFileStore.Write(DataFiles.Emails, emails);
            }

            return true;
        }
        catch
        {
            if (stockReserved)
            {
                ReleaseStock(order);
            }

            throw;
        }
    }

    private static void SaveOrder(Order order)
    {
        Thread.Sleep(200);

        lock (OrdersLock)
        {
            List<Order> orders =
                JsonFileStore.Read<Order>(DataFiles.Orders);

            int index = orders.FindIndex(o => o.Id == order.Id);

            if (index >= 0)
            {
                orders[index] = order;
            }
            else
            {
                orders.Add(order);
            }

            JsonFileStore.Write(DataFiles.Orders, orders);
        }
    }

    private static void ReleaseStock(Order order)
    {
        lock (StockLock)
        {
            List<Product> products =
                JsonFileStore.Read<Product>(DataFiles.Products);

            foreach (OrderItem item in order.Items)
            {
                Product? product = products.FirstOrDefault(
                    p => p.Id == item.ProductId);

                if (product is not null)
                {
                    product.StockQuantity += item.Quantity;
                }
            }

            JsonFileStore.Write(DataFiles.Products, products);
        }
    }
}
