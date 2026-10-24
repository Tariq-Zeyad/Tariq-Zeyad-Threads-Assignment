
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Services;
using Shared.Models;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Processors;

public class FireAndForgetDemo
{
    private readonly EmailService _emailService;

    public FireAndForgetDemo(EmailService emailService)
    {
        _emailService = emailService;
    }

    // Intentionally broken: demonstrates fire-and-forget.
    public async Task RunBrokenAsync(
        Order order,
        Customer customer)
    {
        Console.WriteLine("Starting fire-and-forget email...");

#pragma warning disable CS4014
        SendEmailWithDelayAsync(order, customer);
#pragma warning restore CS4014

        Console.WriteLine(
            "Workflow finished without waiting for the email.");

        await Task.CompletedTask;
    }

    // Fixed version: wait for the email and observe failures.
    public async Task RunFixedAsync(
        Order order,
        Customer customer)
    {
        Console.WriteLine("Starting awaited email...");

        try
        {
            await SendEmailWithDelayAsync(order, customer);

            Console.WriteLine("Email sent successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email failed: {ex.Message}");
        }

        Console.WriteLine("Email operation finished.");
    }

    private async Task SendEmailWithDelayAsync(
        Order order,
        Customer customer)
    {
        // Makes the difference visible during this demonstration.
        await Task.Delay(TimeSpan.FromSeconds(3));

        await _emailService.SendConfirmationAsync(order, customer);
    }
}
