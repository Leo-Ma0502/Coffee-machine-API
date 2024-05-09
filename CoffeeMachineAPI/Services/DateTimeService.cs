namespace CoffeeMachineAPI.Service
{
    public class DateTimeService : IDateTimeService
    {
        public DateTime GetCurrentTime() => DateTime.UtcNow;
    }
}