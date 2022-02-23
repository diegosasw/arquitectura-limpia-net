namespace Soccer.Domain.Exceptions;

public class GameInProgressException
    : Exception
{
    public GameInProgressException(Guid id)
        : base($"Game {id} already in progress")
    {
    }
}
