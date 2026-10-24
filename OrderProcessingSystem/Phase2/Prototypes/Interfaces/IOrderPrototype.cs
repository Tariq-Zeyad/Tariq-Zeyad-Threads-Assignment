using Shared.Models;

namespace Phase2.Prototypes.Interfaces;

public interface IOrderPrototype
{
    Order Clone(Order order);
}