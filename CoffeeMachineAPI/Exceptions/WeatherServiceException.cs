namespace CoffeeMachineAPI.Exceptions
{
    public class WeatherServiceException : Exception
    {
        public WeatherServiceException(string message) : base(message)
        {
        }
    }
}