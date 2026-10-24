
namespace Shared.Exceptions;

public class DataFileCorruptedException : Exception
{
    public string FileName { get; }

    public DataFileCorruptedException(string fileName)
        : base($"The JSON file '{fileName}' is corrupted.")
    {
        FileName = fileName;
    }

    public DataFileCorruptedException(string fileName, Exception innerException)
        : base($"The JSON file '{fileName}' is corrupted.", innerException)
    {
        FileName = fileName;
    }
}
