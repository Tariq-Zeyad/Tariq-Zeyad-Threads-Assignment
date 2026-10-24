
using System;
using System.Linq;
using System.Threading.Tasks;
using TableBooking;

var booking = new FridayNightBooking();

Task[] customers = new Task[40];

for (int i = 0; i < customers.Length; i++)
{
    string name = $"Customer-{i + 1:00}";
    customers[i] = booking.Book(name);
}

await Task.WhenAll(customers);

Console.WriteLine();
Console.WriteLine($"Booked={booking.Booked}, TablesLeft={booking.TablesLeft}");
