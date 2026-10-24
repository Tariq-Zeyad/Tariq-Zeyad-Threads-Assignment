
using Phase1.Interfaces;
using Shared.Models;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Services;

public class InventoryService
{
    private readonly IProductRepository _productRepository;

    public InventoryService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<bool> ReserveStockAsync(Order order)
    {
        var reservedItems = new List<OrderItem>();

        foreach (var item in order.Items)
        {
            var reserved = await _productRepository.ReserveStockAsync(
                item.ProductId,
                item.Quantity);

            if (!reserved)
            {
                foreach (var reservedItem in reservedItems)
                {
                    await _productRepository.ReleaseStockAsync(
                        reservedItem.ProductId,
                        reservedItem.Quantity);
                }

                return false;
            }

            reservedItems.Add(item);
        }

        return true;
    }

    public async Task ReleaseStockAsync(Order order)
    {
        foreach (var item in order.Items)
        {
            await _productRepository.ReleaseStockAsync(
                item.ProductId,
                item.Quantity);
        }
    }
}
