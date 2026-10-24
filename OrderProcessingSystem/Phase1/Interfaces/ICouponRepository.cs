
using Shared.Models;

namespace Phase1.Interfaces;

public interface ICouponRepository
{
    Task<List<Coupon>> GetAllAsync(
        CancellationToken ct = default);

    Task<Coupon?> GetByCodeAsync(
        string code,
        CancellationToken ct = default);
}
