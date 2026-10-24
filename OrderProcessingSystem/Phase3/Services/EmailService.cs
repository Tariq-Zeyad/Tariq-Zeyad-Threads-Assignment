
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Interfaces;
using Shared.Exceptions;
using Shared.Models;
using Shared.Storage;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Services;

public class EmailService : IEmailService
{
    private static readonly SemaphoreSlim EmailLock = new(1, 1);

    private readonly Dictionary<int, int> _failAttempts;
    private readonly Dictionary<int, int> _attemptCounts;
    private readonly bool _failuresEnabled;

    public EmailService(
        Dictionary<int, int>? failAttempts = null,
        bool failuresEnabled = true)
    {
        _failAttempts = failAttempts is null
            ? new Dictionary<int, int>()
            : new Dictionary<int, int>(failAttempts);

        _attemptCounts = new Dictionary<int, int>();
        _failuresEnabled = failuresEnabled;
    }

    public async Task SendConfirmationAsync(
        Order order,
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        await EmailLock.WaitAsync(cancellationToken);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            _attemptCounts.TryGetValue(order.Id, out int attempts);
            attempts++;
            _attemptCounts[order.Id] = attempts;

            if (_failuresEnabled &&
                _failAttempts.TryGetValue(order.Id, out int maxFailures) &&
                attempts <= maxFailures)
            {
                throw new SmtpUnavailableException(
                    $"Email service unavailable for Order #{order.Id}, attempt {attempts}.");
            }

            var emails = await JsonFileStore.ReadAsync<EmailMessage>(
                DataFiles.Emails,
                cancellationToken);

            // Idempotency: do not save another confirmation for this order.
            if (emails.Any(email => email.OrderId == order.Id))
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var email = new EmailMessage
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                To = customer.Email,
                Subject = $"Order #{order.Id} Confirmation",
                Body = $"Your order #{order.Id} has been completed. Total: {order.TotalAmount:F2}",
                SentAt = DateTime.UtcNow,
                Attempts = attempts,
                LastError = null
            };

            emails.Add(email);

            await JsonFileStore.WriteAsync(
                DataFiles.Emails,
                emails,
                cancellationToken);
        }
        finally
        {
            EmailLock.Release();
        }
    }
}
