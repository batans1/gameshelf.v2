using GameShelf.Business.Authorization.Handlers;
using GameShelf.Business.Authorization.Requirements;
using GameShelf.Business.Repositories.Implementations;
using GameShelf.Business.Repositories.Interfaces;
using GameShelf.Business.Services.Implementations;
using GameShelf.Business.Services.Interfaces;
using GameShelf.Data.Persistance;
using GameShelf.Data.Seed;
using GameShelf.Web.Filters;
using GameShelf.Web.HealthChecks;
using GameShelf.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Threading.RateLimiting;

// configure Serilog from configuration only (avoid duplicate sinks)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "GameShelf")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
// Use Serilog for logging
builder.Host.UseSerilog();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("__SET_IN_", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured. Use User Secrets for local development " +
        "or environment variable 'ConnectionStrings__DefaultConnection' in deployed environments.");
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
// for now - disable email confirmation to simplify testing, but enforce strong passwords
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<SignInManager<IdentityUser>, EmailOrUserNameSignInManager>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
});
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddTransient<IPlatformService, PlatformService>();
builder.Services.AddTransient<IImageService, ImageService>();
builder.Services.AddTransient<IGameDealService, GameDealService>();
builder.Services.AddTransient<IGameRatingService, GameRatingService>();
builder.Services.AddTransient<ILiveDealSyncService, LiveDealSyncService>();
builder.Services.AddTransient<IDealRatingService, DealRatingService>();
builder.Services.AddTransient<IDealClickService, DealClickService>();
builder.Services.AddTransient<IReviewModerationService, ReviewModerationService>();
builder.Services.AddTransient<ISavingsCartService, SavingsCartService>();
builder.Services.Configure<GameShelf.Business.Services.LiveDealsOptions>(
    builder.Configuration.GetSection(GameShelf.Business.Services.LiveDealsOptions.SectionName));
builder.Services.AddHttpClient("CheapShark", client =>
{
    client.Timeout = TimeSpan.FromSeconds(25);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("GameShelf/1.0 (+https://github.com/)");
});
builder.Services.AddSingleton<IExchangeRateService, ExchangeRateService>();
// in memory for now - later fix
    builder.Services.AddMemoryCache();

builder.Services.AddTransient<IExternalDealsService, LiveDealsFromDbService>();
builder.Services.AddHostedService<LiveDealSyncBackgroundService>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("PlatformAccessPolicy", policy =>
        policy.Requirements.Add(new PlatformManagementAccessRequirement()));
builder.Services.AddScoped<IAuthorizationHandler, PlatformManagementAccessHandler>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddCheck<LiveDealsSyncHealthCheck>("live_deals_sync")
    .AddCheck<ExternalApiHealthCheck>("external_api");

builder.Services.AddRateLimiter(options =>
{
    // Global limiter for all endpoints to reduce DDoS/burst risk.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.IsAuthenticated == true
                ? $"user:{context.User.Identity.Name}"
                : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "anonymous"}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 20
            }));

    options.AddPolicy("AuthenticatedApiPolicy", context =>
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                context.User.Identity.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                });
        }
        return RateLimitPartition.GetNoLimiter<string>("anonymous");
    });
    options.AddPolicy("AnonymousApiPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", token);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GameShelf API",
        Version = "v1",
        Description = "REST API for GameShelf: platforms and game deals."
    });
});

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var corsMethods = builder.Configuration.GetSection("Cors:AllowedMethods").Get<string[]>() ?? new[] { "*" };
var corsHeaders = builder.Configuration.GetSection("Cors:AllowedHeaders").Get<string[]>() ?? new[] { "*" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment() || (corsOrigins?.Contains("*") == true))
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            if (corsOrigins?.Length > 0 == true)
                policy.WithOrigins(corsOrigins);
            if (corsMethods?.Contains("*") == true)
                policy.AllowAnyMethod();
            else if (corsMethods?.Length > 0 == true)
                policy.WithMethods(corsMethods);
            if (corsHeaders?.Contains("*") == true)
                policy.AllowAnyHeader();
            else if (corsHeaders?.Length > 0 == true)
                policy.WithHeaders(corsHeaders);
            policy.AllowCredentials();
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "GameShelf API"));
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseMiddleware<GameShelf.Web.Middleware.InputSanitizationMiddleware>();
app.UseCors();
app.UseRateLimiter();
app.UseAuthorization();
app.UseMiddleware<GameShelf.Web.Middleware.EnsureUserRoleMiddleware>();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapHealthChecks("/health");

app.Run();
Log.CloseAndFlush();

public partial class Program { }