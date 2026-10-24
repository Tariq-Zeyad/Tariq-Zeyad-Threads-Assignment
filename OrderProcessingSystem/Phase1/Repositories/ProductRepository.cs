using Phase1.Interfaces;
using Shared.Logging;
using Shared.Models;
using Shared.Storage;

namespace Phase1.Repositories;

public class ProductRepository : IProductRepository
{
    private const string Source = "ProductRepository";

    private const int ConnectionDelayMilliseconds = 200;
    private const int TransactionDelayMilliseconds = 100;
    private const int UpdateDelayMilliseconds = 400;
    private const int CommitDelayMilliseconds = 200;
    private const int MinimumQueryDelayMilliseconds = 300;
    private const int MaximumQueryDelayMilliseconds = 600;

    private static readonly SemaphoreSlim StockLock = new(1, 1);

    public async Task<List<Product>> GetAllAsync(
        CancellationToken ct = default)
    {
        await SimulateQueryAsync("all products", ct);

        List<Product> products =
            await JsonFileStore.ReadAsync<Product>(
                DataFiles.Products, ct);

        Log.Success(Source, $"Retrieved {products.Count} products.");

        return products;
    }

    public async Task<Product?> GetByIdAsync(
        int productId,
        CancellationToken ct = default)
    {
        await SimulateQueryAsync($"Product #{productId}", ct);

        List<Product> products =
            await JsonFileStore.ReadAsync<Product>(
                DataFiles.Products, ct);

        Product? product = products.FirstOrDefault(
            p => p.Id == productId);

        if (product is null)
        {
            Log.Error(Source, $"Product #{productId} was not found.");
            return null;
        }

        Log.Success(
            Source,
            $"Product #{productId} found ({product.Name}).");

        return product;
    }

    public async Task<bool> ReserveStockAsync(
        int productId,
        int qty,
        CancellationToken ct = default)
    {
        ValidateQuantity(qty);

        Log.Info(Source, $"Reserving {qty} unit(s) of Product #{productId}.");

        await StockLock.WaitAsync(ct);

        try
        {
            await SimulateTransactionAsync(ct);

            List<Product> products =
                await JsonFileStore.ReadAsync<Product>(
                    DataFiles.Products, ct);

            Product? product = products.FirstOrDefault(
                p => p.Id == productId);

            if (product is null || product.StockQuantity < qty)
            {
                Log.Info(
                    Source,
                    $"Stock reservation failed for Product #{productId}.");

                return false;
            }

            product.StockQuantity -= qty;

            await Task.Delay(UpdateDelayMilliseconds, ct);

            await JsonFileStore.WriteAsync(
                DataFiles.Products, products, ct);

            await Task.Delay(CommitDelayMilliseconds, ct);

            Log.Success(
                Source,
                $"Reserved {qty} unit(s) of {product.Name}.");

            return true;
        }
        finally
        {
            StockLock.Release();
        }
    }

    public async Task<bool> ReleaseStockAsync(
        int productId,
        int qty,
        CancellationToken ct = default)
    {
        ValidateQuantity(qty);

        await StockLock.WaitAsync(ct);

        try
        {
            List<Product> products =
                await JsonFileStore.ReadAsync<Product>(
                    DataFiles.Products, ct);

            Product? product = products.FirstOrDefault(
                p => p.Id == productId);

            if (product is null)
            {
                Log.Error(
                    Source,
                    $"Product #{productId} was not found for stock release.");

                return false;
            }

            product.StockQuantity += qty;

            await JsonFileStore.WriteAsync(
                DataFiles.Products, products, ct);

            Log.Success(
                Source,
                $"Released {qty} unit(s) of {product.Name}.");

            return true;
        }
        finally
        {
            StockLock.Release();
        }
    }

    public async Task<bool> ReserveStockUnsafeAsync(
        int productId,
        int qty,
        CancellationToken ct = default)
    {
        ValidateQuantity(qty);

        await Task.Delay(ConnectionDelayMilliseconds, ct);
        await Task.Delay(TransactionDelayMilliseconds, ct);

        List<Product> products =
            await JsonFileStore.ReadAsync<Product>(
                DataFiles.Products, ct);

        Product? product = products.FirstOrDefault(
            p => p.Id == productId);

        if (product is null || product.StockQuantity < qty)
        {
            return false;
        }

        product.StockQuantity -= qty;

        await Task.Delay(UpdateDelayMilliseconds, ct);

        await JsonFileStore.WriteAsync(
            DataFiles.Products, products, ct);

        return true;
    }

    private static async Task SimulateQueryAsync(
        string queryDescription,
        CancellationToken ct)
    {
        Log.Info(Source, "Opening connection...");
        await Task.Delay(ConnectionDelayMilliseconds, ct);

        Log.Info(Source, $"Executing query for {queryDescription}...");
        await Task.Delay(
            Random.Shared.Next(
                MinimumQueryDelayMilliseconds,
                MaximumQueryDelayMilliseconds + 1),
            ct);
    }

    private static async Task SimulateTransactionAsync(
        CancellationToken ct)
    {
        Log.Info(Source, "Opening connection for stock reservation...");
        await Task.Delay(ConnectionDelayMilliseconds, ct);

        Log.Info(Source, "Beginning stock transaction...");
        await Task.Delay(TransactionDelayMilliseconds, ct);
    }

    private static void ValidateQuantity(int qty)
    {
        if (qty <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(qty),
                "Quantity must be greater than zero.");
        }
    }
}
