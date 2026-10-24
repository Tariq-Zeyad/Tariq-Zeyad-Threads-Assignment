
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Interfaces;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Processors;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Retry;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Services;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Checkout;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Dashboard;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase4.EmailResend;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Fraud;
using ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Shipping;
using Phase1.Interfaces;
using Phase1.Repositories;
using Phase2.Pricing;
using Phase2.Strategies;
using Phase2.Strategies.Interfaces;
using Shared.Storage;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase4.Runners;

public sealed class Phase4Runner
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("\n========================================");
        Console.WriteLine(" PHASE 4: ADVANCED ASYNC ORCHESTRATION");
        Console.WriteLine("========================================");

        IProductRepository productRepository = new ProductRepository();
        IOrderRepository orderRepository =
            new OrderRepository(failSaveForOrderIds: new[] { 13 });
        ICustomerRepository customerRepository = new CustomerRepository();
        ICouponRepository couponRepository = new CouponRepository();

        var subtotalStrategy = new SubtotalStrategy();
        var discountStrategy = new DiscountStrategy();
        var shippingStrategy = new ShippingStrategy();

        var phase2PricingService =
            new global::Phase2.Services.PricingService(
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

        var orderProcessor = new OrderProcessor(
            customerService,
            inventoryService,
            phase3PricingService,
            paymentGateway,
            orderRepository,
            emailRetryService);

        // Task 10.1: إعادة التهيئة ومعالجة الطلبات العشرين.
        var checkoutRunner = new LiveCheckoutRunner(orderProcessor);
        await checkoutRunner.RunAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var dashboardRunner = new CustomerDashboardRunner(
            new CustomerDashboardService(
                failPaymentsForCustomerIds: new[] { 99 }));

        await dashboardRunner.RunAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var carriers = new IShippingCarrier[]
        {
            new FastShipCarrier(failForOrderIds: new[] { 6 }),
            new CairoExpressCarrier(failForOrderIds: new[] { 6 }),
            new NileCourierCarrier(failForOrderIds: new[] { 6 })
        };
        var shippingRunner = new ShippingRunner(
            new ShippingQuoteService(carriers));

        await shippingRunner.RunAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var fraudRunner = new FraudScoringRunner(
            new FraudScoringService());

        await fraudRunner.RunAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var resendRunner = new EmailResendRunner(
            new EmailResendService(
                new EmailService(failuresEnabled: false)));

        await resendRunner.RunAsync(cancellationToken);

        Console.WriteLine("\nPhase 4 finished.");
    }
}
