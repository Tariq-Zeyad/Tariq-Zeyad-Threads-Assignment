
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Processors;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Services;
using Shared.Models;
using Shared.Storage;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Runners;

public class FireAndForgetRunner
{
    public async Task RunAsync(
        bool fixedVersion,
        CancellationToken ct = default)
    {
        await DataSeeder.ResetAsync(ct);

        List<Order> orders =
            await JsonFileStore.ReadAsync<Order>(
                DataFiles.IncomingOrders, ct);

        List<Customer> customers =
            await JsonFileStore.ReadAsync<Customer>(
                DataFiles.Customers, ct);

        Order? order = orders.FirstOrDefault();
        Customer? customer = order is null
            ? null
            : customers.FirstOrDefault(
                c => c.Id == order.CustomerId);

        if (order is null || customer is null)
        {
            Console.WriteLine(
                "Could not find a test order and its customer.");
            return;
        }

        order.Status = OrderStatus.Completed;
        order.TotalAmount = 100m;

        var emailService = new EmailService(
            failuresEnabled: false);

        var demo = new FireAndForgetDemo(emailService);

        Console.WriteLine(
            fixedVersion
                ? "=== FIXED: AWAIT ==="
                : "=== BROKEN: FIRE-AND-FORGET ===");

        if (fixedVersion)
        {
            await demo.RunFixedAsync(order, customer);
        }
        else
        {
            await demo.RunBrokenAsync(order, customer);
        }

        Console.WriteLine("Runner finished.");
    }
}
