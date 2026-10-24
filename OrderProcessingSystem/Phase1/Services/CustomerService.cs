
using Phase1.Interfaces;
using Shared.Logging;
using Shared.Models;

namespace Phase1.Services;

public class CustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<List<Customer>> GetAllCustomersAsync(
        CancellationToken ct = default)
    {
        Log.Info(
            "CustomerService",
            "Getting all customers...");

        await Task.Delay(100, ct);

        List<Customer> customers =
            await _customerRepository.GetAllAsync(ct);

        await Task.Delay(100, ct);

        Log.Success(
            "CustomerService",
            $"Retrieved {customers.Count} customers.");

        return customers;
    }

    public async Task<Customer> GetActiveCustomerAsync(
        int customerId,
        CancellationToken ct = default)
    {
        Log.Info(
            "CustomerService",
            $"Validating customer #{customerId}...");

        await Task.Delay(100, ct);

        Customer? customer =
            await _customerRepository.GetByIdAsync(customerId, ct);

        await Task.Delay(100, ct);

        if (customer is null)
        {
            throw new InvalidOperationException(
                $"Customer #{customerId} does not exist.");
        }

        if (!customer.IsActive)
        {
            throw new InvalidOperationException(
                $"Customer #{customerId} is inactive.");
        }

        Log.Success(
            "CustomerService",
            $"Customer #{customerId} is active.");

        return customer;
    }
}
