
using Phase1.Interfaces;
using Shared.Exceptions;
using Shared.Logging;
using Shared.Models;
using Shared.Storage;

namespace Phase1.Repositories;

public class OrderRepository : IOrderRepository
{
    private const string Source = "OrderRepository";

    private const int ConnectionDelayMilliseconds = 200;
    private const int TransactionDelayMilliseconds = 100;
    private const int UpdateDelayMilliseconds = 400;
    private const int CommitDelayMilliseconds = 200;
    private const int MinimumQueryDelayMilliseconds = 300;
    private const int MaximumQueryDelayMilliseconds = 600;

    private static readonly SemaphoreSlim SaveLock = new(1, 1);

    private readonly HashSet<int> _failSaveForOrderIds;
    private readonly bool _failuresEnabled;

    public OrderRepository(
        IEnumerable<int>? failSaveForOrderIds = null,
        bool failuresEnabled = true)
    {
        _failSaveForOrderIds = new HashSet<int>(
            failSaveForOrderIds ?? Array.Empty<int>());

        _failuresEnabled = failuresEnabled;
    }

    public async Task SaveAsync(
        Order order,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        await SaveLock.WaitAsync(ct);

        try
        {
            Log.Info(Source, "Opening connection...");
            await Task.Delay(ConnectionDelayMilliseconds, ct);

            Log.Info(Source, "Beginning transaction...");
            await Task.Delay(TransactionDelayMilliseconds, ct);

            Log.Info(Source, $"Saving Order #{order.Id}...");
            await Task.Delay(UpdateDelayMilliseconds, ct);

            if (_failuresEnabled &&
                order.Status == OrderStatus.Completed &&
                _failSaveForOrderIds.Contains(order.Id))
            {
                throw new DatabaseWriteException(
                    "Connection lost during commit");
            }

            List<Order> orders =
                await JsonFileStore.ReadAsync<Order>(
                    DataFiles.Orders,
                    ct);

            int existingIndex = orders.FindIndex(
                existing => existing.Id == order.Id);

            if (existingIndex >= 0)
            {
                orders[existingIndex] = order;
            }
            else
            {
                orders.Add(order);
            }

            await JsonFileStore.WriteAsync(
                DataFiles.Orders,
                orders,
                ct);

            Log.Info(Source, "Committing transaction...");
            await Task.Delay(CommitDelayMilliseconds, ct);

            Log.Success(Source, $"Order #{order.Id} saved.");
        }
        finally
        {
            SaveLock.Release();
        }
    }

    public async Task<List<Order>> GetAllAsync(
        CancellationToken ct = default)
    {
        int delay = Random.Shared.Next(
            MinimumQueryDelayMilliseconds,
            MaximumQueryDelayMilliseconds + 1);

        await Task.Delay(delay, ct);

        return await JsonFileStore.ReadAsync<Order>(
            DataFiles.Orders,
            ct);
    }

    public async Task<List<Order>> GetByStatusAsync(
        OrderStatus status,
        CancellationToken ct = default)
    {
        int delay = Random.Shared.Next(
            MinimumQueryDelayMilliseconds,
            MaximumQueryDelayMilliseconds + 1);

        await Task.Delay(delay, ct);

        List<Order> orders =
            await JsonFileStore.ReadAsync<Order>(
                DataFiles.Orders,
                ct);

        return orders
            .Where(order => order.Status == status)
            .ToList();
    }
}
