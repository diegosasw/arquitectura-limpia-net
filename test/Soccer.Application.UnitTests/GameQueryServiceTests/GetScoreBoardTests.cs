using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Soccer.Application.Mappers;
using Soccer.Application.UnitTests.Extensions;
using Soccer.Domain;
using Soccer.Notification.Abstractions;
using Soccer.Persistence.Abstractions;
using Xunit;

namespace Soccer.Application.UnitTests.GameQueryServiceTests;

public class GetScoreBoardTests
{
    [Fact]
    public void Given_A_Game_When_Getting_ScoreBoard_Then_It_Should_Return_The_Expected_Result()
    {
        // Given
        var gameId = 1.ToGuid();
        var game = new Game(gameId, "RMA", "BAR");

        var startedOn = 123.ToDateTime();
        game.Start(startedOn, Mock.Of<INotifier>());

        var goalOne = new Goal(startedOn.AddMinutes(10), "Esther");
        game.ScoreGoal(goalOne, true);

        var goalTwo = new Goal(startedOn.AddMinutes(25), "Esther");
        game.ScoreGoal(goalTwo, true);

        var goalThree = new Goal(startedOn.AddMinutes(90), "Rolfö");
        game.ScoreGoal(goalThree, false);

        var gameRepositoryMock = new Mock<IGameRepository>();
        gameRepositoryMock
            .Setup(x => x.GetGame(gameId))
            .Returns(game);
        var gameRepository = gameRepositoryMock.Object;

        var gameToScoreBoardMapper = new GameToScoreBoardMapper();
        var sut = new GameQueryService(gameRepository, gameToScoreBoardMapper);

        // When
        var result = sut.GetScoreBoard(gameId);

        // Then
        result.LocalTeam.Should().Be(game.LocalTeamCode);
        result.AwayTeam.Should().Be(game.AwayTeamCode);
        result.LocalTeamScore.Should().Be(2);
        result.AwayTeamScore.Should().Be(1);
        result.LocalTeamGoalsDetails.Should().BeEquivalentTo(new List<string> { "10' Esther", "25' Esther" });
        result.AwayTeamGoalsDetails.Should().BeEquivalentTo(new List<string> { "90' Rolfö" });
    }
}
