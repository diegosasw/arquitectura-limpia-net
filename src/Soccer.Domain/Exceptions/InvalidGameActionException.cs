namespace Soccer.Domain.Exceptions;

public class InvalidGameActionException
    : Exception
{
    public InvalidGameActionException(string message)
        : base(message)
    {
    }
}
