
using System.Diagnostics;
using Phase1.Interfaces;
using Phase1.Validators;
using Phase2.Pricing;
using Phase2.Processors;
using Phase2.Prototypes.Interfaces;
using Phase2.Reports;
using Phase2.Runners;
using Shared.Models;
using Shared.Storage;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase2.Runners;

internal class Phase2Runner
{
    private readonly IOrderRepository _orderRepository;
    private readonly OrderPricer _orderPricer;
    private readonly OrderProcessorV2 _orderProcessor;
    private readonly OrderValidator _orderValidator;
    private readonly IOrderPrototype _orderPrototype;
    private readonly RaceConditionDemo _raceConditionDemo;
    private readonly ReportGenerator _reportGenerator;

    public Phase2Runner(
        IOrderRepository orderRepository,
        OrderPricer orderPricer,
        OrderProcessorV2 orderProcessor,
        OrderValidator orderValidator,
        IOrderPrototype orderPrototype,
        RaceConditionDemo raceConditionDemo,
        ReportGenerator reportGenerator)
    {
        _orderRepository = orderRepository;
        _orderPricer = orderPricer;
        _orderProcessor = orderProcessor;
        _orderValidator = orderValidator;
        _orderPrototype = orderPrototype;
        _raceConditionDemo = raceConditionDemo;
        _reportGenerator = reportGenerator;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        Console.WriteLine("Phase 2 started.");

        await DataSeeder.ResetAsync(ct);
        await _raceConditionDemo.RunAsync(ct);
        await DataSeeder.ResetAsync(ct);

        await TestPricingAsync(ct);

        List<Order> incomingOrders =
            await JsonFileStore.ReadAsync<Order>(
                DataFiles.IncomingOrders,
                ct);

        Stopwatch validationTimer = Stopwatch.StartNew();

        await Task.WhenAll(
            incomingOrders.Select(
                order => _orderValidator.ValidateOrderAsync(order, ct)));

        validationTimer.Stop();

        Console.WriteLine(
            $"Validation time: {validationTimer.ElapsedMilliseconds} ms");

        List<Order> orders = await _orderRepository.GetAllAsync(ct);

        Order orderToPrice = orders.First(order => order.Id == 1);
        Order concurrentPricingOrder = _orderPrototype.Clone(orderToPrice);
        Order sequentialPricingOrder = _orderPrototype.Clone(orderToPrice);

        Stopwatch pricingTimer = Stopwatch.StartNew();

        await _orderPricer.PriceOrderAsync(concurrentPricingOrder, ct);

        pricingTimer.Stop();

        Console.WriteLine(
            $"Concurrent pricing: {pricingTimer.ElapsedMilliseconds} ms");

        pricingTimer.Restart();

        await _orderPricer.PriceOrderSequentialAsync(
            sequentialPricingOrder,
            ct);

        pricingTimer.Stop();

        Console.WriteLine(
            $"Sequential pricing: {pricingTimer.ElapsedMilliseconds} ms");

        List<Order> validatedOrders = await _orderRepository.GetByStatusAsync(
            OrderStatus.Validated,
            ct);

        Stopwatch processingTimer = Stopwatch.StartNew();

        List<Task<bool>> processingTasks = validatedOrders
            .Select(order => _orderProcessor.ProcessValidatedOrderAsync(order, ct))
            .ToList();

        if (processingTasks.Count > 0)
        {
            Console.WriteLine(
                $"First task status: {processingTasks[0].Status}");
        }

        await Task.Delay(1000, ct);

        foreach (Task<bool> task in processingTasks)
        {
            Console.WriteLine($"Task status: {task.Status}");
        }

        for (int index = 0; index < processingTasks.Count; index++)
        {
            Order order = validatedOrders[index];

            try
            {
                await processingTasks[index];
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    $"Task status: {processingTasks[index].Status}");

                await _orderProcessor.ReleaseOrderStockAsync(order.Id, ct);

                order.Status = OrderStatus.Failed;
                order.FailureReason = exception.Message;

                await _orderRepository.SaveAsync(order, ct);

                Console.WriteLine(
                    $"Order #{order.Id} failed: {exception.Message}");
            }
        }

        processingTimer.Stop();

        Console.WriteLine();
        Console.WriteLine("Final order statuses:");

        List<Order> finalOrders = await _orderRepository.GetAllAsync(ct);

        foreach (Order order in finalOrders.OrderBy(order => order.Id))
        {
            Console.WriteLine($"Order #{order.Id}: {order.Status}");
        }

        Console.WriteLine($"Total orders in orders.json: {finalOrders.Count}");

        Console.WriteLine(
            $"Concurrent processing time: {processingTimer.ElapsedMilliseconds} ms");

        Console.WriteLine("Generating report...");

        Task<Report> reportTask = _reportGenerator.GenerateAsync(ct);
        Report report = await reportTask;

        Console.WriteLine($"Total orders: {report.TotalOrders}");
        Console.WriteLine($"Completed: {report.CompletedCount}");
        Console.WriteLine($"Rejected: {report.RejectedCount}");
        Console.WriteLine($"Failed: {report.FailedCount}");
        Console.WriteLine($"Payment failed: {report.PaymentFailedCount}");
        Console.WriteLine($"Total revenue: {report.TotalRevenue:F2}");
        Console.WriteLine($"Average order value: {report.AverageOrderValue:F2}");
        Console.WriteLine(
            $"Top-selling product: {report.TopSellingProduct} ({report.TopSellingQuantity})");

        Console.WriteLine("Phase 2 finished.");
    }

    private async Task TestPricingAsync(CancellationToken ct)
    {
        List<Order> orders = await _orderRepository.GetAllAsync(ct);

        foreach (int orderId in new[] { 1, 2 })
        {
            Order? order = orders.FirstOrDefault(
                order => order.Id == orderId);

            if (order is null)
            {
                Console.WriteLine($"Order #{orderId} not found.");
                continue;
            }

            Order testOrder = _orderPrototype.Clone(order);

            await _orderPricer.PriceOrderAsync(testOrder, ct);

            Console.WriteLine();
            Console.WriteLine($"Pricing results for Order #{orderId}");
            Console.WriteLine($"SubTotal: {testOrder.SubTotal:F2}");
            Console.WriteLine($"Discount: {testOrder.Discount:F2}");
            Console.WriteLine($"Shipping: {testOrder.ShippingCost:F2}");
            Console.WriteLine($"Total: {testOrder.TotalAmount:F2}");

            bool isCorrect = orderId switch
            {
                1 => testOrder.SubTotal == 1225.00m
                     && testOrder.Discount == 245.00m
                     && testOrder.ShippingCost == 0.00m
                     && testOrder.TotalAmount == 980.00m,

                2 => testOrder.SubTotal == 800.00m
                     && testOrder.Discount == 40.00m
                     && testOrder.ShippingCost == 7.30m
                     && testOrder.TotalAmount == 767.30m,

                _ => false
            };

            Console.WriteLine(
                $"Pricing test: {(isCorrect ? "PASS" : "FAIL")}");
        }

        Console.WriteLine();
    }
}
