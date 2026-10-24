
using Phase1.Interfaces;
using Phase1.Repositories;
using Phase1.Validators;
using Shared.Logging;
using Shared.Models;
using Shared.Storage;
using System.Diagnostics;

namespace Phase1.Runners;

public class Phase1Runner
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        Log.Info("Phase1Runner", "Resetting test data...");
        await DataSeeder.ResetAsync(ct);

        IIncomingOrderRepository incomingRepository =
            new IncomingOrderRepository();

        Log.Info("Phase1Runner", "Loading incoming orders...");

        List<Order> syncOrders =
            await incomingRepository.GetAllAsync(ct);

        Console.WriteLine($"Incoming orders loaded: {syncOrders.Count}");

        var syncValidator = new SyncOrderValidator(
            new SyncCustomerRepository(),
            new SyncProductRepository(),
            new SyncCouponRepository());

        Log.Info("Phase1Runner", "Starting synchronous validation...");

        Stopwatch syncWatch = Stopwatch.StartNew();

        foreach (Order order in syncOrders)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                syncValidator.ValidateOrder(order);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(
                    "Phase1Runner",
                    $"Sync Order #{order.Id}: {ex.Message}");
            }
        }

        syncWatch.Stop();

        Console.WriteLine(
            $"Sync validation finished in {syncWatch.Elapsed.TotalSeconds:F1} seconds.");

        Log.Info(
            "Phase1Runner",
            "Resetting test data before asynchronous validation...");

        await DataSeeder.ResetAsync(ct);

        ICustomerRepository customerRepository =
            new CustomerRepository();

        IProductRepository productRepository =
            new ProductRepository();

        ICouponRepository couponRepository =
            new CouponRepository();

        IOrderRepository orderRepository =
            new OrderRepository();

        incomingRepository = new IncomingOrderRepository();

        var asyncValidator = new OrderValidator(
            customerRepository,
            productRepository,
            couponRepository,
            orderRepository);

        List<Order> asyncOrders =
            await incomingRepository.GetAllAsync(ct);

        Log.Info("Phase1Runner", "Starting asynchronous validation...");

        Stopwatch asyncWatch = Stopwatch.StartNew();

        foreach (Order order in asyncOrders)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await asyncValidator.ValidateOrderAsync(order, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(
                    "Phase1Runner",
                    $"Async Order #{order.Id}: {ex.Message}");
            }
        }

        asyncWatch.Stop();

        List<Order> validatedOrders =
            await orderRepository.GetByStatusAsync(
                OrderStatus.Validated, ct);

        List<Order> rejectedOrders =
            await orderRepository.GetByStatusAsync(
                OrderStatus.Rejected, ct);

        List<Order> savedOrders =
            await orderRepository.GetAllAsync(ct);

        Console.WriteLine();
        Console.WriteLine("===== Validation Summary =====");
        Console.WriteLine($"Total Orders: {asyncOrders.Count}");
        Console.WriteLine($"Validated:    {validatedOrders.Count}");
        Console.WriteLine($"Rejected:     {rejectedOrders.Count}");

        foreach (Order order in rejectedOrders.OrderBy(o => o.Id))
        {
            Console.WriteLine(
                $"  - Order #{order.Id}: {order.FailureReason}");
        }

        Console.WriteLine(
            $"Sync Time:  {syncWatch.Elapsed.TotalSeconds:F1} seconds");

        Console.WriteLine(
            $"Async Time: {asyncWatch.Elapsed.TotalSeconds:F1} seconds");

        Console.WriteLine(
            $"orders.json now contains {savedOrders.Count} records.");
    }
}
