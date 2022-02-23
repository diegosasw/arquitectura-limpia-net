using Soccer.Domain;

namespace Soccer.Persistence.Abstractions;
public interface IGameRepository
{
    void Upsert(Game game);
    Game GetGame(Guid id);
}
