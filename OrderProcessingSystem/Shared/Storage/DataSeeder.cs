
using ECommerceOrderProcessing.OrderProcessingSystem.Shared.Models;
using Shared.Models;

namespace Shared.Storage;

public static class DataSeeder
{
    private static readonly string DataDirectory =
        Path.Combine(AppContext.BaseDirectory, "Data");

    public static async Task ResetAsync(
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (Directory.Exists(DataDirectory))
        {
            Directory.Delete(DataDirectory, recursive: true);
        }

        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(
            Path.Combine(DataDirectory, DataFiles.InvoicesFolder));
        Directory.CreateDirectory(
            Path.Combine(DataDirectory, DataFiles.ReportsFolder));

        await JsonFileStore.WriteAsync(
            DataFiles.Customers, GetCustomers(), ct);
        Console.WriteLine($"Created {DataFiles.Customers}");

        await JsonFileStore.WriteAsync(
            DataFiles.Products, GetProducts(), ct);
        Console.WriteLine($"Created {DataFiles.Products}");

        await JsonFileStore.WriteAsync(
            DataFiles.Coupons, GetCoupons(), ct);
        Console.WriteLine($"Created {DataFiles.Coupons}");

        await JsonFileStore.WriteAsync(
            DataFiles.IncomingOrders, GetIncomingOrders(), ct);
        Console.WriteLine($"Created {DataFiles.IncomingOrders}");

        await JsonFileStore.WriteAsync<Order>(
            DataFiles.Orders, new List<Order>(), ct);
        Console.WriteLine($"Created {DataFiles.Orders}");

        await JsonFileStore.WriteAsync<PaymentTransaction>(
            DataFiles.Payments, new List<PaymentTransaction>(), ct);
        Console.WriteLine($"Created {DataFiles.Payments}");

        await JsonFileStore.WriteAsync<EmailMessage>(
            DataFiles.Emails, new List<EmailMessage>(), ct);
        Console.WriteLine($"Created {DataFiles.Emails}");

        await JsonFileStore.WriteAsync<EmailMessage>(
            DataFiles.EmailRetry, new List<EmailMessage>(), ct);
        Console.WriteLine($"Created {DataFiles.EmailRetry}");

        Console.WriteLine("Data reset completed successfully.");
    }

    private static List<Customer> GetCustomers()
    {
        return new List<Customer>
        {
            new() { Id = 1, Name = "Ahmed Ali", Email = "ahmed.ali@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Gold },
            new() { Id = 2, Name = "Sara Mohamed", Email = "sara.mohamed@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Silver },
            new() { Id = 3, Name = "Omar Hassan", Email = "omar.hassan@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Bronze },
            new() { Id = 4, Name = "Mona Ibrahim", Email = "mona.ibrahim@example.com", IsActive = false, LoyaltyTier = LoyaltyTier.Silver },
            new() { Id = 5, Name = "Youssef Adel", Email = "youssef.adel@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Gold },
            new() { Id = 6, Name = "Nour Khaled", Email = "nour.khaled@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Bronze },
            new() { Id = 7, Name = "Karim Samir", Email = "karim.samir@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Silver },
            new() { Id = 8, Name = "Laila Mahmoud", Email = "laila.mahmoud@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Gold },
            new() { Id = 9, Name = "Hassan Tarek", Email = "hassan.tarek@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Bronze },
            new() { Id = 10, Name = "Dina Fathy", Email = "dina.fathy@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Silver }
        };
    }

    private static List<Product> GetProducts()
    {
        return new List<Product>
        {
            new() { Id = 1, Name = "Laptop", Price = 1200.00m, StockQuantity = 10, WeightKg = 2.2m },
            new() { Id = 2, Name = "Smartphone", Price = 800.00m, StockQuantity = 15, WeightKg = 0.4m },
            new() { Id = 3, Name = "Wireless Mouse", Price = 25.00m, StockQuantity = 2, WeightKg = 0.1m },
            new() { Id = 4, Name = "Mechanical Keyboard", Price = 90.00m, StockQuantity = 1, WeightKg = 0.9m },
            new() { Id = 5, Name = "USB-C Hub", Price = 45.00m, StockQuantity = 30, WeightKg = 0.2m },
            new() { Id = 6, Name = "Headphones", Price = 150.00m, StockQuantity = 12, WeightKg = 0.3m },
            new() { Id = 7, Name = "27\" Monitor", Price = 350.00m, StockQuantity = 6, WeightKg = 5.0m },
            new() { Id = 8, Name = "Webcam", Price = 70.00m, StockQuantity = 20, WeightKg = 0.2m }
        };
    }

    private static List<Coupon> GetCoupons()
    {
        DateTime today = DateTime.Today;

        return new List<Coupon>
        {
            new() { Code = "SAVE10", DiscountPercent = 10, ExpiryDate = today.AddDays(180) },
            new() { Code = "WELCOME15", DiscountPercent = 15, ExpiryDate = today.AddDays(90) },
            new() { Code = "FLASH20", DiscountPercent = 20, ExpiryDate = today.AddDays(7) },
            new() { Code = "OLD20", DiscountPercent = 20, ExpiryDate = today.AddDays(-30) }
        };
    }

    private static List<Order> GetIncomingOrders()
    {
        DateTime now = DateTime.Now;

        return new List<Order>
        {
            CreateOrder(1, 1, new[] { (1, 1), (3, 1) }, "SAVE10", now),
            CreateOrder(2, 2, new[] { (2, 1) }, null, now),
            CreateOrder(3, 3, new[] { (5, 2), (8, 1) }, "WELCOME15", now),
            CreateOrder(4, 4, new[] { (6, 1) }, null, now),
            CreateOrder(5, 5, new[] { (7, 1) }, null, now),
            CreateOrder(6, 6, new[] { (5, 1) }, "FLASH20", now),
            CreateOrder(7, 7, new[] { (6, 1), (8, 1) }, null, now),
            CreateOrder(8, 8, new[] { (1, 1) }, "SAVE10", now),
            CreateOrder(9, 9, new[] { (3, 5) }, null, now),
            CreateOrder(10, 10, new[] { (4, 1) }, null, now),
            CreateOrder(11, 1, new[] { (4, 1) }, null, now),
            CreateOrder(12, 2, new[] { (2, 1) }, "OLD20", now),
            CreateOrder(13, 3, new[] { (6, 2) }, null, now),
            CreateOrder(14, 5, new[] { (8, 2), (5, 1) }, "WELCOME15", now),
            CreateOrder(15, 99, new[] { (2, 1) }, null, now),
            CreateOrder(16, 6, new[] { (7, 1), (5, 1) }, null, now),
            CreateOrder(17, 7, new[] { (2, 1) }, "SAVE10", now),
            CreateOrder(18, 8, new[] { (6, 1) }, "FAKE50", now),
            CreateOrder(19, 9, new[] { (8, 1) }, null, now),
            CreateOrder(20, 10, new[] { (1, 1), (6, 1) }, "FLASH20", now)
        };
    }

    private static Order CreateOrder(
        int orderId,
        int customerId,
        (int ProductId, int Quantity)[] items,
        string? couponCode,
        DateTime createdAt)
    {
        return new Order
        {
            Id = orderId,
            CustomerId = customerId,
            Items = items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = 0m
            }).ToList(),
            CouponCode = couponCode,
            SubTotal = 0m,
            Discount = 0m,
            ShippingCost = 0m,
            TotalAmount = 0m,
            Status = OrderStatus.Pending,
            CreatedAt = createdAt,
            FailureReason = null,
            TransactionId = null
        };
    }
}
