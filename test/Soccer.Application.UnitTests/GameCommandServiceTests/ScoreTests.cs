using System.Linq;
using Moq;
using Soccer.Application.Factories;
using Soccer.Application.Models;
using Soccer.Application.UnitTests.Extensions;
using Soccer.Domain;
using Soccer.Notification.Abstractions;
using Soccer.Persistence.Abstractions;
using Xunit;

namespace Soccer.Application.UnitTests.GameCommandServiceTests;

public class ScoreTests
{
    [Fact]
    public void Given_An_In_Progress_Game_When_Local_Team_Scores_Then_It_Should_Save_The_Expected_Game_With_Goal()
    {
        // Given
        var gameId = 1.ToGuid();
        var game = new Game(gameId, "RMA", "BAR");

        var gameRepositoryMock = new Mock<IGameRepository>();
        gameRepositoryMock
            .Setup(x => x.GetGame(gameId))
            .Returns(game);
        var gameRepository = gameRepositoryMock.Object;

        var notifierMock = new Mock<INotifier>();
        var notifier = notifierMock.Object;

        var dateTimeFactoryMock = new Mock<IDateTimeFactory>();
        var startedOn = 123.ToDateTime();
        var scoredOn = startedOn.AddMinutes(10);
        dateTimeFactoryMock
            .SetupSequence(x => x.CreateUtcNow())
            .Returns(startedOn)
            .Returns(scoredOn);
        var dateTimeFactory = dateTimeFactoryMock.Object;

        var sut = new GameCommandService(gameRepository, dateTimeFactory, notifier);

        var gameProgressStart =
            new GameProgress
            {
                IsInProgress = true
            };

        sut.SetProgress(gameId, gameProgressStart);

        var newGoal =
            new NewGoal
            {
                ScoredBy = "Esther",
                TeamCode = "RMA"
            };

        // When
        sut.Score(gameId, newGoal);

        // Then
        gameRepositoryMock.Verify(x => x.Upsert(It.Is<Game>(g =>
                g.IsInProgress == true &&
                g.LocalTeamGoals.Count == 1 &&
                g.LocalTeamGoals.Single().ScoredOn == scoredOn &&
                g.LocalTeamGoals.Single().ScoredBy == newGoal.ScoredBy &&
                g.IsEnded == false)),
            Times.AtLeastOnce);
    }
}
