using System.Collections.Concurrent;
using Phase1.Interfaces;
using Phase2.Invoices;
using Phase2.Pricing;
using Shared.Models;

namespace Phase2.Processors;

public class OrderProcessorV2
{
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly OrderPricer _orderPricer;
    private readonly InvoiceGenerator _invoiceGenerator;
    private readonly ConcurrentDictionary<int, List<OrderItem>> _reservedItems = new();

    public OrderProcessorV2(
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        OrderPricer orderPricer,
        InvoiceGenerator invoiceGenerator)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _orderPricer = orderPricer;
        _invoiceGenerator = invoiceGenerator;
    }

    public async Task<bool> ProcessValidatedOrderAsync(
        Order order,
        CancellationToken ct = default)
    {
        List<OrderItem> reservedItems = [];

        foreach (OrderItem item in order.Items)
        {
            bool reserved = await _productRepository.ReserveStockAsync(
                item.ProductId,
                item.Quantity,
                ct);

            if (!reserved)
            {
                await ReleaseReservedItemsAsync(reservedItems, ct);

                Product? product = await _productRepository.GetByIdAsync(
                    item.ProductId,
                    ct);

                string productName =
                    product?.Name ?? $"Product #{item.ProductId}";

                order.Status = OrderStatus.Rejected;
                order.FailureReason =
                    $"Insufficient stock for '{productName}'";

                await _orderRepository.SaveAsync(order, ct);

                return false;
            }

            reservedItems.Add(item);
        }

        _reservedItems[order.Id] = reservedItems;

        await _orderPricer.PriceOrderAsync(order, ct);
        await _invoiceGenerator.GenerateAsync(order, ct);

        order.Status = OrderStatus.Completed;
        order.FailureReason = null;

        await _orderRepository.SaveAsync(order, ct);

        _reservedItems.TryRemove(order.Id, out _);

        return true;
    }

    public async Task ReleaseOrderStockAsync(
        int orderId,
        CancellationToken ct = default)
    {
        if (_reservedItems.TryRemove(orderId, out List<OrderItem>? items))
        {
            await ReleaseReservedItemsAsync(items, ct);
        }
    }

    private async Task ReleaseReservedItemsAsync(
        IEnumerable<OrderItem> items,
        CancellationToken ct)
    {
        foreach (OrderItem item in items)
        {
            await _productRepository.ReleaseStockAsync(
                item.ProductId,
                item.Quantity,
                ct);
        }
    }
}