# Answers

## 1. Initial Run and Observed Problems

When I first ran the project, exceptions appeared during execution. I did not record the exact exception messages, so I cannot identify their precise causes from that run.

The view-counting test also produced an incorrect result:

```text
Views expected=50000, actual=19687
```

This showed that the actual view count was lower than the expected count.

The email test produced:

```text
Email jobs placed=20, unique sent=20, total sends=20
```

This indicated that all 20 email jobs were sent without duplicates in that run.

## 2. Concurrency Fixes

The view-counting operation should use `ConcurrentDictionary.AddOrUpdate()` to update counts safely when multiple threads record views simultaneously.

The email jobs should use `ConcurrentQueue.TryDequeue()` so workers can safely retrieve jobs. Access to the shared `SentEmails` list should be protected by a lock.

## 3. Why Thread-Safe Collections Are Not Enough

Thread-safe collections protect their individual operations, but they do not automatically make every multi-step business operation safe. The application must still coordinate updates, shared data access, and worker shutdown correctly.

## 4. Verification

After applying the fixes, run the project again and verify that the output is:

```text
Views expected=50000, actual=50000
Email jobs placed=20, unique sent=20, total sends=20
```

These are the expected results after the fixes, not confirmed results until the project is rerun.