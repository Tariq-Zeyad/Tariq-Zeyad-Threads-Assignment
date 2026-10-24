
using Shared.Models;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Interfaces;

public interface IEmailService
{
    Task SendConfirmationAsync(
        Order order,
        Customer customer,
        CancellationToken cancellationToken = default);
}
