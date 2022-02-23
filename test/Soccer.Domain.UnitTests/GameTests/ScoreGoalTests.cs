using System;
using System.Linq;
using FluentAssertions;
using Moq;
using Soccer.Domain.Exceptions;
using Soccer.Notification.Abstractions;
using Xunit;

namespace Soccer.Domain.UnitTests.GameTests;

public class ScoreGoalTests
{
    [Fact]
    public void Given_A_Game_In_Progress_When_Local_Team_Scores_Then_Local_Team_Goal_Is_Added()
    {
        // Given
        var id = Guid.Empty;
        var localTeamCode = "RMA";
        var awayTeamCode = "BAR";
        var sut = new Game(id, localTeamCode, awayTeamCode);

        var startedOn = new DateTime(2022, 3, 1, 18, 0, 0);
        var notifierMock = new Mock<INotifier>();
        var notifier = notifierMock.Object;
        sut.Start(startedOn, notifier);

        var scoredOn = startedOn.AddMinutes(20);
        var scoredBy = "Esther";
        var goal = new Goal(scoredOn, scoredBy);

        // When
        sut.ScoreGoal(goal, true);

        // Then
        sut.LocalTeamGoals.Count.Should().Be(1);
        sut.AwayTeamGoals.Count.Should().Be(0);
        sut.LocalTeamGoals.Single().Should().Be(goal);
    }

    [Fact]
    public void Given_A_Game_In_Progress_When_Away_Team_Scores_Then_Away_Team_Goal_Is_Added()
    {
        // Given
        var id = Guid.Empty;
        var localTeamCode = "RMA";
        var awayTeamCode = "BAR";
        var sut = new Game(id, localTeamCode, awayTeamCode);

        var startedOn = new DateTime(2022, 3, 1, 18, 0, 0);
        var notifierMock = new Mock<INotifier>();
        var notifier = notifierMock.Object;
        sut.Start(startedOn, notifier);

        var scoredOn = startedOn.AddMinutes(20);
        var scoredBy = "Rolfö";
        var goal = new Goal(scoredOn, scoredBy);

        // When
        sut.ScoreGoal(goal, false);

        // Then
        sut.LocalTeamGoals.Count.Should().Be(0);
        sut.AwayTeamGoals.Count.Should().Be(1);
        sut.AwayTeamGoals.Single().Should().Be(goal);
    }

    [Fact]
    public void Given_A_Game_Not_Started_When_Local_Team_Scores_Then_It_Should_Throw_GameNotInProgressException()
    {
        // Given
        var id = Guid.Empty;
        var localTeamCode = "RMA";
        var awayTeamCode = "BAR";
        var sut = new Game(id, localTeamCode, awayTeamCode);

        var scoredOn = new DateTime(2022, 3, 1, 18, 0, 0);
        var scoredBy = "Esther";
        var goal = new Goal(scoredOn, scoredBy);

        // When
        
        // When, Then
        Assert.Throws<GameNotInProgressException>(() =>
        {
            sut.ScoreGoal(goal, true);
        });

        // Then
        sut.LocalTeamGoals.Count.Should().Be(0);
        sut.AwayTeamGoals.Count.Should().Be(0);
    }

    [Fact]
    public void Given_A_Game_Ended_When_Local_Team_Scores_Then_It_Should_Throw_GameNotInProgressException()
    {
        // Given
        var id = Guid.Empty;
        var localTeamCode = "RMA";
        var awayTeamCode = "BAR";
        var sut = new Game(id, localTeamCode, awayTeamCode);
        var startedOn = new DateTime(2022, 3, 1, 18, 0, 0);
        var notifierMock = new Mock<INotifier>();
        var notifier = notifierMock.Object;
        sut.Start(startedOn, notifier);
        var endedOn = startedOn.AddHours(2);
        sut.End(endedOn, notifier);

        var scoredOn = endedOn.AddSeconds(1);
        var scoredBy = "Esther";
        var goal = new Goal(scoredOn, scoredBy);
        
        // When, Then
        Assert.Throws<GameNotInProgressException>(() =>
        {
            sut.ScoreGoal(goal, true);
        });

        // Then
        sut.LocalTeamGoals.Count.Should().Be(0);
        sut.AwayTeamGoals.Count.Should().Be(0);
    }
}
