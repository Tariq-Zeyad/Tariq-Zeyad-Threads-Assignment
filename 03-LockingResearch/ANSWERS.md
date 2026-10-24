# Assignment 03 — Locking Research

## 1. What is `System.Threading.Monitor` used for?

`System.Threading.Monitor` is a class in .NET that helps manage access to shared data when multiple threads are running at the same time.

Sometimes, two or more threads need to access or modify the same variable or object. If they do this at the same time without synchronization, the result may be incorrect. This problem is called a **race condition**.

The `Monitor` class helps prevent this problem by allowing only one thread at a time to enter a critical section protected by the same lock object.

For example, if two threads want to increase the same counter, we can use `Monitor.Enter()` to acquire a lock before changing the counter and `Monitor.Exit()` to release the lock afterward. While one thread owns the lock, another thread must wait before entering the protected section.

The `Monitor` class also provides other methods, such as `TryEnter()`, `Wait()`, and `Pulse()`. These methods are useful when we need more control over how threads acquire locks or communicate with each other.

In simple words, I can think of `Monitor` as a way to organize access to shared resources so that different threads do not interfere with each other.

## 2. What does the C# `lock` statement compile to?

The C# `lock` statement is a simple way to protect a critical section without manually managing every locking operation.

When we use `lock` with a normal reference-type object, the compiler translates it into code that uses `System.Threading.Monitor`. The generated code acquires the lock before executing the protected code and releases it when the block finishes.

For example:

```csharp
private readonly object _sync = new object();
private int _counter = 0;

public void Increment()
{
    lock (_sync)
    {
        _counter++;
    }
}
```

For a traditional reference-type lock object, this is conceptually similar to the following code:

```csharp
bool lockTaken = false;

try
{
    Monitor.Enter(_sync, ref lockTaken);

    _counter++;
}
finally
{
    if (lockTaken)
    {
        Monitor.Exit(_sync);
    }
}
```

The `try/finally` block is important because the lock must be released even if an exception occurs while executing the protected code.

The `lockTaken` variable indicates whether the lock was successfully acquired. This helps ensure that `Monitor.Exit()` is called only when the current thread actually acquired the lock.

Therefore, `lock` makes synchronization easier to write and reduces the chance of mistakes when acquiring and releasing locks manually.

One important detail is that modern C# also supports `System.Threading.Lock`. When the expression has this type, the compiler uses a different mechanism specifically designed for that lock type. So, it is not always correct to say that every C# `lock` statement is translated into `Monitor` calls.

## 3. List at least two differences between using `lock` and calling `Monitor` APIs directly.

There are several differences between using the `lock` statement and calling `Monitor` methods directly.

### First difference: Simplicity and readability

The `lock` statement is easier to read because C# manages acquiring and releasing the lock automatically.

For example:

```csharp
lock (_sync)
{
    _counter++;
}
```

This is shorter and easier to understand than manually calling `Monitor.Enter()` and `Monitor.Exit()`.

When using `Monitor` directly, we need to write additional code, usually with a `try/finally` block, to make sure the lock is released correctly.

### Second difference: Control over lock acquisition

The `lock` statement does not provide a built-in timeout for acquiring a lock. If another thread owns the lock, the current thread waits until it becomes available.

With `Monitor.TryEnter()`, we can attempt to acquire the lock for a specific amount of time.

For example, if we try to acquire a lock for 100 milliseconds and fail, we can perform another action instead of waiting longer.

This is useful when an application should remain responsive or when waiting too long for a resource is undesirable.

### Third difference: Communication between threads

The `Monitor` class provides methods such as `Wait()`, `Pulse()`, and `PulseAll()`.

These methods allow threads to coordinate their work based on changes in shared state.

For example, one thread can wait until a shared queue contains an item, while another thread adds an item and sends a notification.

The `lock` statement itself does not provide this functionality directly, so we need to use `Monitor` methods when this kind of coordination is required.

### Conclusion

In my understanding, `lock` is usually the better choice for ordinary critical sections because it is simple and safer to maintain. Direct `Monitor` calls are more useful when we need additional control over timeouts or when threads must wait for and notify each other.

## 4. When might a developer use `Monitor.TryEnter`, `Monitor.Pulse`, or `Monitor.Wait` instead of a plain `lock`?

A developer may use these methods when a normal `lock` statement does not provide enough control over thread synchronization.

### A. `Monitor.TryEnter()`

`Monitor.TryEnter()` attempts to acquire a lock without requiring the thread to wait indefinitely.

