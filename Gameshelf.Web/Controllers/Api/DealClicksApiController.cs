using GameShelf.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace GameShelf.Web.Controllers.Api
{
    [ApiController]
    [Route("api/deal-clicks")]
    [Produces("application/json")]
    [EnableRateLimiting("AnonymousApiPolicy")]
    public class DealClicksApiController : ControllerBase
    {
        private readonly IDealClickService _clickService;
        private readonly IPlatformService _platformService;
        private readonly IAuthorizationService _authorizationService;

        public DealClicksApiController(
            IDealClickService clickService,
            IPlatformService platformService,
            IAuthorizationService authorizationService)
        {
            _clickService = clickService;
            _platformService = platformService;
            _authorizationService = authorizationService;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LogClick([FromBody] LogClickRequest request)
        {
            if (string.IsNullOrEmpty(request.DealId) || string.IsNullOrEmpty(request.StoreName) || 
                string.IsNullOrEmpty(request.GameTitle))
                return BadRequest(new { error = "DealId, StoreName and GameTitle are required" });

            if (request.DealUrl == null)
                request.DealUrl = "#";

            var userId = User.Identity?.IsAuthenticated == true 
                ? User.FindFirstValue(ClaimTypes.NameIdentifier) 
                : null;

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _clickService.LogClickAsync(
                request.DealId, 
                request.StoreName, 
                request.GameTitle, 
                request.DealUrl, 
                userId, 
                ipAddress);

            return Ok(new { message = "Click logged" });
        }

        [HttpGet("platform/{platformName}")]
        [Authorize(Roles = "Admin,PlatformOwner")]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetClicksForPlatform(string platformName)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var platform = await _platformService.GetAllAsync();
            var targetPlatform = platform.FirstOrDefault(p => string.Equals(p.Name, platformName, StringComparison.OrdinalIgnoreCase));
            if (targetPlatform == null)
                return NotFound(new { error = "Platform not found" });

            var authResult = await _authorizationService.AuthorizeAsync(User, targetPlatform.Id, "PlatformAccessPolicy");
            if (!authResult.Succeeded)
                return Forbid();

            var clicks = await _clickService.GetClicksForPlatformAsync(platformName, userId);
            return Ok(clicks);
        }

        public class LogClickRequest
        {
            public string DealId { get; set; } = string.Empty;
            public string StoreName { get; set; } = string.Empty;
            public string GameTitle { get; set; } = string.Empty;
            public string DealUrl { get; set; } = string.Empty;
        }
    }
}
