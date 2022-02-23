using Microsoft.AspNetCore.Mvc;
using Soccer.Application;
using Soccer.Application.Models;

namespace Soccer.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class GamesController
    : ControllerBase
{
    private readonly GameCommandService _gameCommandService;
    private readonly GameQueryService _gameQueryService;

    public GamesController(
        GameCommandService gameCommandService,
        GameQueryService gameQueryService)
    {
        _gameCommandService = gameCommandService;
        _gameQueryService = gameQueryService;
    }
    
    /// <param name="id" example="00000000-0000-0000-0000-000000000000">The game id</param>
    [HttpGet("{id}")]
    public IActionResult GetScoreBoard(Guid id)
    {
        var scoreBoard = _gameQueryService.GetScoreBoard(id);
        return Ok(scoreBoard);
    }

    [HttpPost]
    public IActionResult CreateGame([FromBody] NewGame newGame)
    {
        var id = _gameCommandService.CreateGame(newGame);
        return CreatedAtAction(nameof(GetScoreBoard), new { id = id }, newGame);
    }

    /// <param name="id" example="00000000-0000-0000-0000-000000000000">The game id</param>
    /// <param name="gameProgress">The patch game object containing the isInProgress property</param>
    [HttpPatch("{id}")]
    public IActionResult StartGame(Guid id, [FromBody] GameProgress gameProgress)
    {
        _gameCommandService.SetProgress(id, gameProgress);
        return Ok();
    }

    /// <param name="id" example="00000000-0000-0000-0000-000000000000">The game id</param>
    /// <param name="newGoal">The new goal containing the team that scores and the scorer</param>
    [HttpPost("{id}/goals")]
    public IActionResult AddGoal(Guid id, [FromBody] NewGoal newGoal)
    {
        _gameCommandService.Score(id, newGoal);
        return Ok();
    }
}
