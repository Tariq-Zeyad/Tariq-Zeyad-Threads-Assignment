
using System.Diagnostics;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Processors;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Retry;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Services;
using Phase1.Interfaces;
using Phase1.Repositories;
using Phase2.Pricing;
using Phase2.Strategies;
using Shared.Models;
using Shared.Storage;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Runners;

public class SyncVsAsyncRunner
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("          TASK 9.6: SYNC VS ASYNC");
        Console.WriteLine("======================================");

        await DataSeeder.ResetAsync(ct);

        List<Order> syncOrders =
            (await JsonFileStore.ReadAsync<Order>(
                DataFiles.IncomingOrders, ct))
            .Where(o => o.Id <= 10)
            .ToList();

        await JsonFileStore.WriteAsync(
            DataFiles.Orders, syncOrders, ct);

        Console.WriteLine();
        Console.WriteLine("Running synchronous version...");

        int syncThreadCountBefore =
            Process.GetCurrentProcess().Threads.Count;

        int syncPoolBefore = ThreadPool.ThreadCount;

        var syncProcessor = new SyncOrderProcessor();

        var syncStopwatch = Stopwatch.StartNew();

        Thread[] threads = syncOrders
            .Select(order => new Thread(() =>
            {
                try
                {
                    syncProcessor.ProcessOrder(order);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Sync Order #{order.Id} failed: {ex.Message}");
                }
            }))
            .ToArray();

        foreach (Thread thread in threads)
        {
            thread.Start();
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        syncStopwatch.Stop();

        int syncThreadCountAfter =
            Process.GetCurrentProcess().Threads.Count;

        int syncPoolAfter = ThreadPool.ThreadCount;

        List<Order> syncResults =
            await JsonFileStore.ReadAsync<Order>(
                DataFiles.Orders, ct);

        PrintResults(
            "SYNC",
            syncStopwatch.Elapsed,
            syncResults.Where(o => o.Id <= 10).ToList(),
            syncThreadCountBefore,
            syncThreadCountAfter,
            syncPoolBefore,
            syncPoolAfter);

        Console.WriteLine();
        Console.WriteLine("Resetting data before async version...");

        await DataSeeder.ResetAsync(ct);

        List<Order> asyncOrders =
            (await JsonFileStore.ReadAsync<Order>(
                DataFiles.IncomingOrders, ct))
            .Where(o => o.Id <= 10)
            .ToList();

        await JsonFileStore.WriteAsync(
            DataFiles.Orders, asyncOrders, ct);

        IProductRepository productRepository =
            new ProductRepository();

        IOrderRepository orderRepository =
            new OrderRepository(failuresEnabled: false);

        ICustomerRepository customerRepository =
            new CustomerRepository();

        ICouponRepository couponRepository =
            new CouponRepository();

        var phase2Pricing =
            new global::Phase2.Services.PricingService(
                productRepository,
                customerRepository,
                couponRepository,
                new SubtotalStrategy(),
                new DiscountStrategy(),
                new ShippingStrategy(),
                Array.Empty<int>());

        var orderPricer = new OrderPricer(phase2Pricing);

        var phase3Pricing =
            new ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Services.PricingService(
                orderPricer,
                couponRepository);

        var customerService =
            new CustomerService(customerRepository);

        var inventoryService =
            new InventoryService(productRepository);

        var paymentGateway =
            new PaymentGateway(failuresEnabled: false);

        var emailService =
            new EmailService(failuresEnabled: false);

        var retryService =
            new EmailRetryService(emailService);

        var asyncProcessor =
            new OrderProcessor(
                customerService,
                inventoryService,
                phase3Pricing,
                paymentGateway,
                orderRepository,
                retryService);

        Console.WriteLine();
        Console.WriteLine("Running asynchronous version...");

        int asyncThreadCountBefore =
            Process.GetCurrentProcess().Threads.Count;

        int asyncPoolBefore = ThreadPool.ThreadCount;

        var asyncStopwatch = Stopwatch.StartNew();

        await Task.WhenAll(
            asyncOrders.Select(order =>
                asyncProcessor.ProcessOrderAsync(order, ct)));

        asyncStopwatch.Stop();

        int asyncThreadCountAfter =
            Process.GetCurrentProcess().Threads.Count;

        int asyncPoolAfter = ThreadPool.ThreadCount;

        List<Order> asyncResults =
            await JsonFileStore.ReadAsync<Order>(
                DataFiles.Orders, ct);

        PrintResults(
            "ASYNC",
            asyncStopwatch.Elapsed,
            asyncResults.Where(o => o.Id <= 10).ToList(),
            asyncThreadCountBefore,
            asyncThreadCountAfter,
            asyncPoolBefore,
            asyncPoolAfter);

        // Compare the business results of both implementations.
        CompareOrderResults(
            syncResults.Where(o => o.Id <= 10).ToList(),
            asyncResults.Where(o => o.Id <= 10).ToList());

        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("          PERFORMANCE COMPARISON");
        Console.WriteLine("======================================");

        Console.WriteLine(
            $"Sync elapsed:  {syncStopwatch.Elapsed.TotalSeconds:F2} seconds");

        Console.WriteLine(
            $"Async elapsed: {asyncStopwatch.Elapsed.TotalSeconds:F2} seconds");

        if (asyncStopwatch.Elapsed < syncStopwatch.Elapsed)
        {
            double improvement =
                (syncStopwatch.Elapsed.TotalMilliseconds -
                 asyncStopwatch.Elapsed.TotalMilliseconds) /
                syncStopwatch.Elapsed.TotalMilliseconds * 100;

            Console.WriteLine(
                $"Async was {improvement:F1}% faster.");
        }
        else
        {
            Console.WriteLine(
                "Async was not faster in this run; compare the measurements and workload.");
        }

        Console.WriteLine();
        Console.WriteLine("Task 9.6 finished.");
    }

    private static void PrintResults(
        string mode,
        TimeSpan elapsed,
        List<Order> orders,
        int threadCountBefore,
        int threadCountAfter,
        int poolBefore,
        int poolAfter)
    {
        Console.WriteLine();
        Console.WriteLine($"--- {mode} RESULTS ---");

        Console.WriteLine(
            $"Elapsed: {elapsed.TotalSeconds:F2} seconds");

        Console.WriteLine(
            $"Process threads before: {threadCountBefore}");

        Console.WriteLine(
            $"Process threads after:  {threadCountAfter}");

        Console.WriteLine(
            $"ThreadPool count before: {poolBefore}");

        Console.WriteLine(
            $"ThreadPool count after:  {poolAfter}");

        Console.WriteLine(
            $"Completed: {orders.Count(o => o.Status == OrderStatus.Completed)}");

        Console.WriteLine(
            $"Rejected:  {orders.Count(o => o.Status == OrderStatus.Rejected)}");

        Console.WriteLine();

        Console.WriteLine(
            $"{"ID",-6} {"Status",-16} {"Total",-12} Failure reason");

        foreach (Order order in orders.OrderBy(o => o.Id))
        {
            Console.WriteLine(
                $"{order.Id,-6} " +
                $"{order.Status,-16} " +
                $"{order.TotalAmount,-12:F2} " +
                $"{order.FailureReason ?? "-"}");
        }
    }

    private static void CompareOrderResults(
        List<Order> syncOrders,
        List<Order> asyncOrders)
    {
        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("          RESULT VALIDATION");
        Console.WriteLine("======================================");

        bool allMatch = true;

        foreach (Order syncOrder in syncOrders.OrderBy(o => o.Id))
        {
            Order? asyncOrder =
                asyncOrders.FirstOrDefault(o => o.Id == syncOrder.Id);

            if (asyncOrder is null)
            {
                Console.WriteLine(
                    $"Order #{syncOrder.Id}: missing from ASYNC results.");

                allMatch = false;
                continue;
            }

            bool statusMatches =
                syncOrder.Status == asyncOrder.Status;

            bool totalMatches =
                Math.Abs(
                    syncOrder.TotalAmount - asyncOrder.TotalAmount) < 0.01m;

            Console.WriteLine(
                $"Order #{syncOrder.Id}: " +
                $"StatusMatch={statusMatches}, " +
                $"TotalMatch={totalMatches}");

            if (!statusMatches || !totalMatches)
            {
                allMatch = false;
            }
        }

        if (syncOrders.Count != asyncOrders.Count)
        {
            Console.WriteLine(
                $"Order count mismatch: SYNC={syncOrders.Count}, " +
                $"ASYNC={asyncOrders.Count}");

            allMatch = false;
        }

        Console.WriteLine();

        Console.WriteLine(
            allMatch
                ? "Validation passed: statuses and totals match."
                : "Validation failed: differences were detected.");
    }
}
