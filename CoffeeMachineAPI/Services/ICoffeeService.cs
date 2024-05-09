using CoffeeMachineAPI.Model;

namespace CoffeeMachineAPI.Service
{
    public interface ICoffeeService
    {
        CoffeeResponse BrewCoffee();
    }
}