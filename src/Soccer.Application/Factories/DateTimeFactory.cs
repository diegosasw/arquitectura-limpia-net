namespace Soccer.Application.Factories;

public class DateTimeFactory
    : IDateTimeFactory
{
    public DateTime CreateUtcNow()
    {
        return DateTime.UtcNow;
    }
}
