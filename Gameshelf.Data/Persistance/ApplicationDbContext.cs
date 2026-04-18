using GameShelf.Models.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GameShelf.Data.Persistance;

public class ApplicationDbContext : IdentityDbContext
{
    public DbSet<Platform> Platforms { get; set; }
    public DbSet<PlatformImage> PlatformImages { get; set; }
    public DbSet<GameDeal> GameDeals { get; set; }
    public DbSet<PlatformOwner> PlatformOwners { get; set; }
    public DbSet<GameRating> GameRatings { get; set; }
    public DbSet<DealRating> DealRatings { get; set; }
    public DbSet<DealClick> DealClicks { get; set; }
    public DbSet<UserModerationStatus> UserModerationStatuses { get; set; }
    public DbSet<SavingsCartItem> SavingsCartItems { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Platform>()
            .HasMany(p => p.Images)
            .WithOne(i => i.Platform)
            .HasForeignKey(i => i.PlatformId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Platform>()
            .HasMany(p => p.Deals)
            .WithOne(d => d.Platform)
            .HasForeignKey(d => d.PlatformId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GameDeal>(entity =>
        {
            entity.Property(d => d.Name).HasMaxLength(200).IsRequired();
            entity.Property(d => d.Description).HasMaxLength(2000);
            entity.Property(d => d.Price).HasPrecision(18, 2);
            entity.Property(d => d.OriginalPrice).HasPrecision(18, 2);
            entity.Property(d => d.ImageUrl).HasMaxLength(1000);
            entity.Property(d => d.DealUrl).HasMaxLength(1000);
            entity.Property(d => d.DisplayOrder).IsRequired();
            entity.Property(d => d.Source).IsRequired();
            entity.Property(d => d.DealId).HasMaxLength(100);
            entity.Property(d => d.StoreName).HasMaxLength(100).IsRequired();
            
            entity.HasIndex(d => new { d.PlatformId, d.Name });
            entity.HasIndex(d => new { d.PlatformId, d.DisplayOrder });
            entity.HasIndex(d => new { d.Source, d.DealId }).HasFilter("[DealId] IS NOT NULL");
            entity.HasIndex(d => d.StoreName);
        });

        modelBuilder.Entity<PlatformOwner>()
            .HasKey(po => new { po.PlatformId, po.UserId });

        modelBuilder.Entity<PlatformOwner>()
            .HasOne(po => po.Platform)
            .WithMany(p => p.Owners)
            .HasForeignKey(po => po.PlatformId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlatformOwner>()
            .HasOne(po => po.User)
            .WithMany()
            .HasForeignKey(po => po.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GameRating>(entity =>
        {
            entity.Property(r => r.DealId).HasMaxLength(100).IsRequired();
            entity.Property(r => r.StoreName).HasMaxLength(100).IsRequired();
            entity.Property(r => r.Rating).IsRequired();
            entity.HasIndex(r => new { r.DealId, r.UserId }).IsUnique();
            entity.HasIndex(r => new { r.StoreName, r.DealId });
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DealRating>(entity =>
        {
            entity.Property(r => r.Verdict).IsRequired();
            entity.Property(r => r.ReasonId).IsRequired();
            entity.Property(r => r.ReviewText).HasMaxLength(2000);
            
            // Foreign key to GameDeal
            entity.HasOne(r => r.GameDeal)
                .WithMany(d => d.Ratings)
                .HasForeignKey(r => r.GameDealId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Foreign key to User
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Unique constraint: one rating per user per deal
            entity.HasIndex(r => new { r.GameDealId, r.UserId }).IsUnique();
            
            // Indexes for querying
            entity.HasIndex(r => r.GameDealId);
            entity.HasIndex(r => r.Verdict);
        });

        modelBuilder.Entity<DealClick>(entity =>
        {
            entity.Property(c => c.DealId).HasMaxLength(100).IsRequired();
            entity.Property(c => c.StoreName).HasMaxLength(100).IsRequired();
            entity.Property(c => c.GameTitle).HasMaxLength(500).IsRequired();
            entity.Property(c => c.DealUrl).HasMaxLength(1000).IsRequired();
            entity.HasIndex(c => new { c.StoreName, c.DealId });
            entity.HasIndex(c => c.ClickedAt);
        });

        modelBuilder.Entity<UserModerationStatus>(entity =>
        {
            entity.HasKey(m => m.UserId);
            entity.Property(m => m.UserId).HasMaxLength(450);
            entity.Property(m => m.StrikeCount).IsRequired();
            entity.Property(m => m.WarningsInCurrentStrike).IsRequired();
            entity.HasIndex(m => m.TimeoutUntilUtc);
        });

        modelBuilder.Entity<SavingsCartItem>(entity =>
        {
            entity.Property(c => c.UserId).HasMaxLength(450).IsRequired();
            entity.HasOne(c => c.GameDeal)
                .WithMany()
                .HasForeignKey(c => c.GameDealId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(c => new { c.UserId, c.GameDealId }).IsUnique();
            entity.HasIndex(c => c.CreatedAt);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(p => p.UserId);
            entity.Property(p => p.UserId).HasMaxLength(450);
            entity.Property(p => p.AvatarPath).HasMaxLength(500);
        });
    }
}
