
using Phase1.Interfaces;
using Phase1.Repositories;
using Phase1.Runners;
using Phase1.Validators;
using Phase2.Invoices;
using Phase2.Pricing;
using Phase2.Processors;
using Phase2.Prototypes;
using Phase2.Reports;
using Phase2.Services;
using Phase2.Strategies;
using Phase2.Runners;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase2.Runners;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Interfaces;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Processors;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Retry;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Runners;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Services;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Runners;
using Shared.Logging;
using Shared.Storage;

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run");
    Console.WriteLine("  dotnet run -- setup");
    Console.WriteLine("  dotnet run -- phase1");
    Console.WriteLine("  dotnet run -- phase2");
    Console.WriteLine("  dotnet run -- phase3");
    Console.WriteLine("  dotnet run -- phase3-compare");
    Console.WriteLine("  dotnet run -- phase3-fire-and-forget");
    Console.WriteLine("  dotnet run -- phase3-fire-and-forget-fixed");
    Console.WriteLine("  dotnet run -- phase4");
    Console.WriteLine("  dotnet run -- all");
    Console.WriteLine("  Add --cancel-after 4000 to request cancellation after 4000 ms.");
}

static string? ShowMenu()
{
    Console.WriteLine();
    Console.WriteLine("======================================");
    Console.WriteLine("    Order Processing System");
    Console.WriteLine("======================================");
    Console.WriteLine("1. Setup / Reset data");
    Console.WriteLine("2. Phase 1");
    Console.WriteLine("3. Phase 2");
    Console.WriteLine("4. Phase 3");
    Console.WriteLine("5. Phase 3 - Sync vs Async comparison");
    Console.WriteLine("6. Phase 3 - Fire-and-forget demo");
    Console.WriteLine("7. Phase 3 - Fixed fire-and-forget demo");
    Console.WriteLine("8. Phase 4");
    Console.WriteLine("9. Run all phases");
    Console.WriteLine("0. Exit");
    Console.Write("Select an option: ");

    return Console.ReadLine()?.Trim() switch
    {
        "1" => "setup",
        "2" => "phase1",
        "3" => "phase2",
        "4" => "phase3",
        "5" => "phase3-compare",
        "6" => "phase3-fire-and-forget",
        "7" => "phase3-fire-and-forget-fixed",
        "8" => "phase4",
        "9" => "all",
        "0" => null,
        _ => "invalid"
    };
}

string phase;
int argStartIndex;

if (args.Length == 0)
{
    string? selected = ShowMenu();

    if (selected is null)
    {
        Console.WriteLine("Goodbye.");
        return;
    }

    if (selected == "invalid")
    {
        Console.WriteLine("Invalid selection.");
        PrintUsage();
        return;
    }

    phase = selected;
    argStartIndex = args.Length;
}
else
{
    phase = args[0].ToLowerInvariant();
    argStartIndex = 1;
}

int? cancelAfterMs = null;

for (int index = argStartIndex; index < args.Length; index++)
{
    if (args[index] == "--cancel-after" &&
        index + 1 < args.Length &&
        int.TryParse(args[index + 1], out int milliseconds) &&
        milliseconds >= 0)
    {
        cancelAfterMs = milliseconds;
        index++;
    }
}

var validPhases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "setup",
    "phase1",
    "phase2",
    "phase3",
    "phase3-compare",
    "phase3-fire-and-forget",
    "phase3-fire-and-forget-fixed",
    "phase4",
    "all"
};

if (!validPhases.Contains(phase))
{
    Console.WriteLine($"Unknown phase: {phase}");
    PrintUsage();
    return;
}

using var cts = new CancellationTokenSource();

ConsoleCancelEventHandler cancelHandler = (_, e) =>
{
    e.Cancel = true;

    if (!cts.IsCancellationRequested)
    {
        cts.Cancel();
        Console.WriteLine("\nCancellation requested...");
    }
};

Console.CancelKeyPress += cancelHandler;

