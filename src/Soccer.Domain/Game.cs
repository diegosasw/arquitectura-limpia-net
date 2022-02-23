using Soccer.Domain.Exceptions;
using Soccer.Notification.Abstractions;

namespace Soccer.Domain;

public class Game
{
    public Guid Id { get; }
    public string LocalTeamCode { get; }
    public string AwayTeamCode { get; }
    public DateTime? StartedOn { get; private set; }
    public DateTime? EndedOn { get; private set; }
    public bool IsInProgress => StartedOn.HasValue && !EndedOn.HasValue;
    public bool IsEnded => EndedOn.HasValue && StartedOn.HasValue;

    private readonly List<Goal> _localTeamGoals = new();
    public IReadOnlyCollection<Goal> LocalTeamGoals => _localTeamGoals.AsReadOnly();
    private readonly List<Goal> _awayTeamGoals = new();
    public IReadOnlyCollection<Goal> AwayTeamGoals => _awayTeamGoals.AsReadOnly();


    public Game(Guid id, string localTeamCode, string awayTeamCode)
    {
        if (!IsValidTeam(localTeamCode))
        {
            throw new InvalidTeamException(localTeamCode);
        }

        if (!IsValidTeam(awayTeamCode))
        {
            throw new InvalidTeamException(awayTeamCode);
        }

        Id = id;
        LocalTeamCode = localTeamCode;
        AwayTeamCode = awayTeamCode;
    }

    public void Start(DateTime startedOn, INotifier notifier)
    {
        if (IsInProgress)
        {
            throw new GameInProgressException(Id);
        }

        if (IsEnded)
        {
            throw new GameEndedException(Id);
        }

        StartedOn = startedOn;

        notifier
            .Notify(
                $"Game {Id} started",
                $"The game between {LocalTeamCode} and {AwayTeamCode} has started on {StartedOn}",
                LocalTeamCode,
                AwayTeamCode);
    }

    public void End(DateTime endedOn, INotifier notifier)
    {
        if (!IsInProgress)
        {
            throw new GameNotInProgressException(Id);
        }

        if (endedOn <= StartedOn)
        {
            throw new InvalidGameActionException($"The game started on {StartedOn} and cannot end prior to that time");
        }

        EndedOn = endedOn;

        notifier
            .Notify(
                $"Game {Id} ended",
                $"The game between {LocalTeamCode} and {AwayTeamCode} has ended on {EndedOn}",
                LocalTeamCode,
                AwayTeamCode);
    }

    public void ScoreGoal(Goal goal, bool isLocalTeam)
    {
        if (!IsInProgress)
        {
            throw new GameNotInProgressException(Id);
        }

        if (isLocalTeam)
        {
            _localTeamGoals.Add(goal);
        }
        else
        {
            _awayTeamGoals.Add(goal);
        }
    }
    
    private bool IsValidTeam(string teamCode)
    {
        return teamCode.Length == 3 && !teamCode.Any(char.IsLower);
    }
}
