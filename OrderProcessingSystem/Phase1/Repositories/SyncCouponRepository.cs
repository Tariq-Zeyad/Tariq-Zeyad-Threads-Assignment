
using Shared.Logging;
using Shared.Models;
using Shared.Storage;
using System.Threading;

namespace Phase1.Repositories;

public class SyncCouponRepository
{
    private const string Source = "SyncCouponRepository";

    public Coupon? GetByCode(string code)
    {
        Log.Info(Source, "Opening connection...");
        Thread.Sleep(200);

        Log.Info(Source, $"Executing query for coupon '{code}'...");
        Thread.Sleep(Random.Shared.Next(300, 601));

        List<Coupon> coupons =
            JsonFileStore.Read<Coupon>(DataFiles.Coupons);

        Coupon? coupon = coupons.FirstOrDefault(
            c => string.Equals(
                c.Code,
                code,
                StringComparison.OrdinalIgnoreCase));

        if (coupon is null)
        {
            Log.Error(Source, $"Coupon '{code}' was not found.");
            return null;
        }

        Log.Success(Source, $"Coupon '{code}' found.");
        return coupon;
    }
}
