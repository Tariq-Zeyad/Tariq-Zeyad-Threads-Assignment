
using Shared.Models;
using Shared.Storage;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Dashboard;

public sealed class CustomerDashboardService
{
    private readonly HashSet<int> _failPaymentsForCustomerIds;

    public CustomerDashboardService(
        IEnumerable<int>? failPaymentsForCustomerIds = null)
    {
        _failPaymentsForCustomerIds =
            (failPaymentsForCustomerIds ?? Array.Empty<int>()).ToHashSet();
    }

    public async Task<CustomerDashboard> LoadAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var profileTask = LoadProfileAsync(customerId, cancellationToken);
        var ordersTask = LoadOrdersAsync(customerId, cancellationToken);
        var paymentsTask = LoadPaymentsAsync(customerId, cancellationToken);
        var emailsTask = LoadEmailsAsync(customerId, cancellationToken);

        Task[] tasks =
        {
            profileTask,
            ordersTask,
            paymentsTask,
            emailsTask
        };

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
        }

        cancellationToken.ThrowIfCancellationRequested();

        var errors = new List<string>();
        AddError(profileTask, "Profile", errors);
        AddError(ordersTask, "Orders", errors);
        AddError(paymentsTask, "Payments", errors);
        AddError(emailsTask, "Emails", errors);

        return new CustomerDashboard
        {
            CustomerId = customerId,
            Profile = GetResult(profileTask),
            Orders = GetResult(ordersTask) ?? new List<Order>(),
            Payments = GetResult(paymentsTask) ?? new List<PaymentTransaction>(),
            Emails = GetResult(emailsTask) ?? new List<EmailMessage>(),
            Errors = errors
        };
    }

    private static async Task<Customer> LoadProfileAsync(
        int customerId,
        CancellationToken ct)
    {
        await Task.Delay(100, ct);
        var customers = await JsonFileStore.ReadAsync<Customer>(
            DataFiles.Customers, ct);

        return customers.FirstOrDefault(c => c.Id == customerId)
            ?? throw new InvalidOperationException(
                $"Customer #{customerId} not found.");
    }

    private static async Task<List<Order>> LoadOrdersAsync(
        int customerId,
        CancellationToken ct)
    {
        await Task.Delay(100, ct);
        var orders = await JsonFileStore.ReadAsync<Order>(
            DataFiles.Orders, ct);

        return orders.Where(o => o.CustomerId == customerId).ToList();
    }

    private async Task<List<PaymentTransaction>> LoadPaymentsAsync(
        int customerId,
        CancellationToken ct)
    {
        await Task.Delay(100, ct);

        if (_failPaymentsForCustomerIds.Contains(customerId))
        {
            throw new TimeoutException("Payments store timeout.");
        }

        var orders = await JsonFileStore.ReadAsync<Order>(
            DataFiles.Orders, ct);

        var orderIds = orders
            .Where(o => o.CustomerId == customerId)
            .Select(o => o.Id)
            .ToHashSet();

        var payments = await JsonFileStore.ReadAsync<PaymentTransaction>(
            DataFiles.Payments, ct);

        return payments.Where(p => orderIds.Contains(p.OrderId)).ToList();
    }

    private static async Task<List<EmailMessage>> LoadEmailsAsync(
        int customerId,
        CancellationToken ct)
    {
        await Task.Delay(100, ct);

        var orders = await JsonFileStore.ReadAsync<Order>(
            DataFiles.Orders, ct);

        var orderIds = orders
            .Where(o => o.CustomerId == customerId)
            .Select(o => o.Id)
            .ToHashSet();

        var emails = await JsonFileStore.ReadAsync<EmailMessage>(
            DataFiles.Emails, ct);

        return emails.Where(e => orderIds.Contains(e.OrderId)).ToList();
    }

    private static void AddError<T>(
        Task<T> task,
        string section,
        List<string> errors)
    {
        if (task.Exception is null)
        {
            return;
        }

        foreach (var exception in task.Exception.Flatten().InnerExceptions)
        {
            errors.Add($"{section}: {exception.Message}");
        }
    }

    private static T? GetResult<T>(Task<T> task)
    {
        return task.Status == TaskStatus.RanToCompletion
            ? task.Result
            : default;
    }
}
