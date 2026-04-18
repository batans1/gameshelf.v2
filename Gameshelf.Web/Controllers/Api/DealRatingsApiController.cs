using GameShelf.Business.Services.Interfaces;
using GameShelf.Business.Services.Moderation;
using GameShelf.Models.Domain.Entities;
using GameShelf.Models.ViewModels.DealRatings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace GameShelf.Web.Controllers.Api
{
    [ApiController]
    [Route("api/deal-ratings")]
    [Produces("application/json")]
    [EnableRateLimiting("AuthenticatedApiPolicy")]
    public class DealRatingsApiController : ControllerBase
    {
        private readonly IDealRatingService _dealRatingService;
       

        public DealRatingsApiController(IDealRatingService dealRatingService)
        {
            _dealRatingService = dealRatingService;
            
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SetRating([FromBody] SetDealRatingRequest request)
        {
            if (string.IsNullOrEmpty(request.StoreName))
                return BadRequest(new { error = "StoreName is required" });

            if (string.IsNullOrEmpty(request.DealId) && !request.CustomDealId.HasValue)
                return BadRequest(new { error = "Either DealId or CustomDealId is required" });

            if (!Enum.IsDefined(typeof(DealVerdict), request.Verdict))
                return BadRequest(new { error = "Invalid verdict" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            

            try
            {
                var moderationOutcome = await _dealRatingService.SetRatingAsync(
                    request.DealId,
                    request.CustomDealId,
                    request.StoreName,
                    userId,
                    request.Verdict,
                    request.ReasonId,
                    request.ReviewText);

                return Ok(new
                {
                    message = "Rating saved",
                    moderationWarning = moderationOutcome?.ContainsProfanity == true ? moderationOutcome.UserMessage : null
                });
            }
            catch (ReviewModerationException ex)
            {
                return StatusCode(ex.StatusCode, new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Database or server error. Ensure migrations are applied: " + ex.Message });
            }
        }

        [HttpDelete("{ratingId:guid}/review-text")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteReviewText(Guid ratingId)
        {
            await _dealRatingService.DeleteReviewTextAsync(ratingId);
            return NoContent();
        }

        [HttpGet("{dealId}")]
        [ProducesResponseType(typeof(DealRatingResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRating(string dealId)
        {
            var communityVerdict = await _dealRatingService.GetCommunityVerdictAsync(dealId, null);
            
            DealRatingViewModel? userRating = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    userRating = await _dealRatingService.GetUserRatingAsync(dealId, null, userId);
                }
            }

            return Ok(new DealRatingResponse
            {
                CommunityVerdict = new CommunityVerdictResponse
                {
                    BuyNow = new DealVerdictStatResponse
                    {
                        Verdict = DealVerdict.BuyNow,
                        Count = communityVerdict.BuyNow.Count,
                        Percentage = communityVerdict.BuyNow.Percentage,
                        ReviewCount = communityVerdict.BuyNow.ReviewCount
                    },
                    Wait = new DealVerdictStatResponse
                    {
                        Verdict = DealVerdict.Wait,
                        Count = communityVerdict.Wait.Count,
                        Percentage = communityVerdict.Wait.Percentage,
                        ReviewCount = communityVerdict.Wait.ReviewCount
                    },
                    NotWorthIt = new DealVerdictStatResponse
                    {
                        Verdict = DealVerdict.NotWorthIt,
                        Count = communityVerdict.NotWorthIt.Count,
                        Percentage = communityVerdict.NotWorthIt.Percentage,
                        ReviewCount = communityVerdict.NotWorthIt.ReviewCount
                    },
                    TotalRatings = communityVerdict.TotalRatings
                },
                UserVerdict = userRating?.UserVerdict,
                UserReasonId = userRating?.UserReasonId,
                UserReviewText = userRating?.UserReviewText
            });
        }

        [HttpGet("custom/{customDealId}")]
        [ProducesResponseType(typeof(DealRatingResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCustomDealRating(Guid customDealId)
        {
            var communityVerdict = await _dealRatingService.GetCommunityVerdictAsync(null, customDealId);
            
            DealRatingViewModel? userRating = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    userRating = await _dealRatingService.GetUserRatingAsync(null, customDealId, userId);
                }
            }

            return Ok(new DealRatingResponse
            {
                CommunityVerdict = new CommunityVerdictResponse
                {
                    BuyNow = new DealVerdictStatResponse
                    {
                        Verdict = DealVerdict.BuyNow,
                        Count = communityVerdict.BuyNow.Count,
                        Percentage = communityVerdict.BuyNow.Percentage,
                        ReviewCount = communityVerdict.BuyNow.ReviewCount
                    },
                    Wait = new DealVerdictStatResponse
                    {
                        Verdict = DealVerdict.Wait,
                        Count = communityVerdict.Wait.Count,
                        Percentage = communityVerdict.Wait.Percentage,
                        ReviewCount = communityVerdict.Wait.ReviewCount
                    },
                    NotWorthIt = new DealVerdictStatResponse
                    {
                        Verdict = DealVerdict.NotWorthIt,
                        Count = communityVerdict.NotWorthIt.Count,
                        Percentage = communityVerdict.NotWorthIt.Percentage,
                        ReviewCount = communityVerdict.NotWorthIt.ReviewCount
                    },
                    TotalRatings = communityVerdict.TotalRatings
                },
                UserVerdict = userRating?.UserVerdict,
                UserReasonId = userRating?.UserReasonId,
                UserReviewText = userRating?.UserReviewText
            });
        }

        [HttpGet("{dealId}/reviews")]
        [ProducesResponseType(typeof(IEnumerable<DealReviewResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReviews(string dealId, [FromQuery] DealVerdict? verdict = null)
        {
            // Try parsing verdict from query string if it's a string
            if (verdict == null && Request.Query.ContainsKey("verdict"))
            {
                var verdictStr = Request.Query["verdict"].ToString();
                if (Enum.TryParse<DealVerdict>(verdictStr, ignoreCase: true, out var parsedVerdict))
                {
                    verdict = parsedVerdict;
                }
            }
            
            var reviews = await _dealRatingService.GetDealReviewsAsync(dealId, null, verdict);
            return Ok(reviews.Select(r => new DealReviewResponse
            {
                Id = r.Id,
                Verdict = r.Verdict,
                ReasonId = r.ReasonId,
                ReasonText = r.ReasonText,
                ReviewText = r.ReviewText,
                UserName = r.UserName,
                CreatedAt = r.CreatedAt
            }));
        }

        [HttpGet("custom/{customDealId}/reviews")]
        [ProducesResponseType(typeof(IEnumerable<DealReviewResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCustomDealReviews(Guid customDealId, [FromQuery] DealVerdict? verdict = null)
        {
            // Try parsing verdict from query string if it's a string
            if (verdict == null && Request.Query.ContainsKey("verdict"))
            {
                var verdictStr = Request.Query["verdict"].ToString();
                if (Enum.TryParse<DealVerdict>(verdictStr, ignoreCase: true, out var parsedVerdict))
                {
                    verdict = parsedVerdict;
                }
            }
            
            var reviews = await _dealRatingService.GetDealReviewsAsync(null, customDealId, verdict);
            return Ok(reviews.Select(r => new DealReviewResponse
            {
                Id = r.Id,
                Verdict = r.Verdict,
                ReasonId = r.ReasonId,
                ReasonText = r.ReasonText,
                ReviewText = r.ReviewText,
                UserName = r.UserName,
                CreatedAt = r.CreatedAt
            }));
        }

        public class SetDealRatingRequest
        {
            public string? DealId { get; set; }
            public Guid? CustomDealId { get; set; }
            public string StoreName { get; set; } = string.Empty;
            public DealVerdict Verdict { get; set; }
            public int ReasonId { get; set; }
            public string? ReviewText { get; set; }
        }

        public class DealRatingResponse
        {
            public CommunityVerdictResponse CommunityVerdict { get; set; } = new();
            public DealVerdict? UserVerdict { get; set; }
            public int? UserReasonId { get; set; }
            public string? UserReviewText { get; set; }
        }

        public class CommunityVerdictResponse
        {
            public DealVerdictStatResponse BuyNow { get; set; } = new() { Verdict = DealVerdict.BuyNow };
            public DealVerdictStatResponse Wait { get; set; } = new() { Verdict = DealVerdict.Wait };
            public DealVerdictStatResponse NotWorthIt { get; set; } = new() { Verdict = DealVerdict.NotWorthIt };
            public int TotalRatings { get; set; }
        }

        public class DealVerdictStatResponse
        {
            public DealVerdict Verdict { get; set; }
            public int Count { get; set; }
            public double Percentage { get; set; }
            public int ReviewCount { get; set; }
        }

        public class DealReviewResponse
        {
            public Guid Id { get; set; }
            public DealVerdict Verdict { get; set; }
            public int ReasonId { get; set; }
            public string ReasonText { get; set; } = string.Empty;
            public string? ReviewText { get; set; }
            public string UserName { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }
    }
}
