using CoffeeMachineAPI.Model;

namespace CoffeeMachineAPI.Service
{
    public interface ICoffeeService
    {
        Task<CoffeeResponse> BrewCoffee();
    }
}