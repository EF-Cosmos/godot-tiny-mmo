using Microsoft.AspNetCore.Mvc;
using Game.Shared.Models;
using Game.ApiGateway.Services;
using GameModel = Game.Shared.Models.Game;

namespace Game.ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly IGameService _gameService;

        public GamesController(IGameService gameService)
        {
            _gameService = gameService;
        }

        [HttpGet("{gameId}")]
        public async Task<ActionResult<GameModel>> GetGameById(Guid gameId)
        {
            try
            {
                var game = await _gameService.GetGameByIdAsync(gameId);
                return Ok(game);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<GameModel>>> GetAllGames()
        {
            try
            {
                var games = await _gameService.GetAllGamesAsync();
                return Ok(games);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<GameModel>> CreateGame([FromBody] GameModel game)
        {
            try
            {
                var createdGame = await _gameService.CreateGameAsync(game);
                return CreatedAtAction(nameof(GetGameById), new { gameId = createdGame.Id }, createdGame);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{gameId}")]
        public async Task<IActionResult> DeleteGame(Guid gameId)
        {
            try
            {
                var success = await _gameService.DeleteGameAsync(gameId);
                return success ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
