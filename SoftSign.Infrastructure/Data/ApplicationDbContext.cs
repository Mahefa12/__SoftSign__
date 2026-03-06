using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SoftSign.Domain.Common;
using SoftSign.Domain.Entities;

namespace SoftSign.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentSignature> DocumentSignatures => Set<DocumentSignature>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<DocumentActivity> DocumentActivities => Set<DocumentActivity>();
    public DbSet<SignatureZone> SignatureZones => Set<SignatureZone>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<DocumentWorkflowStep> DocumentWorkflowSteps => Set<DocumentWorkflowStep>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    /// <summary>
    /// Returns Users DbSet without query filters - critical for Identity login performance.
    /// This bypasses the IsDeleted filter so Identity can efficiently lookup users by email/username.
    /// </summary>
    public DbSet<User> UsersNoFilter => Set<User>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Company
        builder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TradeName).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // User
        builder.Entity<User>(entity =>
        {
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            // Indexes for performance - critical for login queries
            entity.HasIndex(e => e.NormalizedEmail).IsUnique(false);
            entity.HasIndex(e => e.NormalizedUserName).IsUnique();
            
            // Composite index for login query: WHERE IsDeleted = 0 AND NormalizedEmail = @email
            // This is the MOST CRITICAL INDEX for login performance
            entity.HasIndex(e => new { e.NormalizedEmail, e.IsDeleted });
            
            // Index on IsDeleted alone to support the query filter efficiently
            entity.HasIndex(e => e.IsDeleted);
        });

        // Document
        builder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.HasOne(e => e.CreatedBy)
                .WithMany(u => u.CreatedDocuments)
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Company)
                .WithMany(c => c.Documents)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Workflow)
                .WithMany(w => w.Documents)
                .HasForeignKey(e => e.WorkflowId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            // Indexes for common queries
            entity.HasIndex(e => e.CreatedById);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            
            // Composite index for filtering by company and status
            entity.HasIndex(e => new { e.CompanyId, e.Status });
        });

        // DocumentSignature
        builder.Entity<DocumentSignature>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Document)
                .WithMany(d => d.Signatures)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Signer)
                .WithMany(u => u.Signatures)
                .HasForeignKey(e => e.SignerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes for common queries
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.SignerId);
        });

        // SignatureZone
        builder.Entity<SignatureZone>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Document)
                .WithMany(d => d.SignatureZones)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AssignedUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.SignedByUser)
                .WithMany()
                .HasForeignKey(e => e.SignedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Index for common queries
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.SignedByUserId);
        });

        // DocumentVersion
        builder.Entity<DocumentVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.HasOne(e => e.Document)
                .WithMany(d => d.Versions)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // DocumentActivity
        builder.Entity<DocumentActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Document)
                .WithMany(d => d.Activities)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Workflow
        builder.Entity<Workflow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // WorkflowStep
        builder.Entity<WorkflowStep>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Workflow)
                .WithMany(w => w.Steps)
                .HasForeignKey(e => e.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AssignedUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // DocumentWorkflowStep - stores custom workflow steps for specific documents
        builder.Entity<DocumentWorkflowStep>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Document)
                .WithMany(d => d.WorkflowSteps)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.OriginalWorkflowStep)
                .WithMany()
                .HasForeignKey(e => e.WorkflowStepId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CustomAssignedUser)
                .WithMany()
                .HasForeignKey(e => e.CustomAssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // UserNotification
        builder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Document)
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Rename Identity tables
        builder.Entity<User>().ToTable("Users");
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
