
using Phase1.Interfaces;
using Shared.Logging;
using Shared.Models;

namespace Phase1.Services;

public class ProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<List<Product>> GetAllProductsAsync(
        CancellationToken ct = default)
    {
        Log.Info("ProductService", "Getting all products...");

        await Task.Delay(100, ct);

        List<Product> products =
            await _productRepository.GetAllAsync(ct);

        await Task.Delay(100, ct);

        Log.Success(
            "ProductService",
            $"Retrieved {products.Count} products.");

        return products;
    }

    public async Task<Product> GetProductByIdAsync(
        int productId,
        CancellationToken ct = default)
    {
        Log.Info(
            "ProductService",
            $"Getting product #{productId}...");

        await Task.Delay(100, ct);

        Product? product =
            await _productRepository.GetByIdAsync(productId, ct);

        await Task.Delay(100, ct);

        if (product is null)
        {
            throw new InvalidOperationException(
                $"Product #{productId} does not exist.");
        }

        Log.Success(
            "ProductService",
            $"Product found: {product.Name}.");

        return product;
    }

    public async Task<Product> GetAvailableProductAsync(
        int productId,
        int quantity,
        CancellationToken ct = default)
    {
        Log.Info(
            "ProductService",
            $"Checking availability of Product #{productId}...");

        await Task.Delay(100, ct);

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        Product? product =
            await _productRepository.GetByIdAsync(productId, ct);

        await Task.Delay(100, ct);

        if (product is null)
        {
            throw new InvalidOperationException(
                $"Product #{productId} does not exist.");
        }

        if (product.StockQuantity < quantity)
        {
            throw new InvalidOperationException(
                $"Insufficient stock for {product.Name}. " +
                $"Available: {product.StockQuantity}, requested: {quantity}.");
        }

        Log.Success(
            "ProductService",
            $"{product.Name} is available. Requested quantity: {quantity}.");

        return product;
    }
}
