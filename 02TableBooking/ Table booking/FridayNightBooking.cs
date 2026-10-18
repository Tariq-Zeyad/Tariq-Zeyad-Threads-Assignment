
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TableBooking;

public sealed class FridayNightBooking
{
    public int TablesLeft { get; private set; } = 5;
    public int Booked { get; private set; }

    public readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

    public async Task Book(string customerName)
    {
        await semaphore.WaitAsync();

        try
        {
            await BookTable(customerName);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task BookTable(string customerName)
    {
        if (TablesLeft <= 0)
        {
            Console.WriteLine($"{customerName}: No tables available.");
            return;
        }
        await Task.Delay(20);
        TablesLeft--;
        Booked++;
        Console.WriteLine($"{customerName} booked a table. Left={TablesLeft}");
    }
}
