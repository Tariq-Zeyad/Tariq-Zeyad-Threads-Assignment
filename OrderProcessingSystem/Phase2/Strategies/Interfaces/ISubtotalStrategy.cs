using Shared.Models;

namespace Phase2.Strategies.Interfaces;

public interface ISubtotalStrategy
{
    decimal Calculate(Order order);
}