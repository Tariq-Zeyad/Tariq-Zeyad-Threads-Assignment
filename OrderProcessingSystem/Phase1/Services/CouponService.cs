
using Phase1.Interfaces;
using Shared.Logging;
using Shared.Models;

namespace Phase1.Services;

public class CouponService
{
    private readonly ICouponRepository _couponRepository;

    public CouponService(ICouponRepository couponRepository)
    {
        _couponRepository = couponRepository;
    }

    public async Task<Coupon> GetValidCouponAsync(
        string code,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Log.Info("CouponService", $"Validating coupon '{code}'...");
        await Task.Delay(100, ct);

        Coupon? coupon =
            await _couponRepository.GetByCodeAsync(code, ct);

        await Task.Delay(100, ct);

        if (coupon is null)
        {
            throw new InvalidOperationException(
                $"Coupon '{code}' does not exist.");
        }

        if (coupon.ExpiryDate.Date < DateTime.Today)
        {
            throw new InvalidOperationException(
                $"Coupon '{code}' has expired.");
        }

        if (coupon.DiscountPercent is < 0 or > 100)
        {
            throw new InvalidOperationException(
                $"Coupon '{code}' has an invalid discount percentage.");
        }

        Log.Success("CouponService", $"Coupon '{code}' is valid.");

        return coupon;
    }
}
    