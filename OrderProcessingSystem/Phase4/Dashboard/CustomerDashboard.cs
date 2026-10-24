
using Shared.Models;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Dashboard;

public sealed class CustomerDashboard
{
    public int CustomerId { get; init; }
    public Customer? Profile { get; init; }
    public List<Order> Orders { get; init; } = new();
    public List<PaymentTransaction> Payments { get; init; } = new();
    public List<EmailMessage> Emails { get; init; } = new();
    public List<string> Errors { get; init; } = new();

    public bool IsSuccessful => Errors.Count == 0;
}
