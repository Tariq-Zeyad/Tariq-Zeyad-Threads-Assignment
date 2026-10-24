
namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Dashboard;

public sealed class CustomerDashboardRunner
{
    private readonly CustomerDashboardService _service;

    public CustomerDashboardRunner(CustomerDashboardService service)
    {
        _service = service;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        Console.WriteLine("\n=== Task 10.2: Customer Dashboard ===");

        await PrintDashboardAsync(3, ct);
        await PrintDashboardAsync(99, ct);
    }

    private async Task PrintDashboardAsync(int customerId, CancellationToken ct)
    {
        Console.WriteLine($"\nLoading dashboard for Customer #{customerId}...");

        var dashboard = await _service.LoadAsync(customerId, ct);

        if (dashboard.Profile is not null)
        {
            Console.WriteLine(
                $"Profile: {dashboard.Profile.Name} ({dashboard.Profile.Email})");
        }

        Console.WriteLine($"Orders: {dashboard.Orders.Count}");
        Console.WriteLine($"Payments: {dashboard.Payments.Count}");
        Console.WriteLine($"Emails: {dashboard.Emails.Count}");

        foreach (var error in dashboard.Errors)
        {
            Console.WriteLine($"ERROR: {error}");
        }

        Console.WriteLine(
            dashboard.IsSuccessful
                ? "Dashboard loaded successfully."
                : $"Dashboard loaded with {dashboard.Errors.Count} error(s).");
    }
}
