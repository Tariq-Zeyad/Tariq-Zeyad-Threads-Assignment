# Assignment 04 — MenuViews Locking Research

## R1, R2, R3 — Expected vs Actual

### Why did Expected and Actual disagree?

The expected count is 400,000 because there are 8 threads, and each thread registers 50,000 views.

In the original code, the `RegisterView()` method used `Views++` without any synchronization. The problem is that `Views++` is not an atomic operation. It reads the current value, increases it, and then writes the new value back.

When multiple threads execute this operation at the same time, one thread can overwrite another thread's update. This causes some increments to be lost, which is called a race condition.

Because of this problem, the actual count could be lower than the expected count.

### How did I fix RegisterView?

I changed the counter to use `Interlocked.Increment()` instead of `Views++`.

```csharp
using System.Threading;

namespace MenuViews;

public sealed class MenuViewCounter
{
    private int _views;

    public int Views => _views;

    public void RegisterView()
    {
        Interlocked.Increment(ref _views);
    }
}
```

The `Interlocked.Increment()` method increases the counter as an atomic operation. This means that multiple threads can increment the same counter safely without losing updates.

I also changed the counter field to `private` so that other parts of the program cannot modify it directly. The `Views` property allows the program to read the counter's value.

The test program uses `Join()` to wait for all threads to finish before printing the result.

After running the program, the expected output is:

```text
Expected: 400000
Actual:   400000
```

Both numbers should match on every run because each increment is performed atomically.

### Why did I choose Interlocked instead of lock?

I chose `Interlocked.Increment()` because this assignment only requires increasing a single integer counter.

Using `lock` would also solve the race condition, but it requires a lock object and a protected code block.

`Interlocked.Increment()` is simpler for this situation because it performs the increment atomically without needing an explicit lock.

For a single counter, I think `Interlocked.Increment()` is the right choice because it solves the problem with less code.

## R4 — When would I protect this update differently?

I would use `lock` if registering a view required updating multiple shared variables that needed to remain consistent together.

For example, if registering a view required increasing the total view count and updating another related value as part of the same operation, I could use `lock` to protect both operations.

`Interlocked.Increment()` makes the increment of one integer atomic, but it does not make multiple separate operations atomic as a group.

In this assignment, I chose `Interlocked.Increment()` because it is suitable for a simple counter. For more complex operations involving shared data, I would consider using `lock`.