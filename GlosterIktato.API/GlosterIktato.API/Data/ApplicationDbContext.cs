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
        public DbSet<DocumentHistory> DocumentHistories { get; set; }
        public DbSet<DocumentComment> DocumentComments { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserCompany> UserCompanies { get; set; }
        public DbSet<DocumentRelation> DocumentRelations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserRole many-to-many
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

            // UserCompany many-to-many
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

            modelBuilder.Entity<Document>()
                .HasOne(d => d.CreatedBy)
                .WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict); // Cascade delete tiltása

            modelBuilder.Entity<Document>()
                .HasOne(d => d.AssignedTo)
                .WithMany()
                .HasForeignKey(d => d.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.ModifiedBy)
                .WithMany()
                .HasForeignKey(d => d.ModifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentHistory>()
                .HasOne(dh => dh.Document)
                .WithMany(d => d.History)
                .HasForeignKey(dh => dh.DocumentId)
                .OnDelete(DeleteBehavior.Cascade); // Ha Document törlődik, History is

            modelBuilder.Entity<DocumentHistory>()
                .HasOne(dh => dh.User)
                .WithMany()
                .HasForeignKey(dh => dh.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentComment>()
                .HasOne(dc => dc.Document)
                .WithMany()
                .HasForeignKey(dc => dc.DocumentId)
                .OnDelete(DeleteBehavior.Cascade); // Document törléskor Comments is törlődik

            modelBuilder.Entity<DocumentComment>()
                .HasOne(dc => dc.User)
                .WithMany()
                .HasForeignKey(dc => dc.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // DocumentRelation konfiguráció
            modelBuilder.Entity<DocumentRelation>(entity =>
            {
                // Primary key
                entity.HasKey(dr => dr.Id);

                // DocumentId -> Document kapcsolat
                entity.HasOne(dr => dr.Document)
                    .WithMany(d => d.DocumentRelations)
                    .HasForeignKey(dr => dr.DocumentId)
                    .OnDelete(DeleteBehavior.Restrict); // Ne törölje a kapcsolatot ha a dokumentumot törlik

                // RelatedDocumentId -> Document kapcsolat
                entity.HasOne(dr => dr.RelatedDocument)
                    .WithMany(d => d.RelatedToDocuments)
                    .HasForeignKey(dr => dr.RelatedDocumentId)
                    .OnDelete(DeleteBehavior.Restrict); // Ne törölje a kapcsolatot ha a dokumentumot törlik

                // CreatedBy kapcsolat
                entity.HasOne(dr => dr.CreatedBy)
                    .WithMany()
                    .HasForeignKey(dr => dr.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Index a gyorsabb kereséshez
                entity.HasIndex(dr => dr.DocumentId);
                entity.HasIndex(dr => dr.RelatedDocumentId);

                // Unique constraint - ugyanaz a kapcsolat ne létezzen kétszer
                entity.HasIndex(dr => new { dr.DocumentId, dr.RelatedDocumentId })
                    .IsUnique();
            });

            // INDEXEK (Performance optimalizálás)

            modelBuilder.Entity<Document>()
                .HasIndex(d => d.ArchiveNumber)
                .IsUnique(); // Iktatószám egyedi kell legyen

            modelBuilder.Entity<Document>()
                .HasIndex(d => d.Status);

            modelBuilder.Entity<Document>()
                .HasIndex(d => d.CreatedAt);

            modelBuilder.Entity<Document>()
                .HasIndex(d => new { d.CompanyId, d.DocumentTypeId, d.CreatedAt });

            modelBuilder.Entity<DocumentHistory>()
                .HasIndex(dh => new { dh.DocumentId, dh.CreatedAt });

            modelBuilder.Entity<DocumentComment>()
                .HasIndex(dc => new { dc.DocumentId, dc.CreatedAt });
        }
    }
}