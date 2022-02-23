namespace Soccer.Domain.Exceptions;

public class GameNotInProgressException
    : Exception
{
    public GameNotInProgressException(Guid id)
        : base($"Game {id} has not started")
    {
    }
}
