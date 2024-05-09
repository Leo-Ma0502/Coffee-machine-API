using CoffeeMachineAPI.Model;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeMachineAPI.Service
{
    public interface ICoffeeService
    {
        Task<CoffeeResponse> BrewCoffee([FromBody] Location? location);
    }
}