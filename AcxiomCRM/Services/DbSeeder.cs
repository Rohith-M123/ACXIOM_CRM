using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AcxiomCRM.Services;

/// <summary>
/// Seeds the database with required roles, default user accounts, and initial CRM demonstration data.
/// </summary>
public static class DbSeeder
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string SalesExecutive = "Sales Executive";

        public static readonly string[] All = [Admin, Manager, SalesExecutive];
    }

    public static async Task SeedAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // --- 1. Seed Roles ---
            foreach (var roleName in Roles.All)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                        logger.LogInformation("Created role: {Role}", roleName);
                }
            }

            // --- 2. Seed Default Accounts (Admin, Manager, Sales Executive) ---
            var defaultUsers = new[]
            {
                (Email: config["SeedAdmin:Email"] ?? "admin@acxiomcrm.local",
                 Password: string.IsNullOrWhiteSpace(config["SeedAdmin:Password"]) ? "Admin@123!" : config["SeedAdmin:Password"]!,
                 Role: Roles.Admin),
                (Email: "manager@acxiomcrm.local", Password: "Manager@123!", Role: Roles.Manager),
                (Email: "sales@acxiomcrm.local", Password: "Sales@123!", Role: Roles.SalesExecutive)
            };

            foreach (var (email, password, role) in defaultUsers)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };
                    var createResult = await userManager.CreateAsync(user, password);
                    if (createResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, role);
                        logger.LogInformation("Seeded account: {Email} ({Role})", email, role);
                    }
                }
                else
                {
                    if (!await userManager.IsInRoleAsync(user, role))
                    {
                        await userManager.AddToRoleAsync(user, role);
                    }
                }
            }

            // --- 3. Seed Sample CRM Entities if database has no customers ---
            if (!await db.Customers.AnyAsync())
            {
                logger.LogInformation("Seeding initial CRM demonstration data...");

                var salesUser = await userManager.FindByEmailAsync("sales@acxiomcrm.local");
                var salesUserId = salesUser?.Id ?? "sales@acxiomcrm.local";
                var adminUser = await userManager.FindByEmailAsync("admin@acxiomcrm.local");
                var adminUserId = adminUser?.Id ?? "admin@acxiomcrm.local";

                // Customers
                var c1 = new Customer
                {
                    CustomerCode = "CUST-1001",
                    CustomerName = "Nexus Cloud Solutions",
                    Email = "contact@nexuscloud.io",
                    Phone = "+1-555-010-4491",
                    CompanyName = "Nexus Cloud Technologies",
                    Address = "100 Innovation Way, Suite 400",
                    City = "San Jose",
                    State = "California",
                    Status = "Active",
                    CreatedBy = adminUserId,
                    OwnerId = salesUserId,
                    CreatedDate = DateTime.UtcNow.AddDays(-30)
                };

                var c2 = new Customer
                {
                    CustomerCode = "CUST-1002",
                    CustomerName = "Vertex Financial Partners",
                    Email = "operations@vertexfinance.com",
                    Phone = "+1-555-010-8832",
                    CompanyName = "Vertex Group Ltd",
                    Address = "45 Wall St, 18th Floor",
                    City = "New York",
                    State = "New York",
                    Status = "Active",
                    CreatedBy = adminUserId,
                    OwnerId = salesUserId,
                    CreatedDate = DateTime.UtcNow.AddDays(-20)
                };

                var c3 = new Customer
                {
                    CustomerCode = "CUST-1003",
                    CustomerName = "Apex Logistics International",
                    Email = "info@apexlogistics.com",
                    Phone = "+1-555-010-3321",
                    CompanyName = "Apex Global Corp",
                    Address = "720 Harbor Blvd",
                    City = "Chicago",
                    State = "Illinois",
                    Status = "Active",
                    CreatedBy = adminUserId,
                    OwnerId = salesUserId,
                    CreatedDate = DateTime.UtcNow.AddDays(-15)
                };

                db.Customers.AddRange(c1, c2, c3);
                await db.SaveChangesAsync();

                // Leads
                var l1 = new Lead
                {
                    LeadCode = "LEAD-2001",
                    LeadName = "Sarah Jenkins",
                    Email = "s.jenkins@bluehorizon.com",
                    Phone = "+1-555-019-8811",
                    CompanyName = "Blue Horizon Retail",
                    Source = "Website",
                    Status = "Qualified",
                    Priority = "High",
                    ExpectedValue = 75000m,
                    AssignedTo = salesUserId,
                    CreatedDate = DateTime.UtcNow.AddDays(-12)
                };

                var l2 = new Lead
                {
                    LeadCode = "LEAD-2002",
                    LeadName = "Marcus Vance",
                    Email = "m.vance@vancemedical.org",
                    Phone = "+1-555-018-7722",
                    CompanyName = "Vance Healthcare Systems",
                    Source = "Referral",
                    Status = "Contacted",
                    Priority = "Medium",
                    ExpectedValue = 120000m,
                    AssignedTo = salesUserId,
                    CreatedDate = DateTime.UtcNow.AddDays(-8)
                };

                var l3 = new Lead
                {
                    LeadCode = "LEAD-2003",
                    LeadName = "Elena Rostova",
                    Email = "elena@quantumscale.tech",
                    Phone = "+1-555-017-3344",
                    CompanyName = "QuantumScale AI",
                    Source = "Campaign",
                    Status = "New",
                    Priority = "High",
                    ExpectedValue = 95000m,
                    AssignedTo = salesUserId,
                    CreatedDate = DateTime.UtcNow.AddDays(-3)
                };

                var l4 = new Lead
                {
                    LeadCode = "LEAD-2004",
                    LeadName = "David Chen",
                    Email = "dchen@chenadvisors.net",
                    Phone = "+1-555-016-5566",
                    CompanyName = "Chen & Associates",
                    Source = "Cold Call",
                    Status = "Lost",
                    Priority = "Low",
                    ExpectedValue = 30000m,
                    AssignedTo = salesUserId,
                    CreatedDate = DateTime.UtcNow.AddDays(-18)
                };

                db.Leads.AddRange(l1, l2, l3, l4);
                await db.SaveChangesAsync();

                // Opportunities
                var o1 = new Opportunity
                {
                    OpportunityName = "Nexus Enterprise CRM Migration",
                    CustomerId = c1.CustomerId,
                    Amount = 145000m,
                    Stage = "Proposal",
                    Probability = 60,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(25),
                    Status = "Open",
                    AssignedTo = salesUserId,
                    Notes = "High priority enterprise deal, architecture review completed.",
                    CreatedDate = DateTime.UtcNow.AddDays(-15)
                };

                var o2 = new Opportunity
                {
                    OpportunityName = "Vertex Risk Analytics Platform",
                    CustomerId = c2.CustomerId,
                    Amount = 280000m,
                    Stage = "Negotiation",
                    Probability = 80,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(14),
                    Status = "Open",
                    AssignedTo = salesUserId,
                    Notes = "Commercial terms currently under final legal scrutiny.",
                    CreatedDate = DateTime.UtcNow.AddDays(-22)
                };

                var o3 = new Opportunity
                {
                    OpportunityName = "Apex Supply Chain Integration",
                    CustomerId = c3.CustomerId,
                    Amount = 90000m,
                    Stage = "Won",
                    Probability = 100,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(-5),
                    Status = "Won",
                    AssignedTo = salesUserId,
                    Notes = "Contract signed. Implementation kick-off scheduled.",
                    CreatedDate = DateTime.UtcNow.AddDays(-40)
                };

                db.Opportunities.AddRange(o1, o2, o3);
                await db.SaveChangesAsync();

                // Follow-ups
                var f1 = new FollowUp
                {
                    CustomerId = c1.CustomerId,
                    OpportunityId = o1.OpportunityId,
                    FollowUpDate = DateTime.UtcNow.AddDays(2),
                    FollowUpType = "Meeting",
                    Subject = "Proposal Presentation to CTO",
                    Remarks = "Review technical requirements and custom reporting module.",
                    Status = "Planned",
                    AssignedTo = salesUserId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2)
                };

                var f2 = new FollowUp
                {
                    CustomerId = c2.CustomerId,
                    OpportunityId = o2.OpportunityId,
                    FollowUpDate = DateTime.UtcNow.AddDays(4),
                    FollowUpType = "Call",
                    Subject = "Commercial Negotiation Follow-up",
                    Remarks = "Confirm pricing schedule and SLA terms.",
                    Status = "Planned",
                    AssignedTo = salesUserId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1)
                };

                var f3 = new FollowUp
                {
                    LeadId = l2.LeadId,
                    FollowUpDate = DateTime.UtcNow.AddDays(-2),
                    FollowUpType = "Email",
                    Subject = "Introductory Product Deck Follow-up",
                    Remarks = "Checking if they had time to review whitepaper.",
                    Status = "Missed",
                    AssignedTo = salesUserId,
                    CreatedDate = DateTime.UtcNow.AddDays(-5)
                };

                var f4 = new FollowUp
                {
                    CustomerId = c3.CustomerId,
                    OpportunityId = o3.OpportunityId,
                    FollowUpDate = DateTime.UtcNow.AddDays(-10),
                    FollowUpType = "Meeting",
                    Subject = "Final Contract Signing",
                    Remarks = "Signed and onboarded successfully.",
                    Status = "Completed",
                    CompletedDate = DateTime.UtcNow.AddDays(-10),
                    AssignedTo = salesUserId,
                    CreatedDate = DateTime.UtcNow.AddDays(-15)
                };

                db.FollowUps.AddRange(f1, f2, f3, f4);

                // Initial Activities
                var a1 = new Activity
                {
                    ActivityType = "Call",
                    Subject = "Discovery call with Nexus Lead Architect",
                    Description = "Discussed CRM integration with legacy SQL databases.",
                    ActivityDate = DateTime.UtcNow.AddDays(-14),
                    CustomerId = c1.CustomerId,
                    AssignedTo = salesUserId,
                    Status = "Completed"
                };

                var a2 = new Activity
                {
                    ActivityType = "Meeting",
                    Subject = "Product Demo with Vertex Steering Committee",
                    Description = "Demonstrated role-based dashboards and custom reports.",
                    ActivityDate = DateTime.UtcNow.AddDays(-10),
                    CustomerId = c2.CustomerId,
                    AssignedTo = salesUserId,
                    Status = "Completed"
                };

                db.Activities.AddRange(a1, a2);

                // Initial Audit Logs
                var audit1 = new AuditLog
                {
                    UserId = "admin@acxiomcrm.local",
                    Action = "Login",
                    EntityName = "Authentication",
                    RecordId = "admin@acxiomcrm.local",
                    NewValue = "Administrator authenticated successfully",
                    CreatedDate = DateTime.UtcNow.AddDays(-2)
                };

                var audit2 = new AuditLog
                {
                    UserId = "admin@acxiomcrm.local",
                    Action = "Create",
                    EntityName = "Customer",
                    RecordId = "CUST-1001",
                    NewValue = "Created Nexus Cloud Solutions",
                    CreatedDate = DateTime.UtcNow.AddDays(-2)
                };

                db.AuditLogs.AddRange(audit1, audit2);

                await db.SaveChangesAsync();
                logger.LogInformation("Demonstration CRM data seeded successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database seeding.");
            throw;
        }
    }
}
