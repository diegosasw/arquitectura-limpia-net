using Soccer.Application.Models;
using Soccer.Domain;

namespace Soccer.Application.Mappers;

public class GameToScoreBoardMapper
{
    public ScoreBoard Map(Game game)
    {
        var startedOn = game.StartedOn;
        var localGoalDescriptions = game.LocalTeamGoals.Select(x => ToGoalDescription(startedOn, x)).ToList();
        var awayGoalDescriptions = game.AwayTeamGoals.Select(x => ToGoalDescription(startedOn, x)).ToList();
        var scoreBoard = 
            new ScoreBoard(
                game.LocalTeamCode,
                localGoalDescriptions,
                game.AwayTeamCode,
                awayGoalDescriptions);

        return scoreBoard;
    }

    private GoalDescription ToGoalDescription(DateTime? startedOn, Goal goal)
    {
        if (startedOn == null)
        {
            throw new ArgumentNullException(nameof(startedOn));
        }
        var scoredOn = goal.ScoredOn;
        var timeSpan = scoredOn.Subtract(startedOn.Value);
        var goalDescription = new GoalDescription((int)timeSpan.TotalMinutes, goal.ScoredBy);
        return goalDescription;
    }
}
