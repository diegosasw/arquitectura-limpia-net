namespace Soccer.Application.Models;

public class ScoreBoard
{
    private readonly IList<GoalDescription> _localTeamGoals;
    private readonly IList<GoalDescription> _awayTeamGoals;
    public string LocalTeam { get; }
    public int LocalTeamScore => _localTeamGoals.Count;
    public string AwayTeam { get; }
    public int AwayTeamScore => _awayTeamGoals.Count;
    public string Result => $"{LocalTeam} {LocalTeamScore} - {AwayTeamScore} {AwayTeam}";
    public IEnumerable<string> LocalTeamGoalsDetails => _localTeamGoals.Select(Details);
    public IEnumerable<string> AwayTeamGoalsDetails => _awayTeamGoals.Select(Details);
    private string Details(GoalDescription goalDescription) => $"{goalDescription.Minute}' {goalDescription.Player}";

    public ScoreBoard(
        string localTeam,
        IList<GoalDescription> localTeamGoals,
        string awayTeam,
        IList<GoalDescription> awayTeamGoals)
    {
        _localTeamGoals = localTeamGoals;
        _awayTeamGoals = awayTeamGoals;
        LocalTeam = localTeam;
        AwayTeam = awayTeam;
    }
}

public class GoalDescription
{
    public int Minute { get; }
    public string Player { get; }

    public GoalDescription(int minute, string player)
    {
        Minute = minute;
        Player = player;
    }
}