using Absolute.Cinema.AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace Absolute.Cinema.AccountService.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseLazyLoadingProxies();
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id);
            
            entity.Property(e => e.EmailAddress)
                .IsRequired()
                .HasMaxLength(64);
            
            entity.HasIndex(e => e.EmailAddress).IsUnique();

            entity.Property(e => e.FirstName)
                .HasMaxLength(64);
        });
    }
}