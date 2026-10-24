
using Phase1.Interfaces;
using Shared.Exceptions;
using Shared.Models;

namespace ECommerceOrderProcessing.OrderProcessingSystem.Phase3.Services;

public class CustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Customer> GetCustomerAsync(int id)
    {
        await Task.Delay(200);

        var customer = await _customerRepository.GetByIdAsync(id);

        if (customer is null)
        {
            throw new CustomerNotFoundException(
                $"Customer #{id} not found");
        }

        return customer;
    }
}
