
using Shared.Logging;
using Shared.Models;
using Shared.Storage;
using System.Threading;

namespace Phase1.Repositories;

public class SyncProductRepository
{
    private const string Source = "SyncProductRepository";

    public Product? GetById(int productId)
    {
        Log.Info(Source, "Opening connection...");
        Thread.Sleep(200);

        Log.Info(Source, $"Executing query for Product #{productId}...");
        Thread.Sleep(Random.Shared.Next(300, 601));

        List<Product> products =
            JsonFileStore.Read<Product>(DataFiles.Products);

        Product? product = products.FirstOrDefault(
            p => p.Id == productId);

        if (product is null)
        {
            Log.Error(Source, $"Product #{productId} was not found.");
            return null;
        }

        Log.Success(Source, $"Product #{productId} found.");
        return product;
    }
}
