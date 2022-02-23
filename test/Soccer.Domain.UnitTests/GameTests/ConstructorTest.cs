using System;
using FluentAssertions;
using Soccer.Domain.Exceptions;
using Xunit;

namespace Soccer.Domain.UnitTests.GameTests;

public class ConstructorTest
{
    [Fact]
    public void Given_Valid_Arguments_When_Creating_A_Game_It_Should_Be_Created()
    {
        // Given
        var id = Guid.Empty;
        var localTeamCode = "RMA";
        var awayTeamCode = "BAR";

        // When
        var sut = new Game(id, localTeamCode, awayTeamCode);

        // Then
        sut.Should().NotBeNull();
        sut.Id.Should().Be(id);
        sut.LocalTeamCode.Should().Be(localTeamCode);
        sut.AwayTeamCode.Should().Be(awayTeamCode);
        sut.IsInProgress.Should().BeFalse();
        sut.IsEnded.Should().BeFalse();
    }

    [Fact]
    public void Given_Invalid_Local_Team_When_Creating_A_Game_It_Should_Throw_An_InvalidTeamException()
    {
        // Given
        var id = Guid.Empty;
        var localTeamCode = "RM";
        var awayTeamCode = "BAR";

        // When, Then
        Assert.Throws<InvalidTeamException>(() =>
        {
            _ = new Game(id, localTeamCode, awayTeamCode);
        });
    }

    [Fact]
    public void Given_Invalid_Away_Team_When_Creating_A_Game_It_Should_Throw_An_InvalidTeamException()
    {
        // Given
        var id = Guid.Empty;
        var localTeamCode = "RMA";
        var awayTeamCode = "bar";

        // When, Then
        Assert.Throws<InvalidTeamException>(() =>
        {
            _ = new Game(id, localTeamCode, awayTeamCode);
        });
    }
}
