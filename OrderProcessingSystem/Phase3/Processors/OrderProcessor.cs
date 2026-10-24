
using System.Diagnostics;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Interfaces;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Retry;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Services;
using Phase1.Interfaces;
using Shared.Exceptions;
using Shared.Models;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Processors;

public class OrderProcessor
{
    private readonly CustomerService _customerService;
    private readonly InventoryService _inventoryService;
    private readonly PricingService _pricingService;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IOrderRepository _orderRepository;
    private readonly EmailRetryService _emailRetryService;

    public OrderProcessor(
        CustomerService customerService,
        InventoryService inventoryService,
        PricingService pricingService,
        IPaymentGateway paymentGateway,
        IOrderRepository orderRepository,
        EmailRetryService emailRetryService)
    {
        _customerService = customerService;
        _inventoryService = inventoryService;
        _pricingService = pricingService;
        _paymentGateway = paymentGateway;
        _orderRepository = orderRepository;
        _emailRetryService = emailRetryService;
    }

    public async Task<bool> ProcessOrderAsync(
        Order order,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        bool stockReserved = false;
        Customer? customer = null;

        try
        {
            ct.ThrowIfCancellationRequested();

            customer = await ExecuteStepAsync(
                order.Id,
                "1/6 Customer",
                () => _customerService.GetCustomerAsync(order.CustomerId));

            ct.ThrowIfCancellationRequested();

            if (!customer.IsActive)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = "Customer is inactive";

                await SaveFinalOrderAsync(order);
                return false;
            }

            bool reserved = await ExecuteStepAsync(
                order.Id,
                "2/6 Inventory",
                () => _inventoryService.ReserveStockAsync(order));

            if (!reserved)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason =
                    "Insufficient stock for one or more products";

                await SaveFinalOrderAsync(order);
                return false;
            }

            stockReserved = true;

            ct.ThrowIfCancellationRequested();

            await ExecuteStepAsync(
                order.Id,
                "3/6 Pricing",
                async () =>
                {
                    await _pricingService.CalculateTotalAsync(order, ct);
                    return true;
                });

            ct.ThrowIfCancellationRequested();

            PaymentResult payment;

            using (var timeoutCts =
                   new CancellationTokenSource())
            using (var linkedCts =
                   CancellationTokenSource.CreateLinkedTokenSource(
                       ct,
                       timeoutCts.Token))
            {
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

                try
                {
                    payment = await ExecuteStepAsync(
                        order.Id,
                        "4/6 Payment",
                        () => _paymentGateway.ProcessPaymentAsync(
                            order.Id,
                            order.TotalAmount,
                            linkedCts.Token));
                }
                catch (OperationCanceledException)
                    when (!ct.IsCancellationRequested &&
                          timeoutCts.IsCancellationRequested)
                {
                    await ReleaseStockSafelyAsync(order, stockReserved);
                    stockReserved = false;

                    order.Status = OrderStatus.PaymentFailed;
                    order.FailureReason =
                        "Payment gateway timeout (3s)";

                    await SaveFinalOrderAsync(order);
                    return false;
                }
            }

            ct.ThrowIfCancellationRequested();

            if (!payment.Success)
            {
                await ReleaseStockSafelyAsync(order, stockReserved);
                stockReserved = false;

                order.Status = OrderStatus.PaymentFailed;
                order.FailureReason =
                    payment.Message ?? "Payment declined";

                await SaveFinalOrderAsync(order);
                return false;
            }

            order.TransactionId = payment.TransactionId;
            order.Status = OrderStatus.Completed;
            order.FailureReason = null;

            try
            {
                await ExecuteStepAsync(
                    order.Id,
                    "5/6 Save",
                    async () =>
                    {
                        await _orderRepository.SaveAsync(order, ct);
                        return true;
                    });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                await RefundAndReleaseAsync(order, stockReserved);
                stockReserved = false;

                order.Status = OrderStatus.Failed;
                order.FailureReason =
                    "Order processing cancelled after payment";

                await SaveFinalOrderAsync(order);
                return false;
            }
            catch (Exception ex)
            {
                Log(
                    order.Id,
                    "5/6 Save",
                    "FAILED",
                    0,
                    ex.Message);

                await RefundAndReleaseAsync(order, stockReserved);
                stockReserved = false;

                order.Status = OrderStatus.Failed;
                order.FailureReason =
                    $"Database save failed after payment; payment {order.TransactionId} refunded";

                await SaveFinalOrderAsync(order);
                return false;
            }

            try
            {
                await ExecuteStepAsync(
                    order.Id,
                    "6/6 Email",
                    async () =>
                    {
                        await _emailRetryService
                            .SendConfirmationWithRetryAsync(order, customer);

                        return true;
                    });
            }
            catch (Exception ex)
            {
                Log(
                    order.Id,
                    "6/6 Email",
                    "FAILED",
                    0,
                    ex.Message);
            }

            return order.Status == OrderStatus.Completed;
        }
        catch (CustomerNotFoundException ex)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = ex.Message;

            await SaveFinalOrderAsync(order);
            return false;
        }
        catch (InvalidCouponException ex)
        {
            if (stockReserved)
            {
                await ReleaseStockSafelyAsync(order, stockReserved);
                stockReserved = false;
            }

            order.Status = OrderStatus.Rejected;
            order.FailureReason = ex.Message;

            await SaveFinalOrderAsync(order);
            return false;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            if (stockReserved)
            {
                await ReleaseStockSafelyAsync(order, stockReserved);
                stockReserved = false;
            }

            order.Status = OrderStatus.Failed;
            order.FailureReason = "Order processing cancelled";

            await SaveFinalOrderAsync(order);
            return false;
        }
    }

    private async Task RefundAndReleaseAsync(
        Order order,
        bool stockReserved)
    {
        if (!string.IsNullOrWhiteSpace(order.TransactionId))
        {
            await _paymentGateway.RefundAsync(order.TransactionId);
        }

        if (stockReserved)
        {
            await _inventoryService.ReleaseStockAsync(order);
        }
    }

    private async Task ReleaseStockSafelyAsync(
        Order order,
        bool stockReserved)
    {
        if (stockReserved)
        {
            await _inventoryService.ReleaseStockAsync(order);
        }
    }

    private async Task SaveFinalOrderAsync(Order order)
    {
        await _orderRepository.SaveAsync(order);
    }

    private static async Task<T> ExecuteStepAsync<T>(
        int orderId,
        string step,
        Func<Task<T>> action)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            T result = await action();

            stopwatch.Stop();
            Log(
                orderId,
                step,
                "OK",
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            Log(
                orderId,
                step,
                "CANCELLED",
                stopwatch.ElapsedMilliseconds);

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Log(
                orderId,
                step,
                "FAILED",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }

    private static void Log(
        int orderId,
        string step,
        string result,
        long elapsedMs,
        string? details = null)
    {
        Console.WriteLine(
            $"[{DateTime.Now:HH:mm:ss.fff}] " +
            $"[Thread {Environment.CurrentManagedThreadId}] " +
            $"Order #{orderId} -> {step} {result} " +
            $"in {elapsedMs} ms" +
            (details is null ? "" : $" | {details}"));
    }
}
