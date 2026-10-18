# Table Booking – Answers

1. **What is the problem?**

   When multiple customers try to book tables at the same time, a race condition may happen and cause incorrect booking results.

2. **How does SemaphoreSlim help?**

   `SemaphoreSlim` allows only one customer at a time to access the booking section, keeping the shared data safe.

3. **Why use WaitAsync()?**

   It waits asynchronously until the semaphore is available without blocking the thread.

4. **Why use finally?**

   The `finally` block ensures that the semaphore is released even if an error occurs.

5. **What is the expected result?**

   There are 5 tables and 40 customers. Only 5 customers can book successfully.

   - Booked = 5
   - TablesLeft = 0

6. **Conclusion**

   `SemaphoreSlim` prevents multiple customers from booking the same available table and keeps the booking count correct.