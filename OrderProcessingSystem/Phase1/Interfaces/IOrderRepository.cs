using Shared.Models;

namespace Phase1.Interfaces;

public interface IOrderRepository
{
    Task SaveAsync(
        Order order,
        CancellationToken ct = default);

    Task<List<Order>> GetAllAsync(
        CancellationToken ct = default);

    Task<List<Order>> GetByStatusAsync(
        OrderStatus status,
        CancellationToken ct = default);
}
