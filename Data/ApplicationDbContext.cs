using Microsoft.EntityFrameworkCore;
using WebApplication27.Models;

namespace WebApplication27.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ImageMetadata> Images { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ImageMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StorageUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UploadedAt).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
        });
    }
}
