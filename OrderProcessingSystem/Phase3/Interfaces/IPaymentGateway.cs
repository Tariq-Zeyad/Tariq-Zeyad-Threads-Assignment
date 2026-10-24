
using Shared.Models;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Interfaces;

public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(
        int orderId,
        decimal amount,
        CancellationToken ct = default);

    Task RefundAsync(string transactionId);
}
