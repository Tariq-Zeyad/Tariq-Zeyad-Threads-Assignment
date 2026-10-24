using Shared.Models;

namespace Phase1.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync(
        CancellationToken ct = default);

    Task<Product?> GetByIdAsync(
        int productId,
        CancellationToken ct = default);

    Task<bool> ReserveStockAsync(
        int productId,
        int qty,
        CancellationToken ct = default);

    Task<bool> ReleaseStockAsync(
        int productId,
        int qty,
        CancellationToken ct = default);

    Task<bool> ReserveStockUnsafeAsync(
        int productId,
        int qty,
        CancellationToken ct = default);
}
