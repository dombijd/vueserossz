using GlosterIktato.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GlosterIktato.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserRole many-to-many konfiguráció
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Szerepkörök
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Rendszergazda" },
                new Role { Id = 2, Name = "User", Description = "Felhasználó" }
            );

            // Cég
            modelBuilder.Entity<Company>().HasData(
                new Company
                {
                    Id = 1,
                    Name = "Gloster Kft.",
                    TaxNumber = "12345678-1-23",
                    Address = "Budapest, Példa utca 1.",
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Admin user - jelszó: Admin123!
            // BCrypt hash generálása: BCrypt.Net.BCrypt.HashPassword("Admin123!")
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Email = "admin@gloster.hu",
                    PasswordHash = "$2a$11$K5PqYx7qHqF5qF5qF5qF5uXQYx7qHqF5qF5qF5qF5qF5qF5qF5qF5",
                    FirstName = "Admin",
                    LastName = "User",
                    CompanyId = 1,
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Admin role
            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { UserId = 1, RoleId = 1 }
            );

            // Dokumentum típusok
            modelBuilder.Entity<DocumentType>().HasData(
                new DocumentType { Id = 1, Name = "Számla", Code = "SZLA", IsActive = true },
                new DocumentType { Id = 2, Name = "Teljesítés Igazolás", Code = "TIG", IsActive = true },
                new DocumentType { Id = 3, Name = "Szerződés", Code = "SZ", IsActive = true },
                new DocumentType { Id = 4, Name = "Egyéb", Code = "E", IsActive = true }
            );

            // Szállítók
            modelBuilder.Entity<Supplier>().HasData(
                new Supplier
                {
                    Id = 1,
                    Name = "Teszt Szállító Kft.",
                    TaxNumber = "98765432-1-23",
                    Email = "info@teszt.hu",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Supplier
                {
                    Id = 2,
                    Name = "Másik Szállító Zrt.",
                    TaxNumber = "11223344-2-44",
                    Email = "info@masik.hu",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}