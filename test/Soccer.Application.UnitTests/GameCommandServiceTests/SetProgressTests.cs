using Moq;
using Soccer.Application.Factories;
using Soccer.Application.Models;
using Soccer.Application.UnitTests.Extensions;
using Soccer.Domain;
using Soccer.Notification.Abstractions;
using Soccer.Persistence.Abstractions;
using Xunit;

namespace Soccer.Application.UnitTests.GameCommandServiceTests;

public class SetProgressTests
{
    [Fact]
    public void Given_A_Game_When_Setting_To_Started_Then_It_Should_Save_The_Expected_In_Progress_Game()
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
        dateTimeFactoryMock
            .Setup(x => x.CreateUtcNow())
            .Returns(startedOn);
        var dateTimeFactory = dateTimeFactoryMock.Object;

        var sut = new GameCommandService(gameRepository, dateTimeFactory, notifier);
        
        var gameProgress = 
                new GameProgress
                {
                    IsInProgress = true
                };

        // When
        sut.SetProgress(gameId, gameProgress);

        // Then
        gameRepositoryMock.Verify(x => x.Upsert(It.Is<Game>(g => 
            g.IsInProgress == true &&
            g.StartedOn == startedOn &&
            g.IsEnded == false)));
    }

    [Fact]
    public void Given_An_In_Progress_Game_When_Setting_To_Ended_Then_It_Should_Save_The_Expected_Ended_Game()
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
        var endedOn = startedOn.AddHours(2);
        dateTimeFactoryMock
            .SetupSequence(x => x.CreateUtcNow())
            .Returns(startedOn)
            .Returns(endedOn);
        var dateTimeFactory = dateTimeFactoryMock.Object;

        var sut = new GameCommandService(gameRepository, dateTimeFactory, notifier);
        
        var gameProgressStart =
            new GameProgress
            {
                IsInProgress = true
            };

        sut.SetProgress(gameId, gameProgressStart);

        var gameProgressEnd =
            new GameProgress
            {
                IsInProgress = false
            };

        // When
        sut.SetProgress(gameId, gameProgressEnd);

        // Then
        gameRepositoryMock.Verify(x => x.Upsert(It.Is<Game>(g =>
            g.IsInProgress == false &&
            g.EndedOn == endedOn &&
            g.IsEnded == true)),
            Times.AtLeastOnce);
    }
}
