using Soccer.Application.Factories;
using Soccer.Application.Models;
using Soccer.Domain;
using Soccer.Notification.Abstractions;
using Soccer.Persistence.Abstractions;

namespace Soccer.Application;
public class GameCommandService
{
    private readonly IGameRepository _gameRepository;
    private readonly IDateTimeFactory _dateTimeFactory;
    private readonly INotifier _notifier;

    public GameCommandService(
        IGameRepository gameRepository,
        IDateTimeFactory dateTimeFactory,
        INotifier notifier)
    {
        _gameRepository = gameRepository;
        _dateTimeFactory = dateTimeFactory;
        _notifier = notifier;
    }

    public Guid CreateGame(NewGame newGame)
    {
        var newId = Guid.NewGuid();
        var game = new Game(newId, newGame.LocalTeamCode, newGame.ForeignTeamCode);

        _gameRepository.Upsert(game);

        return game.Id;
    }

    public void SetProgress(Guid gameId, GameProgress gameProgress)
    {
        var game = _gameRepository.GetGame(gameId);
        var currentDate = _dateTimeFactory.CreateUtcNow();
        if (gameProgress.IsInProgress)
        {
            game.Start(currentDate, _notifier);
        }
        else
        {
            game.End(currentDate, _notifier);
        }

        _gameRepository.Upsert(game);
    }

    public void Score(Guid id, NewGoal newGoal)
    {
        var game = _gameRepository.GetGame(id);
        var currentDate = _dateTimeFactory.CreateUtcNow();
        var teamCode = newGoal.TeamCode;
        var goal = new Goal(currentDate, newGoal.ScoredBy);
        var isTeamPlaying = game.LocalTeamCode == teamCode || game.AwayTeamCode == teamCode;
        if (!isTeamPlaying)
        {
            throw new KeyNotFoundException($"The team code {teamCode} is not playing the game");
        }

        var isLocalTeam = game.LocalTeamCode == teamCode;
        game.ScoreGoal(goal, isLocalTeam);

        _gameRepository.Upsert(game);
    }
}
