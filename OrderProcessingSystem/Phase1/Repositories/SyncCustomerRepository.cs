
using Shared.Logging;
using Shared.Models;
using Shared.Storage;
using System.Threading;

namespace Phase1.Repositories;

public class SyncCustomerRepository
{
    private const string Source = "SyncCustomerRepository";

    public Customer? GetById(int customerId)
    {
        Log.Info(Source, "Opening connection...");
        Thread.Sleep(200);

        Log.Info(Source, $"Executing query for Customer #{customerId}...");
        Thread.Sleep(Random.Shared.Next(300, 601));

        List<Customer> customers =
            JsonFileStore.Read<Customer>(DataFiles.Customers);

        Customer? customer = customers.FirstOrDefault(
            c => c.Id == customerId);

        if (customer is null)
        {
            Log.Error(Source, $"Customer #{customerId} was not found.");
            return null;
        }

        Log.Success(Source, $"Customer #{customerId} found.");
        return customer;
    }
}
