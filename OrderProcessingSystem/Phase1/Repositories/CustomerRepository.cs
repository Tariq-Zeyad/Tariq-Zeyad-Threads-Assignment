
using Shared.Logging;
using Shared.Models;
using Shared.Storage;
using Phase1.Interfaces;

namespace Phase1.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private const string Source = "CustomerRepository";

    public async Task<List<Customer>> GetAllAsync(
        CancellationToken ct = default)
    {
        Log.Info(Source, "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info(Source, "Executing query for all customers...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);

        List<Customer> customers =
            await JsonFileStore.ReadAsync<Customer>(
                DataFiles.Customers, ct);

        Log.Success(
            Source,
            $"Retrieved {customers.Count} customers.");

        return customers;
    }

    public async Task<Customer?> GetByIdAsync(
        int customerId,
        CancellationToken ct = default)
    {
        Log.Info(Source, "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info(
            Source,
            $"Executing query for Customer #{customerId}...");

        await Task.Delay(Random.Shared.Next(300, 601), ct);

        List<Customer> customers =
            await JsonFileStore.ReadAsync<Customer>(
                DataFiles.Customers, ct);

        Customer? customer = customers.FirstOrDefault(
            c => c.Id == customerId);

        if (customer is null)
        {
            Log.Error(
                Source,
                $"Customer #{customerId} was not found.");

            return null;
        }

        Log.Success(
            Source,
            $"Customer #{customerId} found ({customer.Name}).");

        return customer;
    }
}
