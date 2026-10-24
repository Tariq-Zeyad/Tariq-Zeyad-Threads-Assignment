
using Shared.Models;

namespace Phase1.Interfaces;

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllAsync(
        CancellationToken ct = default);

    Task<Customer?> GetByIdAsync(
        int customerId,
        CancellationToken ct = default);
}