try
{
    if (cancelAfterMs.HasValue)
    {
        cts.CancelAfter(cancelAfterMs.Value);
    }

    Console.WriteLine($"Selected mode: {phase}");

    if (phase == "all")
    {
        // Run the primary phases in sequence.
        // Each phase receives the same cancellation token.
        await RunPhaseAsync("setup", cts.Token);
        await RunPhaseAsync("phase1", cts.Token);
        await RunPhaseAsync("phase2", cts.Token);
        await RunPhaseAsync("phase3", cts.Token);
        await RunPhaseAsync("phase4", cts.Token);
    }
    else
    {
        await RunPhaseAsync(phase, cts.Token);
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Run cancelled.");
}
catch (Exception ex)
{
    Log.Error("Program", ex.ToString());
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}

async Task RunPhaseAsync(string selectedPhase, CancellationToken ct)
{
    ct.ThrowIfCancellationRequested();

    Console.WriteLine();
    Console.WriteLine($"========== {selectedPhase.ToUpperInvariant()} ==========");

    switch (selectedPhase)
    {
        case "setup":
            {
                await DataSeeder.ResetAsync(ct);
                Console.WriteLine("Setup completed successfully.");
                break;
            }

        case "phase1":
            {
                var runner = new Phase1Runner();
                await runner.RunAsync(ct);
                break;
            }

        case "phase2":
            {
                IProductRepository productRepository = new ProductRepository();
                IOrderRepository orderRepository = new OrderRepository();
                ICustomerRepository customerRepository = new CustomerRepository();
                ICouponRepository couponRepository = new CouponRepository();

                var subtotalStrategy = new SubtotalStrategy();
                var discountStrategy = new DiscountStrategy();
                var shippingStrategy = new ShippingStrategy();

                var pricingService = new Phase2.Services.PricingService(
                    productRepository,
                    customerRepository,
                    couponRepository,
                    subtotalStrategy,
                    discountStrategy,
                    shippingStrategy,
                    new[] { 7 });

                var orderPricer = new OrderPricer(pricingService);

                var orderValidator = new OrderValidator(
                    customerRepository,
                    productRepository,
                    couponRepository,
                    orderRepository);

                var invoiceGenerator = new InvoiceGenerator();

                var orderProcessor = new OrderProcessorV2(
                    productRepository,
                    orderRepository,
                    orderPricer,
                    invoiceGenerator);

                var orderPrototype = new OrderPrototype();
                var raceConditionDemo = new RaceConditionDemo(productRepository);

                var reportGenerator = new ReportGenerator(
                    orderRepository,
                    productRepository);

                var runner = new Phase2Runner(
                    orderRepository,
                    orderPricer,
                    orderProcessor,
                    orderValidator,
                    orderPrototype,
                    raceConditionDemo,
                    reportGenerator);

                await runner.RunAsync(ct);
                break;
            }

        case "phase3":
            {
                IProductRepository productRepository = new ProductRepository();

                IOrderRepository orderRepository = new OrderRepository(
                    failSaveForOrderIds: new[] { 13 });

                ICustomerRepository customerRepository = new CustomerRepository();
                ICouponRepository couponRepository = new CouponRepository();

                var subtotalStrategy = new SubtotalStrategy();
                var discountStrategy = new DiscountStrategy();
                var shippingStrategy = new ShippingStrategy();

                var phase2PricingService = new Phase2.Services.PricingService(
                    productRepository,
                    customerRepository,
                    couponRepository,
                    subtotalStrategy,
                    discountStrategy,
                    shippingStrategy,
                    Array.Empty<int>());

                var orderPricer = new OrderPricer(phase2PricingService);

                var customerService = new CustomerService(customerRepository);
                var inventoryService = new InventoryService(productRepository);

                var phase3PricingService =
                    new ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Services.PricingService(
                        orderPricer,
                        couponRepository);

                IPaymentGateway paymentGateway = new PaymentGateway(
                    timeoutOrderIds: new[] { 5 },
                    declineOrderIds: new[] { 8 });

                IEmailService emailService = new EmailService(
                    failAttempts: new Dictionary<int, int>
                    {
                        [3] = 2,
                        [17] = int.MaxValue
                    });

                var emailRetryService = new EmailRetryService(emailService);

                var orderProcessor =
                    new ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Processors.OrderProcessor(
                        customerService,
                        inventoryService,
                        phase3PricingService,
                        paymentGateway,
                        orderRepository,
                        emailRetryService);

                var runner = new Phase3Runner(orderProcessor);

                await runner.RunAsync(ct);
                break;
            }

        case "phase3-compare":
            {
                var runner = new SyncVsAsyncRunner();
                await runner.RunAsync(ct);
                break;
            }

        case "phase3-fire-and-forget":
        case "phase3-fire-and-forget-fixed":
            {
                var runner = new FireAndForgetRunner();

                bool fixedVersion = selectedPhase == "phase3-fire-and-forget-fixed";

                await runner.RunAsync(
                    fixedVersion: fixedVersion,
                    ct: ct);

                break;
            }

        case "phase4":
            {
                var runner = new Phase4Runner();
                await runner.RunAsync(ct);
                break;
            }

        default:
            throw new ArgumentOutOfRangeException(
                nameof(selectedPhase),
                selectedPhase,
                "Unknown phase.");
    }
}
