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
        public DbSet<UserCompany> UserCompanies { get; set; }

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

            // UserCompany many-to-many konfiguráció (ÚJ!)
            modelBuilder.Entity<UserCompany>()
                .HasKey(uc => new { uc.UserId, uc.CompanyId });

            modelBuilder.Entity<UserCompany>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.UserCompanies)
                .HasForeignKey(uc => uc.UserId);

            modelBuilder.Entity<UserCompany>()
                .HasOne(uc => uc.Company)
                .WithMany(c => c.UserCompanies)
                .HasForeignKey(uc => uc.CompanyId);
        }
    }
}