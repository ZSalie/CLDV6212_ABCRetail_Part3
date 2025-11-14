using ABC_Retailers_Part3.Models;
using Microsoft.EntityFrameworkCore;

namespace ABC_Retailers_Part3.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Cart> Cart { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Configure Cart entity (FIXED: Remove problematic index)
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasKey(e => e.CartId);
                entity.Property(e => e.ProductId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ProductName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Quantity).HasDefaultValue(1);

                // Relationship
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Only create index on UserId, not on ProductId to avoid the error
                entity.HasIndex(e => e.UserId);
            });

            // Seed initial admin user with CORRECT password hash for "function123#"
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Username = "admin",
                    Password = "lCSC1VdkA5jB8woVCnq5w0QzDnL0hPY6D5nM6F3n1iM=", // CORRECT SHA256 hash of "function123#"
                    Role = "Admin",
                    Email = "admin@abcretailer.com",
                    CreatedAt = DateTime.Parse("2025-01-01")
                }
            );

            base.OnModelCreating(modelBuilder);
        }
    }
}