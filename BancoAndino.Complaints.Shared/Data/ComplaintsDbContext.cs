using Microsoft.EntityFrameworkCore;
using BancoAndino.Complaints.Shared.Models;

namespace BancoAndino.Complaints.Shared.Data;

public class ComplaintsDbContext : DbContext
{
    public ComplaintsDbContext(DbContextOptions<ComplaintsDbContext> options) : base(options)
    {
    }

    public DbSet<Complaint> Complaints { get; set; } = null!;
    public DbSet<ComplaintStatus> ComplaintStatuses { get; set; } = null!;
    public DbSet<ComplaintCategory> ComplaintCategories { get; set; } = null!;
    public DbSet<Evidence> Evidences { get; set; } = null!;
    public DbSet<ComplaintHistory> ComplaintHistories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Complaint entity
        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.HasKey(e => e.ComplaintId);
            entity.Property(e => e.CustomerId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Priority).HasMaxLength(20);
            entity.Property(e => e.AssignedTo).HasMaxLength(100);

            entity.HasOne(e => e.Status)
                .WithMany(s => s.Complaints)
                .HasForeignKey(e => e.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Complaints)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Evidences)
                .WithOne(ev => ev.Complaint)
                .HasForeignKey(ev => ev.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.History)
                .WithOne(h => h.Complaint)
                .HasForeignKey(h => h.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ComplaintStatus entity
        modelBuilder.Entity<ComplaintStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId);
            entity.Property(e => e.StatusName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
        });

        // Configure ComplaintCategory entity
        modelBuilder.Entity<ComplaintCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DefaultPriority).HasMaxLength(20);
        });

        // Configure Evidence entity
        modelBuilder.Entity<Evidence>(entity =>
        {
            entity.HasKey(e => e.EvidenceId);
            entity.Property(e => e.BlobUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.UploadedBy).HasMaxLength(100);
        });

        // Configure ComplaintHistory entity
        modelBuilder.Entity<ComplaintHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId);
            entity.Property(e => e.ChangedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        // Seed initial data
        modelBuilder.Entity<ComplaintStatus>().HasData(
            new ComplaintStatus { StatusId = 1, StatusName = "New", Description = "Newly created complaint", DisplayOrder = 1 },
            new ComplaintStatus { StatusId = 2, StatusName = "In Progress", Description = "Complaint is being worked on", DisplayOrder = 2 },
            new ComplaintStatus { StatusId = 3, StatusName = "Pending Customer", Description = "Waiting for customer response", DisplayOrder = 3 },
            new ComplaintStatus { StatusId = 4, StatusName = "Resolved", Description = "Complaint has been resolved", DisplayOrder = 4 },
            new ComplaintStatus { StatusId = 5, StatusName = "Closed", Description = "Complaint is closed", DisplayOrder = 5 }
        );

        modelBuilder.Entity<ComplaintCategory>().HasData(
            new ComplaintCategory { CategoryId = 1, CategoryName = "Account Issues", DefaultPriority = "High", SlaHours = 24 },
            new ComplaintCategory { CategoryId = 2, CategoryName = "Card Problems", DefaultPriority = "High", SlaHours = 24 },
            new ComplaintCategory { CategoryId = 3, CategoryName = "Transaction Dispute", DefaultPriority = "High", SlaHours = 48 },
            new ComplaintCategory { CategoryId = 4, CategoryName = "Service Quality", DefaultPriority = "Medium", SlaHours = 72 },
            new ComplaintCategory { CategoryId = 5, CategoryName = "Other", DefaultPriority = "Low", SlaHours = 96 }
        );
    }
}
