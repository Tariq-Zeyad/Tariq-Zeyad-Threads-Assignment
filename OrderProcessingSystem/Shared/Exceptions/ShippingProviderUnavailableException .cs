
namespace Shared.Exceptions;

/// <summary>
/// Thrown when a shipping provider is unavailable.
/// </summary>
public class ShippingProviderUnavailableException : Exception
{
    public ShippingProviderUnavailableException()
        : base("All shipping providers are currently unavailable.")
    {
    }

    public ShippingProviderUnavailableException(string message)
        : base(message)
    {
    }

    public ShippingProviderUnavailableException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
