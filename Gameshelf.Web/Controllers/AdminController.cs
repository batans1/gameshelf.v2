using GameShelf.Business.Services.Interfaces;
using GameShelf.Data.Persistance;
using GameShelf.Models.ViewModels.Platforms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameShelf.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private const string HeroImageFileName = "hero-bg.jpg";

    private readonly IPlatformService _platformService;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IWebHostEnvironment _env;

    public AdminController(
        IPlatformService platformService,
        ApplicationDbContext dbContext,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IWebHostEnvironment env)
    {
        _platformService = platformService;
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var stats = new AdminDashboardViewModel
        {
            TotalUsers = await _userManager.Users.CountAsync(),
            TotalPlatforms = await _dbContext.Platforms.CountAsync(),
            TotalDealClicks = await _dbContext.DealClicks.CountAsync(),
            TotalRatings = await _dbContext.GameRatings.CountAsync()
        };

        // Get click statistics by platform
        var clicksByPlatform = await _dbContext.DealClicks
            .GroupBy(c => c.StoreName)
            .Select(g => new PlatformClickStats
            {
                PlatformName = g.Key,
                ClickCount = g.Count(),
                LastClick = g.Max(c => c.ClickedAt)
            })
            .OrderByDescending(s => s.ClickCount)
            .ToListAsync();

        stats.PlatformClickStats = clicksByPlatform;

        // Get recent activity (last 10 clicks)
        var recentClicks = await _dbContext.DealClicks
            .OrderByDescending(c => c.ClickedAt)
            .Take(10)
            .Select(c => new RecentActivity
            {
                GameTitle = c.GameTitle,
                PlatformName = c.StoreName,
                ClickedAt = c.ClickedAt,
                UserId = c.UserId
            })
            .ToListAsync();

        stats.RecentActivity = recentClicks;

        return View(stats);
    }

    public async Task<IActionResult> Users()
    {
        var users = new List<UserViewModel>();
        foreach (var u in _userManager.Users.ToList())
        {
            var roles = await _userManager.GetRolesAsync(u);
            users.Add(new UserViewModel
            {
                Id = u.Id,
                Email = u.Email ?? "",
                UserName = u.UserName ?? "",
                Roles = roles.ToList()
            });
        }

        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> EditRoles(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var userRoles = await _userManager.GetRolesAsync(user);
        var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

        var model = new EditRolesViewModel
        {
            UserId = user.Id,
            UserName = user.UserName ?? user.Email ?? "Unknown",
            Email = user.Email ?? "",
            AllRoles = allRoles,
            SelectedRoleNames = userRoles.ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRoles(EditRolesViewModel model)
    {
        if (string.IsNullOrEmpty(model.UserId))
            return NotFound();

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
            return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToAdd = (model.SelectedRoleNames ?? new List<string>()).Except(currentRoles).ToList();
        var rolesToRemove = currentRoles.Except(model.SelectedRoleNames ?? new List<string>()).ToList();

        if (rolesToAdd.Any())
            await _userManager.AddToRolesAsync(user, rolesToAdd);
        if (rolesToRemove.Any())
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

        TempData["RoleEditSuccess"] = $"Roles updated for {user.UserName ?? user.Email}.";
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Platforms()
    {
        var platforms = await _platformService.GetAllAsync();
        return View(platforms);
    }

    [HttpGet]
    public IActionResult HeroImage()
    {
        var imagesPath = Path.Combine(_env.WebRootPath, "images");
        var heroPath = Path.Combine(imagesPath, HeroImageFileName);
        ViewBag.HasCurrentImage = System.IO.File.Exists(heroPath);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HeroImage(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            TempData["HeroImageError"] = "Please select an image file.";
            return RedirectToAction(nameof(HeroImage));
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
        {
            TempData["HeroImageError"] = "Allowed formats: JPG, PNG, GIF, WebP.";
            return RedirectToAction(nameof(HeroImage));
        }

        var imagesPath = Path.Combine(_env.WebRootPath, "images");
        if (!Directory.Exists(imagesPath))
            Directory.CreateDirectory(imagesPath);

        var heroPath = Path.Combine(imagesPath, HeroImageFileName);
        await using (var stream = new FileStream(heroPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        TempData["HeroImageSuccess"] = "Hero image updated. Refresh the home page to see the new image.";
        return RedirectToAction(nameof(HeroImage));
    }
}

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalPlatforms { get; set; }
    public int TotalDealClicks { get; set; }
    public int TotalRatings { get; set; }
    public List<PlatformClickStats> PlatformClickStats { get; set; } = new();
    public List<RecentActivity> RecentActivity { get; set; } = new();
}

public class PlatformClickStats
{
    public string PlatformName { get; set; } = string.Empty;
    public int ClickCount { get; set; }
    public DateTime? LastClick { get; set; }
}

public class RecentActivity
{
    public string GameTitle { get; set; } = string.Empty;
    public string PlatformName { get; set; } = string.Empty;
    public DateTime ClickedAt { get; set; }
    public string? UserId { get; set; }
}

public class UserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class EditRolesViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> AllRoles { get; set; } = new();
    public List<string> SelectedRoleNames { get; set; } = new();
}
