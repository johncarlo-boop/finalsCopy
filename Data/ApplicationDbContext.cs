using Microsoft.EntityFrameworkCore;
using PropertyInventory.Models;

namespace PropertyInventory.Data;

/// <summary>
/// Database Context - Only two tables: Users (admin) and Properties (inventory)
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Users table (Admin users only)
    public DbSet<ApplicationUser> Users { get; set; } = null!;

    // Properties table (Property Inventory)
    public DbSet<Property> Properties { get; set; } = null!;

    // OTP Verifications table
    public DbSet<OtpVerification> OtpVerifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Users table
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ProfilePicturePath).HasMaxLength(500);
            entity.Property(e => e.RequiresPasswordChange).HasDefaultValue(false);
        });

        // Properties table
        builder.Entity<Property>(entity =>
        {
            entity.ToTable("Properties");
            entity.HasIndex(e => e.PropertyCode).IsUnique();
            entity.Property(e => e.PropertyCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PropertyName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
        });

        // OTP Verifications table
        builder.Entity<OtpVerification>(entity =>
        {
            entity.ToTable("OtpVerifications");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.OtpCode).IsRequired().HasMaxLength(6);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
        });
    }
}
