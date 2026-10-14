using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data;

/// <summary>
/// The main EF Core database context for AcxiomCRM.
/// Extends IdentityDbContext to include ASP.NET Core Identity tables and all CRM domain entities.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Customer Unique Constraints
        builder.Entity<Customer>(entity =>
        {
            entity.HasIndex(c => c.Email).IsUnique();
            entity.HasIndex(c => c.Phone).IsUnique();
            entity.HasIndex(c => c.CustomerCode).IsUnique();
        });

        // Lead Index
        builder.Entity<Lead>(entity =>
        {
            entity.HasIndex(l => l.LeadCode).IsUnique();
            entity.Property(l => l.ExpectedValue).HasPrecision(18, 2);
        });

        // Opportunity Configurations
        builder.Entity<Opportunity>(entity =>
        {
            entity.Property(o => o.Amount).HasPrecision(18, 2);

            entity.HasOne(o => o.Customer)
                  .WithMany(c => c.Opportunities)
                  .HasForeignKey(o => o.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Lead)
                  .WithMany(l => l.Opportunities)
                  .HasForeignKey(o => o.LeadId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // FollowUp Configurations
        builder.Entity<FollowUp>(entity =>
        {
            entity.HasOne(f => f.Customer)
                  .WithMany(c => c.FollowUps)
                  .HasForeignKey(f => f.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Lead)
                  .WithMany(l => l.FollowUps)
                  .HasForeignKey(f => f.LeadId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(f => f.Opportunity)
                  .WithMany(o => o.FollowUps)
                  .HasForeignKey(f => f.OpportunityId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Activity Configurations
        builder.Entity<Activity>(entity =>
        {
            entity.HasOne(a => a.Customer)
                  .WithMany(c => c.Activities)
                  .HasForeignKey(a => a.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Lead)
                  .WithMany(l => l.Activities)
                  .HasForeignKey(a => a.LeadId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // AuditLog Index for quick reporting and filtering
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.Action);
            entity.HasIndex(a => a.EntityName);
            entity.HasIndex(a => a.CreatedDate);
        });
    }
}
