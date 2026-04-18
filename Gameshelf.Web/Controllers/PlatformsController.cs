using System.Security.Claims;
using AutoMapper;
using GameShelf.Business.Services.Interfaces;
using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using GameShelf.Models.ViewModels.DealRatings;
using GameShelf.Models.ViewModels.GameDeals;
using GameShelf.Models.ViewModels.LiveDeals;
using GameShelf.Models.ViewModels.Platforms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameShelf.Web.Controllers;

public class PlatformsController : Controller
{
    private readonly IPlatformService _platformService;
    private readonly IExternalDealsService _externalDealsService;
    private readonly IGameDealService _gameDealService;
    private readonly IGameRatingService _ratingService;
    private readonly IDealRatingService _dealRatingService;
    private readonly IDealClickService _dealClickService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IMapper _mapper;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAuthorizationService _authorizationService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ISavingsCartService _savingsCartService;

    public PlatformsController(IPlatformService platformService, IExternalDealsService externalDealsService, IGameDealService gameDealService, IGameRatingService ratingService, IDealRatingService dealRatingService, IDealClickService dealClickService, IExchangeRateService exchangeRateService, IMapper mapper, UserManager<IdentityUser> userManager, IAuthorizationService authorizationService, ApplicationDbContext dbContext, ISavingsCartService savingsCartService)
    {
        _platformService = platformService;
        _externalDealsService = externalDealsService;
        _gameDealService = gameDealService;
        _ratingService = ratingService;
        _dealRatingService = dealRatingService;
        _dealClickService = dealClickService;
        _exchangeRateService = exchangeRateService;
        _mapper = mapper;
        _userManager = userManager;
        _authorizationService = authorizationService;
        _dbContext = dbContext;
        _savingsCartService = savingsCartService;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _platformService.GetAllAsync());
    }

    [Authorize(Roles = "Admin,PlatformOwner")]
    public async Task<IActionResult> Manage()
    {
        return View(await _platformService.GetByOwnerIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty));
    }

    [Authorize(Roles = "Admin,PlatformOwner")]
    public async Task<IActionResult> CustomDeals(Guid? id)
    {
        if (id == null) return NotFound();
        var authResult = await _authorizationService.AuthorizeAsync(User, id.Value, "PlatformAccessPolicy");
        if (!authResult.Succeeded) return Forbid();

        var platform = await _platformService.GetByIdAsync(id.Value);
        if (platform == null) return NotFound();

        var deals = await _gameDealService.GetByPlatformIdAsync(id.Value);
        ViewData["PlatformId"] = id.Value;
        ViewData["PlatformName"] = platform.Name;
        return View(deals);
    }

    [Authorize(Roles = "Admin,PlatformOwner")]
    [HttpGet]
    public async Task<IActionResult> CreateDeal(Guid platformId)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, platformId, "PlatformAccessPolicy");
        if (!authResult.Succeeded) return Forbid();

        var platform = await _platformService.GetByIdAsync(platformId);
        if (platform == null) return NotFound();

        var model = new GameDealCreateOrEditViewModel
        {
            PlatformId = platformId,
            IsActive = true,
            IsAvailable = true,
            DisplayOrder = 0
        };
        ViewData["PlatformName"] = platform.Name;
        return View(model);
    }

    [Authorize(Roles = "Admin,PlatformOwner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDeal(GameDealCreateOrEditViewModel model)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, model.PlatformId, "PlatformAccessPolicy");
        if (!authResult.Succeeded) return Forbid();

        var platform = await _platformService.GetByIdAsync(model.PlatformId);
        if (platform == null) return NotFound();

        if (ModelState.IsValid)
        {
            await _gameDealService.CreateAsync(model);
            TempData["DealCreateSuccess"] = "Custom deal added successfully.";
            return RedirectToAction(nameof(CustomDeals), new { id = model.PlatformId });
        }

        ViewData["PlatformName"] = platform.Name;
        return View(model);
    }

    [Authorize(Roles = "Admin,PlatformOwner")]
    [HttpGet]
    public async Task<IActionResult> EditDeal(Guid? id)
    {
        if (id == null) return NotFound();

        var deal = await _gameDealService.GetByIdAsync(id.Value);
        if (deal == null) return NotFound();

        var authResult = await _authorizationService.AuthorizeAsync(User, deal.PlatformId, "PlatformAccessPolicy");
        if (!authResult.Succeeded) return Forbid();

        var model = new GameDealCreateOrEditViewModel
        {
            PlatformId = deal.PlatformId,
            Name = deal.Name,
            Description = deal.Description,
            PriceUsd = deal.Price,
            OriginalPriceUsd = deal.OriginalPrice,
            DiscountPercent = deal.DiscountPercent,
            ImageUrl = deal.ImageUrl,
            DealUrl = deal.DealUrl,
            IsActive = deal.IsActive,
            IsAvailable = deal.IsAvailable,
            DisplayOrder = deal.DisplayOrder
        };
        ViewData["PlatformName"] = deal.PlatformName;
        ViewData["DealId"] = deal.Id;
        return View(model);
    }

    [Authorize(Roles = "Admin,PlatformOwner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDeal(Guid id, GameDealCreateOrEditViewModel model)
    {
        var deal = await _gameDealService.GetByIdAsync(id);
        if (deal == null) return NotFound();

        var authResult = await _authorizationService.AuthorizeAsync(User, model.PlatformId, "PlatformAccessPolicy");
        if (!authResult.Succeeded) return Forbid();

        if (ModelState.IsValid)
        {
            await _gameDealService.UpdateAsync(id, model);
            TempData["DealEditSuccess"] = "Custom deal updated successfully.";
            return RedirectToAction(nameof(CustomDeals), new { id = model.PlatformId });
        }

        ViewData["PlatformName"] = deal.PlatformName;
        ViewData["DealId"] = id;
        return View(model);
    }

    [Authorize(Roles = "Admin,PlatformOwner")]
    [HttpGet]
    public async Task<IActionResult> DeleteDeal(Guid? id)
    {
        if (id == null) return NotFound();

        var deal = await _gameDealService.GetByIdAsync(id.Value);
        if (deal == null) return NotFound();

        var authResult = await _authorizationService.AuthorizeAsync(User, deal.PlatformId, "PlatformAccessPolicy");
        if (!authResult.Succeeded) return Forbid();

        return View(deal);
    }

    [Authorize(Roles = "Admin,PlatformOwner")]
    [HttpPost, ActionName("DeleteDeal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDealConfirmed(Guid id)
    {
        var deal = await _gameDealService.GetByIdAsync(id);
        if (deal == null) return NotFound();

        var authResult = await _authorizationService.AuthorizeAsync(User, deal.PlatformId, "PlatformAccessPolicy");
        if (!authResult.Succeeded) return Forbid();

        var platformId = deal.PlatformId;
        await _gameDealService.DeleteAsync(id);
        TempData["DealDeleteSuccess"] = "Custom deal deleted.";
        return RedirectToAction(nameof(CustomDeals), new { id = platformId });
    }

    [Authorize(Roles = "Admin,PlatformOwner")]
    public async Task<IActionResult> ClickStats(Guid? id)
    {
        if (id == null) return NotFound();
        var platform = await _platformService.GetByIdAsync(id.Value);
        if (platform == null) return NotFound();
        
        var authResult = await _authorizationService.AuthorizeAsync(User, id.Value, "PlatformAccessPolicy");
        if (!authResult.Succeeded) return Forbid();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var clicks = await _dealClickService.GetClicksForPlatformAsync(platform.Name, userId);
        ViewData["PlatformName"] = platform.Name;
        return View(clicks);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();
        var platform = await _platformService.GetByIdAsync(id.Value);
        if (platform == null) return NotFound();
        return View(platform);
    }

    /// <summary>
    /// Game details page where users can view info and rate the deal.
    /// </summary>
    public async Task<IActionResult> GameDetails(string dealId, string storeName, Guid? customDealId = null)
    {
        GameDealDetailsViewModel viewModel;
        
        if (customDealId.HasValue)
        {
            // Custom deal
            var customDeal = await _gameDealService.GetByIdAsync(customDealId.Value);
            if (customDeal == null)
                return NotFound();
            
            var platform = await _platformService.GetByIdAsync(customDeal.PlatformId);
            if (platform == null)
                return NotFound();
            
            var exchangeRate = await _exchangeRateService.GetUsdToEurRateAsync();
            
            viewModel = new GameDealDetailsViewModel
            {
                GameDealId = customDeal.Id,
                Title = customDeal.Name,
                SalePriceUsd = customDeal.Price,
                NormalPriceUsd = customDeal.OriginalPrice ?? customDeal.Price,
                SalePriceEur = Math.Round(customDeal.Price * exchangeRate, 2),
                NormalPriceEur = Math.Round((customDeal.OriginalPrice ?? customDeal.Price) * exchangeRate, 2),
                SavingsPercent = customDeal.DiscountPercent ?? 0,
                ThumbUrl = customDeal.ImageUrl,
                DealUrl = customDeal.DealUrl ?? "#",
                StoreName = platform.Name,
                CustomDealId = customDealId.Value
            };
        }
        else
        {
            // Live deal - find in database first
            if (string.IsNullOrEmpty(dealId) || string.IsNullOrEmpty(storeName))
                return NotFound();
            
            // Normalize DealId
            var normalizedDealId = dealId.Trim();
            try
            {
                string prev;
                do
                {
                    prev = normalizedDealId;
                    normalizedDealId = Uri.UnescapeDataString(normalizedDealId);
                } while (normalizedDealId != prev && normalizedDealId.Contains('%'));
            }
            catch { }
            normalizedDealId = normalizedDealId.Trim();
            
            // Find GameDeal in database
            var gameDeal = await _dbContext.GameDeals
                .Include(d => d.Platform)
                .FirstOrDefaultAsync(d => 
                    d.Source == DealSource.Live && 
                    d.DealId != null && 
                    d.DealId.Trim() == normalizedDealId);
            
            if (gameDeal == null)
            {
                // If not in DB, try to sync it on-the-fly (fallback)
                try
                {
                    var liveDealSyncService = HttpContext.RequestServices.GetRequiredService<ILiveDealSyncService>();
                    var gameDealId = await liveDealSyncService.GetOrCreateGameDealIdAsync(normalizedDealId, storeName);
                    gameDeal = await _dbContext.GameDeals
                        .Include(d => d.Platform)
                        .FirstOrDefaultAsync(d => d.Id == gameDealId);
                }
                catch
                {
                    // If sync fails, return NotFound
                    return NotFound();
                }
            }
            
            if (gameDeal == null)
                return NotFound();
            
            var exchangeRate = await _exchangeRateService.GetUsdToEurRateAsync();
            viewModel = new GameDealDetailsViewModel
            {
                GameDealId = gameDeal.Id,
                Title = gameDeal.Name,
                SalePriceUsd = gameDeal.Price,
                NormalPriceUsd = gameDeal.OriginalPrice ?? gameDeal.Price,
                SalePriceEur = Math.Round(gameDeal.Price * exchangeRate, 2),
                NormalPriceEur = Math.Round((gameDeal.OriginalPrice ?? gameDeal.Price) * exchangeRate, 2),
                SavingsPercent = gameDeal.DiscountPercent ?? 0,
                ThumbUrl = gameDeal.ImageUrl,
                DealUrl = gameDeal.DealUrl ?? "#",
                StoreName = gameDeal.StoreName,
                DealId = gameDeal.DealId,
                CustomDealId = gameDeal.Source == DealSource.Custom ? gameDeal.Id : null
            };
        }

        var profileUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ViewData["InSavingsCart"] = !string.IsNullOrEmpty(profileUserId)
            && await _savingsCartService.ContainsAsync(profileUserId, viewModel.GameDealId);
        
        // Get verdict rating info
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            viewModel.Rating = await _dealRatingService.GetUserRatingAsync(viewModel.DealId, viewModel.CustomDealId, userId) ?? new DealRatingViewModel
            {
                CommunityVerdict = await _dealRatingService.GetCommunityVerdictAsync(viewModel.DealId, viewModel.CustomDealId)
            };
        }
        else
        {
            viewModel.Rating = new DealRatingViewModel
            {
                CommunityVerdict = await _dealRatingService.GetCommunityVerdictAsync(viewModel.DealId, viewModel.CustomDealId)
            };
        }
        
        return View(viewModel);
    }

    /// <summary>
    /// Featured games sorted by rating (highest rated first).
    /// </summary>
    public async Task<IActionResult> FeaturedGames(int page = 1)
    {
        const int pageSize = 20;
        
        // Get all GameDeals from database that have ratings, using inner join
        var gameDealsWithRatings = await _dbContext.GameDeals
            .Include(d => d.Platform)
            .Include(d => d.Ratings)
            .Where(d => d.Ratings.Any())
            .ToListAsync();

        // Calculate verdict stats for each deal
        var dealStats = gameDealsWithRatings.Select(d =>
        {
            var ratings = d.Ratings.ToList();
            var total = ratings.Count;
            var buyNowCount = ratings.Count(r => r.Verdict == DealVerdict.BuyNow);
            var waitCount = ratings.Count(r => r.Verdict == DealVerdict.Wait);
            var notWorthItCount = ratings.Count(r => r.Verdict == DealVerdict.NotWorthIt);

            return new
            {
                GameDeal = d,
                BuyNowPercent = (double)buyNowCount / total * 100,
                WaitPercent = (double)waitCount / total * 100,
                NotWorthItPercent = (double)notWorthItCount / total * 100,
                TotalRatings = total
            };
        }).ToList();

        // Sort by verdict percentages and paginate
        var sortedDeals = dealStats
            .OrderByDescending(s => s.BuyNowPercent)
            .ThenByDescending(s => s.WaitPercent)
            .ThenByDescending(s => s.NotWorthItPercent)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Convert to LiveDealDto format (with community verdict percentages for card coloring)
        var exchangeRate = await _exchangeRateService.GetUsdToEurRateAsync();
        var dealsList = sortedDeals.Select(s =>
        {
            var d = s.GameDeal;
            return new LiveDealDto
            {
                Title = d.Name,
                SalePriceUsd = d.Price,
                NormalPriceUsd = d.OriginalPrice ?? d.Price,
                SalePriceEur = Math.Round(d.Price * exchangeRate, 2),
                NormalPriceEur = Math.Round((d.OriginalPrice ?? d.Price) * exchangeRate, 2),
                SavingsPercent = d.DiscountPercent ?? 0,
                ThumbUrl = d.ImageUrl,
                DealUrl = d.DealUrl ?? "#",
                StoreName = d.StoreName,
                DealId = d.DealId,
                CustomDealId = d.Source == DealSource.Custom ? d.Id : null,
                AverageRating = 0,
                RatingCount = s.TotalRatings,
                BuyNowPercent = s.BuyNowPercent,
                WaitPercent = s.WaitPercent,
                NotWorthItPercent = s.NotWorthItPercent
            };
        }).ToList();

        var totalDeals = dealStats.Count;
        var totalPages = (int)Math.Ceiling(totalDeals / (double)pageSize);
        
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalDeals"] = totalDeals;
        return View(dealsList);
    }

    /// <summary>
    /// Search for deals across all platforms by game title.
    /// </summary>
    public async Task<IActionResult> Search(string? query, int page = 1)
    {
        const int pageSize = 20;
        
        if (string.IsNullOrWhiteSpace(query))
        {
            ViewData["Query"] = "";
            ViewData["CurrentPage"] = 1;
            ViewData["TotalPages"] = 0;
            ViewData["TotalDeals"] = 0;
            return View(new List<LiveDealDto>());
        }
        
        // Get all deals and filter by query
        var allDeals = await _externalDealsService.GetLiveDealsAllPlatformsAsync(pageNumber: 1, pageSize: 1000);
        var matchingDeals = allDeals
            .Where(d => d.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        var totalDeals = matchingDeals.Count;
        var totalPages = (int)Math.Ceiling(totalDeals / (double)pageSize);
        var pagedDeals = matchingDeals
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        
        // Populate user ratings if logged in
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            foreach (var deal in pagedDeals.Where(d => !string.IsNullOrEmpty(d.DealId)))
            {
                deal.UserRating = await _ratingService.GetUserRatingAsync(deal.DealId!, userId);
            }
        }
        
        ViewData["Query"] = query;
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalDeals"] = totalDeals;
        
        return View(pagedDeals);
    }

    /// <summary>
    /// Library of deals for the selected platform (thumbnail, name, price USD/EUR).
    /// Deals are loaded from DB (synced on startup and every 1 hour from API).
    /// </summary>
    public async Task<IActionResult> Deals(Guid? id, int page = 1, string? sortBy = null, decimal? minPrice = null, decimal? maxPrice = null, int? minDiscount = null)
    {
        if (id == null) return NotFound();
        var platform = await _platformService.GetByIdAsync(id.Value);
        if (platform == null) return NotFound();
        
        // Get all GameDeals for the platform from database (both custom and live)
        var gameDeals = await _dbContext.GameDeals
            .Where(d => d.PlatformId == id.Value && d.IsActive && d.IsAvailable)
            .Include(d => d.Ratings)
            .ToListAsync();
        
        var exchangeRate = await _exchangeRateService.GetUsdToEurRateAsync();
        var dealsList = gameDeals.Select(d => new LiveDealDto
        {
            Title = d.Name,
            SalePriceUsd = d.Price,
            NormalPriceUsd = d.OriginalPrice ?? d.Price,
            SalePriceEur = Math.Round(d.Price * exchangeRate, 2),
            NormalPriceEur = Math.Round((d.OriginalPrice ?? d.Price) * exchangeRate, 2),
            SavingsPercent = d.DiscountPercent ?? 0,
            ThumbUrl = d.ImageUrl,
            DealUrl = d.DealUrl ?? "#",
            StoreName = d.StoreName,
            DealId = d.DealId, 
            CustomDealId = d.Source == DealSource.Custom ? d.Id : null,
            AverageRating = 0,
            RatingCount = d.Ratings.Count
        }).ToList();
        
        // Apply filters
        if (minPrice.HasValue)
        {
            dealsList = dealsList.Where(d => d.SalePriceEur >= minPrice.Value).ToList();
        }
        if (maxPrice.HasValue)
        {
            dealsList = dealsList.Where(d => d.SalePriceEur <= maxPrice.Value).ToList();
        }
        if (minDiscount.HasValue)
        {
            dealsList = dealsList.Where(d => d.SavingsPercent >= minDiscount.Value).ToList();
        }
        
        
        // Apply sorting
        dealsList = sortBy?.ToLower() switch
        {
            "price_asc" => dealsList.OrderBy(d => d.SalePriceEur).ToList(),
            "price_desc" => dealsList.OrderByDescending(d => d.SalePriceEur).ToList(),
            "discount" => dealsList.OrderByDescending(d => d.SavingsPercent).ToList(),
            
            _ => dealsList.OrderByDescending(d => d.SavingsPercent).ToList() // Default: sort by discount
        };
        
        // Pagination
        const int pageSize = 20;
        var totalDeals = dealsList.Count;
        var totalPages = (int)Math.Ceiling(totalDeals / (double)pageSize);
        var pagedDeals = dealsList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        
        ViewData["PlatformName"] = platform.Name;
        ViewData["PlatformUrl"] = platform.WebsiteUrl;
        ViewData["PlatformId"] = id.Value;
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalDeals"] = totalDeals;
        ViewData["SortBy"] = sortBy;
        ViewData["MinPrice"] = minPrice;
        ViewData["MaxPrice"] = maxPrice;
        ViewData["MinDiscount"] = minDiscount;
        
        return View(pagedDeals);
    }

    [Authorize(Roles = "Admin,PlatformOwner")]
    public async Task<IActionResult> Create()
    {
        var model = new PlatformCreateOrEditViewModel();
        model.AvailableOwners = await GetOwnersSelectList();
        return View(model);
    }

    [Authorize(Roles = "Admin,PlatformOwner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlatformCreateOrEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            await _platformService.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }
        model.AvailableOwners = await GetOwnersSelectList();
        return View(model);
    }

    [Authorize(Roles = "Admin,PlatformOwner")]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();
        var authResult = await _authorizationService.AuthorizeAsync(User, id.Value, "PlatformAccessPolicy");
        if (!authResult.Succeeded) return Forbid();

        var platform = await _platformService.GetByIdAsync(id.Value);
        if (platform == null) return NotFound();
        var editModel = _mapper.Map<PlatformCreateOrEditViewModel>(platform);
        editModel.SelectedOwnerIds = platform.Owners.Select(o => o.Id).ToList();
        editModel.ExistingImages = platform.Images?.ToList() ?? new List<PlatformImageViewModel>();
        editModel.AvailableOwners = await GetOwnersSelectList();
        ViewData["PlatformId"] = id.Value;
        return View(editModel);
    }

    [Authorize(Roles = "Admin,PlatformOwner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PlatformCreateOrEditViewModel model)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, id, "PlatformAccessPolicy");
        if (!authResult.Succeeded) return Forbid();

        if (ModelState.IsValid)
        {
            try
            {
                await _platformService.UpdateAsync(id, model);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await _platformService.GetByIdAsync(id) == null) return NotFound();
                throw;
            }
        }
        
        // Reload existing data for the view
        var existing = await _platformService.GetByIdAsync(id);
        if (existing != null)
        {
            model.ExistingImages = existing.Images?.ToList() ?? new List<PlatformImageViewModel>();
            model.SelectedOwnerIds = existing.Owners.Select(o => o.Id).ToList();
        }
        model.AvailableOwners = await GetOwnersSelectList();
        ViewData["PlatformId"] = id;
        return View(model);
    }

    [Authorize(Roles = "Admin,PlatformOwner")]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null) return NotFound();
        var authResult = await _authorizationService.AuthorizeAsync(User, id.Value, "PlatformAccessPolicy");
        if (!authResult.Succeeded) return Forbid();
        var platform = await _platformService.GetByIdAsync(id.Value);
        if (platform == null) return NotFound();
        return View(platform);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,PlatformOwner")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, id, "PlatformAccessPolicy");
        if (!authResult.Succeeded) return Forbid();
        var platform = await _platformService.GetByIdAsync(id);
        if (platform != null) await _platformService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,PlatformOwner")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLogo(Guid platformId, Guid imageId)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, platformId, "PlatformAccessPolicy");
        if (!authResult.Succeeded) return Forbid();
        
        try
        {
            await _platformService.DeletePlatformLogoAsync(platformId, imageId);
            return RedirectToAction(nameof(Edit), new { id = platformId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private async Task<List<SelectListItem>> GetOwnersSelectList()
    {
        var owners = await _userManager.GetUsersInRoleAsync("PlatformOwner");
        return owners.Select(u => new SelectListItem { Value = u.Id, Text = u.UserName }).ToList();
    }
}
