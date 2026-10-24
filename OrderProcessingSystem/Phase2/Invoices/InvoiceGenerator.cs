using Shared.Models;
using System.Globalization;

namespace Phase2.Invoices;

public class InvoiceGenerator
{
    private const int RenderingIterations = 20_000_000;
    private const int CancellationCheckInterval = 100_000;
    private const string InvoiceDirectory = "Data/invoices";
    private const string InvoiceFileNameFormat = "invoice_{0}.txt";
    private const string MoneyFormat = "F2";

    public async Task GenerateAsync(
        Order order,
        CancellationToken ct = default)
    {
        string invoiceContent = await Task.Run(
            () => RenderInvoice(order, ct),
            ct);

        Directory.CreateDirectory(InvoiceDirectory);

        string fileName = string.Format(
            CultureInfo.InvariantCulture,
            InvoiceFileNameFormat,
            order.Id);

        string filePath = Path.Combine(
            InvoiceDirectory,
            fileName);

        await File.WriteAllTextAsync(
            filePath,
            invoiceContent,
            ct);
    }

    private static string RenderInvoice(
        Order order,
        CancellationToken ct)
    {
        long renderingResult = 0;

        for (int iteration = 0;
             iteration < RenderingIterations;
             iteration++)
        {
            if (iteration % CancellationCheckInterval == 0)
            {
                ct.ThrowIfCancellationRequested();
            }

            renderingResult += iteration;
        }

        ct.ThrowIfCancellationRequested();

        return $"""
            Invoice
            Order ID: {order.Id}
            Customer ID: {order.CustomerId}
            Subtotal: {order.SubTotal.ToString(MoneyFormat, CultureInfo.InvariantCulture)}
            Discount: {order.Discount.ToString(MoneyFormat, CultureInfo.InvariantCulture)}
            Shipping: {order.ShippingCost.ToString(MoneyFormat, CultureInfo.InvariantCulture)}
            Total: {order.TotalAmount.ToString(MoneyFormat, CultureInfo.InvariantCulture)}
            """;
    }
}
