using System.Security.Claims;
using GameShelf.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameShelf.Web.Controllers.Api
{
    [ApiController]
    [Route("api/savings-cart")]
    [Authorize]
    public class SavingsCartApiController : ControllerBase
    {
        private readonly ISavingsCartService _savingsCartService;

        public SavingsCartApiController(ISavingsCartService savingsCartService)
        {
            _savingsCartService = savingsCartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var summary = await _savingsCartService.GetSummaryAsync(userId);
            return Ok(summary);
        }

        [HttpPost("{gameDealId:guid}")]
        public async Task<IActionResult> Add(Guid gameDealId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            await _savingsCartService.AddAsync(userId, gameDealId);
            return NoContent();
        }

        [HttpDelete("{gameDealId:guid}")]
        public async Task<IActionResult> Remove(Guid gameDealId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            await _savingsCartService.RemoveAsync(userId, gameDealId);
            return NoContent();
        }
    }
}