For example, imagine that an application needs to access a shared resource, but it should not spend too much time waiting for that resource. We can use `TryEnter()` with a timeout.

```csharp
if (Monitor.TryEnter(_sync, 100))
{
    try
    {
        // Access the shared resource.
    }
    finally
    {
        Monitor.Exit(_sync);
    }
}
else
{
    // The lock was not acquired within the timeout.
}
```

In this example, the thread waits for up to 100 milliseconds to acquire the lock. If it cannot acquire the lock within that time, the program can handle the situation in another way.

This method is useful when we want to limit waiting time or take an alternative action when a lock is unavailable.

### B. `Monitor.Wait()`

`Monitor.Wait()` is useful when a thread needs to wait for a particular condition before continuing.

For example, imagine a producer-consumer system. A consumer thread needs to remove an item from a shared queue, but the queue is currently empty.

Instead of repeatedly checking the queue, the consumer can call `Monitor.Wait()` and wait until another thread changes the state and notifies it.

An important detail is that `Wait()` releases the monitor lock while the thread waits. When the thread is notified, it must reacquire the lock before `Wait()` returns.

In real code, the condition should be checked in a `while` loop because the thread must verify that the required condition is actually true before continuing.

### C. `Monitor.Pulse()`

`Monitor.Pulse()` is used to notify one thread that is waiting on the same monitor.

For example, in a producer-consumer system, a producer may add an item to a shared queue. After adding the item, it can call `Pulse()` to notify a waiting consumer that the queue may no longer be empty.

The waiting thread cannot immediately continue just because it received a notification. It must first reacquire the lock.

Also, `Pulse()` and `Wait()` must be called while the current thread owns the monitor for the same object. Otherwise, a `SynchronizationLockException` can occur.

### Example of `Wait()` and `Pulse()`

The following example shows the basic idea:

```csharp
private readonly object _sync = new object();
private bool _dataAvailable = false;

public void Consume()
{
    lock (_sync)
    {
        while (!_dataAvailable)
        {
            Monitor.Wait(_sync);
        }

        // Process the available data.
        _dataAvailable = false;
    }
}

public void Produce()
{
    lock (_sync)
    {
        _dataAvailable = true;

        Monitor.Pulse(_sync);
    }
}
```

In this example, `Consume()` waits while data is unavailable. The `Produce()` method changes the shared state and notifies a waiting thread.

The `while` loop is important because a notification does not automatically guarantee that the condition is still true when the waiting thread resumes.

In summary, `TryEnter()` is useful when we need a timeout, `Wait()` is useful when a thread must wait for a condition, and `Pulse()` is useful when another thread may be able to continue after the shared state changes.

## 5. Write the same critical section twice: once with `lock`, once with `Monitor.Enter` / `Monitor.Exit`.

In both examples, we have a shared counter that multiple threads may try to increase. We want to protect the increment operation so that concurrent calls do not interfere with each other.

### Example 1: Using `lock`

```csharp
private readonly object _sync = new object();
private int _counter = 0;

public void Increment()
{
    lock (_sync)
    {
        _counter++;
    }
}
```

In this example, the `lock` statement protects the increment operation. Only one thread at a time can execute the protected section while holding the same lock object.

C# automatically handles acquiring and releasing the lock, including when an exception occurs.

### Example 2: Using `Monitor.Enter()` and `Monitor.Exit()`

```csharp
using System.Threading;

private readonly object _sync = new object();
private int _counter = 0;

public void Increment()
{
    bool lockTaken = false;

    try
    {
        Monitor.Enter(_sync, ref lockTaken);

        _counter++;
    }
    finally
    {
        if (lockTaken)
        {
            Monitor.Exit(_sync);
        }
    }
}
```

This example performs the same operation as the previous example, but we manage the monitor explicitly.

`Monitor.Enter()` acquires the lock before the counter is increased. The `finally` block ensures that the lock is released when execution leaves the protected section, including when an exception occurs.

The `lockTaken` variable helps ensure that we release the lock only if it was successfully acquired.

### Comparison of the two examples

Both examples protect the shared counter, provided that all relevant operations use the same `_sync` object.

The main difference is that `lock` handles the locking structure for us, while direct `Monitor` calls require more code and careful handling.

For a simple critical section like this one, I would normally choose `lock` because it is easier to read and maintain. I would use `Monitor` directly when I need additional synchronization features, such as a timeout or communication between waiting threads.

## Sources

1. Microsoft Learn — Monitor Class:
   https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor

2. Microsoft Learn — C# Language Specification, Lock Statement:
   https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/statements