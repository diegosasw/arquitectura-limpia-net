using Soccer.Domain;
using Soccer.Persistence.Abstractions;

namespace Soccer.Persistence.InMemory;
public class GameRepositoryInMemory
    : IGameRepository
{
    private readonly IDictionary<Guid, Game> _games = new Dictionary<Guid, Game>();

    public void Upsert(Game game)
    {
        _games[game.Id] = game;
    }

    public Game GetGame(Guid id)
    {
        return _games[id];
    }
}
