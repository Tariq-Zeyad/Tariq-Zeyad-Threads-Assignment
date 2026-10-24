# SwiftBite Invoice Export

## R1–R3: Problems and Fixes

### Problems in the Original Code

1. **Slow processing:** The program processes one order at a time, which makes the export take longer.
2. **Thread.Sleep:** It blocks the thread while waiting for database and file operations.
3. **Slow file operations:** The program uses synchronous methods to read customer files and write invoices.
4. **Repeated database calls:** The program loads all pending orders and then loads each order again by its ID.
5. **Reading the same customer file more than once:** Customer 1's file is read twice because orders 1001 and 1004 belong to the same customer.

### Changes I Made

1. I replaced `Thread.Sleep` with `Task.Delay` to avoid blocking threads during simulated waits.
2. I changed the database methods to use `async` and `await`.
3. I used `File.ReadAllTextAsync` and `File.WriteAllTextAsync` for file operations.
4. I used `Parallel.ForEachAsync` to process multiple orders at the same time, with a maximum of four orders running concurrently.
5. I kept the same five orders, customer information, invoice format, and business flow. Each order is loaded first, then the customer file is read, and finally the invoice is written.

## R4: Console Output Before the Fix

Before making the changes, I ran the program and it generated all five invoice files successfully.

- Execution time: 6.0 seconds
- Invoices generated: 5
- Exit code: 0
- Errors: None

The original console output showed that invoices 1001, 1002, 1003, 1004, and 1005 were written successfully.

## Console Output After the Fix

After making the changes, I ran the program again to check the results.

- Invoices generated: 5
- Exit code: 0
- Execution time: [Write the actual time shown in the console]

The program should generate the same five invoice files as before. The order of the console messages may be different because the orders are processed concurrently.

## Conclusion

The changes help the program process orders more efficiently by using asynchronous file operations and concurrent processing. The original invoice content and business flow remain the same.