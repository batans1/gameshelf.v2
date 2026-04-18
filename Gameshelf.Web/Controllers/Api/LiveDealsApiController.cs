using GameShelf.Business.Services.Interfaces;
using GameShelf.Models.ViewModels.LiveDeals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameShelf.Web.Controllers.Api;

[ApiController]
[Route("api/live-deals")]
[Produces("application/json")]
[EnableRateLimiting("AnonymousApiPolicy")]
public class LiveDealsApiController : ControllerBase
{
    private readonly IExternalDealsService _externalDealsService;

    public LiveDealsApiController(IExternalDealsService externalDealsService)
    {
        _externalDealsService = externalDealsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LiveDealDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<LiveDealDto>>> GetLiveDeals(
        [FromQuery] string platform,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 40)
    {
        if (string.IsNullOrWhiteSpace(platform))
            return BadRequest(new { error = "Platform is required (e.g. Steam, GOG, Epic Games, EA App)." });

        var size = Math.Clamp(pageSize, 1, 60);
        var page = Math.Max(1, pageNumber);
        var deals = await _externalDealsService.GetLiveDealsAsync(platform.Trim(), page, size);
        return Ok(deals);
    }

    [HttpGet("all")]
    [ProducesResponseType(typeof(IEnumerable<LiveDealDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LiveDealDto>>> GetAllPlatformsDeals(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 40)
    {
        var size = Math.Clamp(pageSize, 1, 60);
        var page = Math.Max(1, pageNumber);
        var deals = await _externalDealsService.GetLiveDealsAllPlatformsAsync(page, size);
        return Ok(deals);
    }

    [HttpGet("featured")]
    [ProducesResponseType(typeof(IEnumerable<LiveDealDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LiveDealDto>>> GetFeaturedGames(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 40)
    {
        var size = Math.Clamp(pageSize, 1, 60);
        var page = Math.Max(1, pageNumber);
        var deals = await _externalDealsService.GetFeaturedDealsAsync(page, size);
        return Ok(deals);
    }
}