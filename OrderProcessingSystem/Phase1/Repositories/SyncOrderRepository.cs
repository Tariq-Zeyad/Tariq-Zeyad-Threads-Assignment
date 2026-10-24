
using Shared.Logging;
using Shared.Models;
using Shared.Storage;

namespace Phase1.Repositories;

public class SyncOrderRepository
{
    private const string Source = "SyncOrderRepository";

    public void Save(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        Log.Info(Source, "Opening connection...");
        Thread.Sleep(200);

        Log.Info(Source, "Beginning transaction...");
        Thread.Sleep(100);

        Log.Info(Source, $"Saving Order #{order.Id}...");
        Thread.Sleep(400);

        List<Order> orders = JsonFileStore.Read<Order>(
            DataFiles.Orders);

        int index = orders.FindIndex(
            existing => existing.Id == order.Id);

        if (index >= 0)
        {
            orders[index] = order;
        }
        else
        {
            orders.Add(order);
        }

        JsonFileStore.Write(DataFiles.Orders, orders);

        Log.Info(Source, "Committing transaction...");
        Thread.Sleep(200);

        Log.Success(Source, $"Order #{order.Id} saved.");
    }
}
