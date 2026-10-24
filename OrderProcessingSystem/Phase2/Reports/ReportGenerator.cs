using System.Text.Json;
using Phase1.Interfaces;
using Shared.Models;
using Shared.Storage;

namespace Phase2.Reports;

public class ReportGenerator
{
    private const int MoneyDecimalPlaces = 2;
    private const int Zero = 0;

    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    public ReportGenerator(
        IOrderRepository orderRepository,
        IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    public async Task<Report> GenerateAsync(
        CancellationToken ct = default)
    {
        List<Order> orders = await _orderRepository.GetAllAsync(ct);
        List<Product> products = await _productRepository.GetAllAsync(ct);

        List<Order> completedOrders = orders
            .Where(order => order.Status == OrderStatus.Completed)
            .ToList();

        decimal totalRevenue = completedOrders.Sum(
            order => order.TotalAmount);

        int completedCount = completedOrders.Count;

        int topSellingProductId = completedOrders
            .SelectMany(order => order.Items)
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .OrderByDescending(item => item.Quantity)
            .ThenBy(item => item.ProductId)
            .Select(item => item.ProductId)
            .FirstOrDefault();

        int topSellingQuantity = completedOrders
            .SelectMany(order => order.Items)
            .Where(item => item.ProductId == topSellingProductId)
            .Sum(item => item.Quantity);

        string topSellingProduct = products
            .FirstOrDefault(product => product.Id == topSellingProductId)
            ?.Name ?? string.Empty;

        Report report = new()
        {
            TotalOrders = orders.Count,
            TotalRevenue = Math.Round(
                totalRevenue,
                MoneyDecimalPlaces,
                MidpointRounding.AwayFromZero),
            CompletedCount = completedCount,
            RejectedCount = orders.Count(
                order => order.Status == OrderStatus.Rejected),
            FailedCount = orders.Count(
                order => order.Status == OrderStatus.Failed),
            PaymentFailedCount = orders.Count(
                order => order.Status == OrderStatus.PaymentFailed),
            TopSellingProduct = topSellingProduct,
            TopSellingQuantity = topSellingQuantity,
            AverageOrderValue = completedCount == Zero
                ? Zero
                : Math.Round(
                    totalRevenue / completedCount,
                    MoneyDecimalPlaces,
                    MidpointRounding.AwayFromZero),
            GeneratedAt = DateTime.Now
        };

        string reportDirectory = DataFiles.ReportsFolder;
        Directory.CreateDirectory(reportDirectory);

        string fileName = $"report_{report.GeneratedAt:yyyyMMdd_HHmmss}.json";
        string filePath = Path.Combine(reportDirectory, fileName);

        string json = JsonSerializer.Serialize(
            report,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        await File.WriteAllTextAsync(filePath, json, ct);

        return report;
    }
}