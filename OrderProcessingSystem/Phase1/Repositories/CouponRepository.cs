
using Phase1.Interfaces;
using Shared.Logging;
using Shared.Models;
using Shared.Storage;

namespace Phase1.Repositories;

public class CouponRepository : ICouponRepository
{
    private const string Source = "CouponRepository";

    public async Task<List<Coupon>> GetAllAsync(
        CancellationToken ct = default)
    {
        Log.Info(Source, "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info(Source, "Executing query for all coupons...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);

        List<Coupon> coupons =
            await JsonFileStore.ReadAsync<Coupon>(
                DataFiles.Coupons, ct);

        Log.Success(Source, $"Retrieved {coupons.Count} coupons.");

        return coupons;
    }

    public async Task<Coupon?> GetByCodeAsync(
        string code,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Log.Info(Source, "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info(Source, $"Executing query for coupon '{code}'...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);

        List<Coupon> coupons =
            await JsonFileStore.ReadAsync<Coupon>(
                DataFiles.Coupons, ct);

        Coupon? coupon = coupons.FirstOrDefault(
            c => string.Equals(
                c.Code, code, StringComparison.OrdinalIgnoreCase));

        if (coupon is null)
        {
            Log.Error(Source, $"Coupon '{code}' was not found.");
            return null;
        }

        Log.Success(Source, $"Coupon '{code}' found.");

        return coupon;
    }
}
