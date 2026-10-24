
using System;

namespace Shared.Logging;

public static class Log
{
    private static readonly object ConsoleLock = new();

    public static void Info(string source, string message)
    {
        Write(source, message, ConsoleColor.Gray);
    }

    public static void Success(string source, string message)
    {
        Write(source, message, ConsoleColor.Green);
    }

    public static void Error(string source, string message)
    {
        Write(source, message, ConsoleColor.Red);
    }

    private static void Write(
        string source,
        string message,
        ConsoleColor color)
    {
        lock (ConsoleLock)
        {
            ConsoleColor originalColor = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = color;

                string time = DateTime.Now.ToString("HH:mm:ss.fff");
                int threadId = Environment.CurrentManagedThreadId;

                Console.WriteLine(
                    $"[{time}] [Thread {threadId}] {source} -> {message}");
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }
}
