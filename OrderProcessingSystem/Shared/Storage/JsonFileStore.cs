
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Exceptions;

namespace Shared.Storage;

public static class JsonFileStore
{
    private const int MaxMoveAttempts = 3;
    private const int RetryDelayMilliseconds = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly string DataDirectory =
        Path.Combine(AppContext.BaseDirectory, "Data");

    private static readonly SemaphoreSlim FileLock = new(1, 1);

    public static async Task<List<T>> ReadAsync<T>(
        string fileName,
        CancellationToken ct = default)
    {
        await FileLock.WaitAsync(ct);

        try
        {
            string path = GetPath(fileName);

            if (!File.Exists(path))
                return new List<T>();

            string json = await File.ReadAllTextAsync(path, ct);

            return Deserialize<T>(json, fileName);
        }
        finally
        {
            FileLock.Release();
        }
    }

    public static async Task WriteAsync<T>(
        string fileName,
        List<T> records,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(records);

        await FileLock.WaitAsync(ct);

        string? tempPath = null;

        try
        {
            ct.ThrowIfCancellationRequested();

            Directory.CreateDirectory(DataDirectory);

            string path = GetPath(fileName);
            tempPath = path + $".{Guid.NewGuid():N}.tmp";

            string json = JsonSerializer.Serialize(records, JsonOptions);

            await File.WriteAllTextAsync(tempPath, json, ct);

            await MoveFileAsync(tempPath, path, ct);
            tempPath = null;
        }
        finally
        {
            try
            {
                DeleteTempFile(tempPath);
            }
            finally
            {
                FileLock.Release();
            }
        }
    }

    public static List<T> Read<T>(string fileName)
    {
        FileLock.Wait();

        try
        {
            string path = GetPath(fileName);

            if (!File.Exists(path))
                return new List<T>();

            string json = File.ReadAllText(path);

            return Deserialize<T>(json, fileName);
        }
        finally
        {
            FileLock.Release();
        }
    }

    public static void Write<T>(string fileName, List<T> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        FileLock.Wait();

        string? tempPath = null;

        try
        {
            Directory.CreateDirectory(DataDirectory);

            string path = GetPath(fileName);
            tempPath = path + $".{Guid.NewGuid():N}.tmp";

            string json = JsonSerializer.Serialize(records, JsonOptions);

            File.WriteAllText(tempPath, json);

            File.Move(tempPath, path, overwrite: true);
            tempPath = null;
        }
        finally
        {
            try
            {
                DeleteTempFile(tempPath);
            }
            finally
            {
                FileLock.Release();
            }
        }
    }

    private static async Task MoveFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken ct)
    {
        for (int attempt = 1; ; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                File.Move(sourcePath, destinationPath, overwrite: true);
                return;
            }
            catch (IOException) when (attempt < MaxMoveAttempts)
            {
                await Task.Delay(RetryDelayMilliseconds, ct);
            }
            catch (UnauthorizedAccessException) when (attempt < MaxMoveAttempts)
            {
                await Task.Delay(RetryDelayMilliseconds, ct);
            }
        }
    }

    private static void DeleteTempFile(string? tempPath)
    {
        if (tempPath is not null && File.Exists(tempPath))
            File.Delete(tempPath);
    }

    private static List<T> Deserialize<T>(
        string json,
        string fileName)
    {
        try
        {
            return JsonSerializer.Deserialize<List<T>>(
                       json,
                       JsonOptions)
                   ?? new List<T>();
        }
        catch (JsonException ex)
        {
            throw new DataFileCorruptedException(fileName, ex);
        }
    }

    private static string GetPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "File name cannot be empty.",
                nameof(fileName));
        }

        if (fileName != Path.GetFileName(fileName) ||
            fileName is "." or ".." ||
            fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only file names are allowed, not paths.",
                nameof(fileName));
        }

        return Path.Combine(DataDirectory, fileName);
    }
}
