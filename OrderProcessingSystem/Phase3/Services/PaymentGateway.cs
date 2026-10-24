
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Interfaces;
using ECommerceOrderProcessing.OrderProcessingSystem.Shared.Models;
using Shared.Models;
using Shared.Storage;
using System.Collections.Concurrent;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Services;

public class PaymentGateway : IPaymentGateway
{
    private static readonly SemaphoreSlim PaymentLock = new(1, 1);

    private readonly HashSet<int> _timeoutOrderIds;
    private readonly HashSet<int> _declineOrderIds;
    private readonly bool _failuresEnabled;

    public PaymentGateway(
        IEnumerable<int>? timeoutOrderIds = null,
        IEnumerable<int>? declineOrderIds = null,
        bool failuresEnabled = true)
    {
        _timeoutOrderIds = new HashSet<int>(
            timeoutOrderIds ?? Array.Empty<int>());

        _declineOrderIds = new HashSet<int>(
            declineOrderIds ?? Array.Empty<int>());

        _failuresEnabled = failuresEnabled;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(
        int orderId,
        decimal amount,
        CancellationToken ct = default)
    {
        int delay = _failuresEnabled &&
                    _timeoutOrderIds.Contains(orderId)
            ? 5000
            : 700;

        await Task.Delay(delay, ct);

        await Task.Delay(100, ct);

        if (_failuresEnabled &&
            _declineOrderIds.Contains(orderId))
        {
            var declined = new PaymentTransaction
            {
                OrderId = orderId,
                Amount = amount,
                Status = PaymentStatus.Declined,
                TransactionId = null,
                CreatedAt = DateTime.UtcNow
            };

            await AppendPaymentAsync(declined, ct);

            return new PaymentResult
            {
                Success = false,
                TransactionId = null,
                Status = PaymentStatus.Declined,
                Message = "Card declined by issuer"
            };
        }

        string transactionId =
            "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

        var captured = new PaymentTransaction
        {
            OrderId = orderId,
            Amount = amount,
            Status = PaymentStatus.Captured,
            TransactionId = transactionId,
            CreatedAt = DateTime.UtcNow
        };

        await AppendPaymentAsync(captured, ct);

        return new PaymentResult
        {
            Success = true,
            TransactionId = transactionId,
            Status = PaymentStatus.Captured,
            Message = "Payment captured successfully"
        };
    }

    public async Task RefundAsync(string transactionId)
    {
        await PaymentLock.WaitAsync();

        try
        {
            var payments = await JsonFileStore
                .ReadAsync<PaymentTransaction>(DataFiles.Payments);

            var payment = payments.FirstOrDefault(p =>
                p.TransactionId == transactionId &&
                p.Status == PaymentStatus.Captured);

            if (payment is null)
            {
                throw new InvalidOperationException(
                    $"Captured payment '{transactionId}' was not found.");
            }

            payment.Status = PaymentStatus.Refunded;

            await JsonFileStore.WriteAsync(
                DataFiles.Payments,
                payments);
        }
        finally
        {
            PaymentLock.Release();
        }
    }

    private static async Task AppendPaymentAsync(
        PaymentTransaction payment,
        CancellationToken ct)
    {
        await PaymentLock.WaitAsync(ct);

        try
        {
            var payments = await JsonFileStore
                .ReadAsync<PaymentTransaction>(
                    DataFiles.Payments, ct);

            payments.Add(payment);

            await JsonFileStore.WriteAsync(
                DataFiles.Payments,
                payments,
                ct);
        }
        finally
        {
            PaymentLock.Release();
        }
    }
}
