using Phase1.Interfaces;
using Shared.Logging;
using Shared.Models;
using Shared.Storage;

namespace Phase1.Repositories;

public class IncomingOrderRepository : IIncomingOrderRepository
{
    private const string Source = "IncomingOrderRepository";

    public async Task<List<Order>> GetAllAsync(
        CancellationToken ct = default)
    {
        Log.Info(Source, "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info(
            Source,
            "Executing query for incoming orders...");

        await Task.Delay(Random.Shared.Next(300, 601), ct);

        List<Order> orders =
            await JsonFileStore.ReadAsync<Order>(
                DataFiles.IncomingOrders, ct);

        Log.Success(
            Source,
            $"Retrieved {orders.Count} incoming orders.");

        return orders;
    }
}
