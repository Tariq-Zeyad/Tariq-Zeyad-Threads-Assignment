
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Interfaces;
using Shared.Exceptions;
using Shared.Models;
using Shared.Storage;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Retry;

public class EmailRetryService
{
    private readonly IEmailService _emailService;

    public EmailRetryService(IEmailService emailService)
    {
        _emailService = emailService;
    }



    public async Task SendConfirmationWithRetryAsync(
        Order order,
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        int[] delays = { 500, 1000, 2000 };
        string lastError = string.Empty;

        for (int attempt = 1; attempt <= 4; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _emailService.SendConfirmationAsync(
                    order,
                    customer,
                    cancellationToken);

                Console.WriteLine(
                    $"Order #{order.Id} email sent successfully on attempt {attempt}.");

                return;
            }
            catch (SmtpUnavailableException ex)
            {
                lastError = ex.Message;

                Console.WriteLine(
                    $"Order #{order.Id} email attempt {attempt}/4 failed: {ex.Message}");

                if (attempt < 4)
                {
                    await Task.Delay(
                        delays[attempt - 1],
                        cancellationToken);
                }
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        var retryRecord = new EmailMessage
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            To = customer.Email,
            Subject = $"Order #{order.Id} Confirmation",
            Body = $"Your order #{order.Id} has been completed. Total: {order.TotalAmount:F2}",
            SentAt = DateTime.UtcNow,
            Attempts = 4,
            LastError = lastError
        };

        await AppendRetryRecordAsync(retryRecord);

        Console.WriteLine(
            $"Email for Order #{order.Id} queued for retry later");
    }


    private static async Task AppendRetryRecordAsync(
        EmailMessage message)
    {
        const string fileName = "email_retry.json";

        await RetryLock.WaitAsync();

        try
        {
            var records = await JsonFileStore
                .ReadAsync<EmailMessage>(fileName);

            records.Add(message);

            await JsonFileStore.WriteAsync(
                fileName,
                records);
        }
        finally
        {
            RetryLock.Release();
        }
    }

    private static readonly SemaphoreSlim RetryLock = new(1, 1);
}
