using Phase1.Interfaces;
using Shared.Models;
using Shared.Storage;

namespace Phase2.Runners;

public class RaceConditionDemo
{
    private const int KeyboardProductId = 4;
    private const int AvailableStock = 1;
    private const int ReservationQuantity = 1;
    private const int FirstOrderId = 10;
    private const int SecondOrderId = 11;

    private readonly IProductRepository _productRepository;

    public RaceConditionDemo(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        await SetKeyboardStockAsync(ct);

        Console.WriteLine("Unsafe reservation demo");

        Task<bool> firstUnsafeTask =
            _productRepository.ReserveStockUnsafeAsync(
                KeyboardProductId,
                ReservationQuantity,
                ct);

        Task<bool> secondUnsafeTask =
            _productRepository.ReserveStockUnsafeAsync(
                KeyboardProductId,
                ReservationQuantity,
                ct);

        bool[] unsafeResults = await Task.WhenAll(
            firstUnsafeTask,
            secondUnsafeTask);

        await PrintResultsAsync(
            unsafeResults[0],
            unsafeResults[1],
            ct);

        await SetKeyboardStockAsync(ct);

        Console.WriteLine();
        Console.WriteLine("Safe reservation demo");

        Task<bool> firstSafeTask =
            _productRepository.ReserveStockAsync(
                KeyboardProductId,
                ReservationQuantity,
                ct);

        Task<bool> secondSafeTask =
            _productRepository.ReserveStockAsync(
                KeyboardProductId,
                ReservationQuantity,
                ct);

        bool[] safeResults = await Task.WhenAll(
            firstSafeTask,
            secondSafeTask);

        await PrintResultsAsync(
            safeResults[0],
            safeResults[1],
            ct);
    }

    private static async Task SetKeyboardStockAsync(
        CancellationToken ct)
    {
        List<Product> products =
            await JsonFileStore.ReadAsync<Product>(
                DataFiles.Products,
                ct);

        Product? keyboard = products.FirstOrDefault(
            product => product.Id == KeyboardProductId);

        if (keyboard is null)
        {
            throw new InvalidOperationException(
                $"Product #{KeyboardProductId} was not found.");
        }

        keyboard.StockQuantity = AvailableStock;

        await JsonFileStore.WriteAsync(
            DataFiles.Products,
            products,
            ct);
    }


    private async Task PrintResultsAsync(
        bool firstResult,
        bool secondResult,
        CancellationToken ct)
    {
        Console.WriteLine(
            $"Order #{FirstOrderId}: {(firstResult ? "Reservation successful" : "Reservation failed")}");

        Console.WriteLine(
            $"Order #{SecondOrderId}: {(secondResult ? "Reservation successful" : "Reservation failed")}");

        Product? keyboard = await _productRepository.GetByIdAsync(
            KeyboardProductId,
            ct);

        Console.WriteLine(
            $"Keyboard stock remaining: {keyboard?.StockQuantity}");
    }

}