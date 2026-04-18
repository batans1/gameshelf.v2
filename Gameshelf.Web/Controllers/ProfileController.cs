using System.Security.Claims;
using GameShelf.Business.Services.Interfaces;
using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using GameShelf.Models.ViewModels.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameShelf.Web.Controllers
{
    public class ProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDealRatingService _dealRatingService;
        private readonly ISavingsCartService _savingsCartService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _env;

        public ProfileController(
            UserManager<IdentityUser> userManager,
            IDealRatingService dealRatingService,
            ISavingsCartService savingsCartService,
            ApplicationDbContext dbContext,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _dealRatingService = dealRatingService;
            _savingsCartService = savingsCartService;
            _dbContext = dbContext;
            _env = env;
        }

        [Authorize]
        [HttpGet("/profiles/me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(userName))
                return Challenge();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Challenge();
            var profile = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

            var model = new PublicProfileViewModel
            {
                UserId = userId,
                UserName = user.UserName ?? userName,
                AvatarUrl = BuildAvatarUrl(user.UserName ?? userName, profile?.AvatarPath),
                HasCustomAvatar = !string.IsNullOrWhiteSpace(profile?.AvatarPath),
                Reviews = (await _dealRatingService.GetUserReviewsAsync(userId, includeWithoutText: true)).ToList()
            };
            model.TotalRatings = model.Reviews.Count;
            model.TextReviewsCount = model.Reviews.Count(r => !string.IsNullOrWhiteSpace(r.ReviewText));

            ViewData["SavingsCart"] = await _savingsCartService.GetSummaryAsync(userId);
            return View(model);
        }

        [Authorize]
        [HttpPost("/profiles/me")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMyProfile(string? userName, IFormFile? avatarFile, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Challenge();

            if (!string.IsNullOrWhiteSpace(userName) && !string.Equals(user.UserName, userName, StringComparison.Ordinal))
            {
                userName = userName.Trim();
                if (userName.Length < 3 || userName.Length > 32)
                {
                    TempData["ProfileError"] = "Username must be between 3 and 32 characters.";
                    return RedirectToAction(nameof(Me));
                }

                var existing = await _userManager.FindByNameAsync(userName);
                if (existing != null && existing.Id != userId)
                {
                    TempData["ProfileError"] = "This username is already taken.";
                    return RedirectToAction(nameof(Me));
                }

                var result = await _userManager.SetUserNameAsync(user, userName);
                if (!result.Succeeded)
                {
                    TempData["ProfileError"] = string.Join(" ", result.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Me));
                }
            }

            if (avatarFile is { Length: > 0 })
            {
                var ext = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (!allowed.Contains(ext))
                {
                    TempData["ProfileError"] = "Allowed avatar formats: JPG, PNG, GIF, WebP.";
                    return RedirectToAction(nameof(Me));
                }

                var avatarDir = Path.Combine(_env.WebRootPath, "images", "avatars");
                Directory.CreateDirectory(avatarDir);
                var fileName = $"{userId}{ext}";
                var path = Path.Combine(avatarDir, fileName);
                await using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    await avatarFile.CopyToAsync(stream, cancellationToken);
                }

                var profile = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
                if (profile == null)
                {
                    profile = new UserProfile { UserId = userId };
                    _dbContext.UserProfiles.Add(profile);
                }
                profile.AvatarPath = $"/images/avatars/{fileName}";
                profile.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            TempData["ProfileSuccess"] = "Profile updated.";
            return RedirectToAction(nameof(Me));
        }

        [Authorize]
        [HttpPost("/profiles/me/remove-avatar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMyAvatar(CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            DeleteAvatarFilesForUser(userId);

            var profile = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
            if (profile != null)
            {
                profile.AvatarPath = null;
                profile.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            TempData["ProfileSuccess"] = "Avatar removed.";
            return RedirectToAction(nameof(Me));
        }

        [HttpGet("/profile/{username}")]
        public async Task<IActionResult> Public(string username, bool includeNoText = false)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return NotFound();
            var profile = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            var reviews = (await _dealRatingService.GetUserReviewsAsync(user.Id, includeNoText)).ToList();
            var model = new PublicProfileViewModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? username,
                AvatarUrl = BuildAvatarUrl(user.UserName ?? username, profile?.AvatarPath),
                HasCustomAvatar = !string.IsNullOrWhiteSpace(profile?.AvatarPath),
                Reviews = reviews,
                TotalRatings = reviews.Count,
                TextReviewsCount = reviews.Count(r => !string.IsNullOrWhiteSpace(r.ReviewText))
            };

            ViewData["IncludeNoText"] = includeNoText;
            return View("Public", model);
        }

        private static string BuildAvatarUrl(string userName, string? avatarPath)
            => string.IsNullOrWhiteSpace(avatarPath)
                ? $"https://api.dicebear.com/9.x/initials/svg?seed={Uri.EscapeDataString(userName)}"
                : avatarPath;

        private void DeleteAvatarFilesForUser(string userId)
        {
            var avatarDir = Path.Combine(_env.WebRootPath, "images", "avatars");
            if (!Directory.Exists(avatarDir)) return;

            foreach (var file in Directory.GetFiles(avatarDir, $"{userId}.*"))
            {
                try
                {
                    System.IO.File.Delete(file);
                }
                catch
                {
                    // ignore locked files
                }
            }
        }
    }
}
