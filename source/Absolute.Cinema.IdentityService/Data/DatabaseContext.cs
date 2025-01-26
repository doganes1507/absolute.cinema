using Absolute.Cinema.IdentityService.Models;
using Microsoft.EntityFrameworkCore;

namespace Absolute.Cinema.IdentityService.Data;

public class DatabaseContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
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
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.EmailAddress)
                .IsRequired()
                .HasMaxLength(64);
            
            entity.HasIndex(e => e.EmailAddress).IsUnique();
            
            entity.Property(e => e.HashPassword)
                .HasMaxLength(256);
            
            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId)
                .IsRequired();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(32);
            
            entity.HasIndex(e => e.Name).IsUnique();
        });
        
        var adminRoleId = Guid.NewGuid();
        
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = adminRoleId, Name = "Admin" },
            new Role { Id = Guid.NewGuid(), Name = "User" }
        );
        
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = Guid.NewGuid(),
                EmailAddress = "NextM0re@yandex.ru",
                HashPassword = BCrypt.Net.BCrypt.HashPassword("21872187"),
                RoleId = adminRoleId
            }
        );
    }
}