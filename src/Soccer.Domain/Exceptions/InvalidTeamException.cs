namespace Soccer.Domain.Exceptions;

public class InvalidTeamException
    : Exception
{
    public InvalidTeamException(string teamCode)
        : base($"Invalid team code {teamCode}")
    {
    }
}
