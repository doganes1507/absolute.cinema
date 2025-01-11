using Absolute.Cinema.IdentityService.Models;
using Microsoft.EntityFrameworkCore;

namespace Absolute.Cinema.IdentityService.DataContext;

public class DatabaseContext : DbContext
{
    public DbSet<User> Users { get; set; }
    
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
        Database.EnsureCreated();
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(64);
            
            entity.Property(e => e.HashPassword)
                .HasMaxLength(256);
        });
    }
}