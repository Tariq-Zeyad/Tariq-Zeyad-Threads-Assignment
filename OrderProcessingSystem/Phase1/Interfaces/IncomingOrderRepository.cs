using Shared.Models;

namespace Phase1.Interfaces;

public interface IIncomingOrderRepository
{
    Task<List<Order>> GetAllAsync(
        CancellationToken ct = default);
}
