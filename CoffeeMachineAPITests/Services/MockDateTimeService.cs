using CoffeeMachineAPI.Service;

public class MockDateTimeService : IDateTimeService
{
    private readonly DateTime _fixedDateTime;

    public MockDateTimeService(DateTime fixedDateTime)
    {
        _fixedDateTime = fixedDateTime;
    }

    public DateTime GetCurrentTime()
    {
        return _fixedDateTime;
    }
}
