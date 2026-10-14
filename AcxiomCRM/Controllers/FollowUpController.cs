using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class FollowUpController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAuditService _auditService;

    public FollowUpController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    // GET /FollowUp
    public async Task<IActionResult> Index(string? filter, string? search)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .AsNoTracking()
            .AsQueryable();

        if (User.IsInRole("Sales Executive"))
        {
            var user = User.Identity?.Name;
            query = query.Where(f => f.AssignedTo == user);
        }

        var now = DateTime.UtcNow;

        if (filter == "Upcoming")
        {
            query = query.Where(f => f.Status == "Planned" && f.FollowUpDate >= now);
        }
        else if (filter == "Overdue")
        {
            query = query.Where(f => (f.Status == "Planned" || f.Status == "Missed") && f.FollowUpDate < now);
        }
        else if (filter == "Completed")
        {
            query = query.Where(f => f.Status == "Completed");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(f => (f.Subject != null && f.Subject.Contains(search)) ||
                                     (f.Remarks != null && f.Remarks.Contains(search)) ||
                                     (f.Customer != null && f.Customer.CustomerName.Contains(search)) ||
                                     (f.Lead != null && f.Lead.LeadName.Contains(search)));
        }

        var list = await query.OrderBy(f => f.FollowUpDate).ToListAsync();
        ViewData["CurrentFilter"] = filter ?? "All";
        ViewData["CurrentSearch"] = search;

        return View(list);
    }

    // GET /FollowUp/Create
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
    {
        await PopulateDropdownsAsync();
        var model = new FollowUp
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
            FollowUpDate = DateTime.UtcNow.AddDays(1),
            FollowUpType = "Call",
            Status = "Planned",
            AssignedTo = User.Identity?.Name
        };
        return View(model);
    }

    // POST /FollowUp/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUp model)
    {
        // Business rule: Follow-up date cannot be earlier than today for a new/planned follow-up
        if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today for a planned activity.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(model);
        }

        model.CreatedDate = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(model.AssignedTo))
        {
            model.AssignedTo = User.Identity?.Name;
        }

        _context.FollowUps.Add(model);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "Anonymous",
            "Create",
            "FollowUp",
            model.FollowUpId.ToString(),
            null,
            $"Scheduled {model.FollowUpType} follow-up for {model.FollowUpDate:d}");

        TempData["SuccessMessage"] = "Follow-up scheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST /FollowUp/Complete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? completionNotes)
    {
        var item = await _context.FollowUps.FindAsync(id);
        if (item == null) return NotFound();

        item.Status = "Completed";
        item.CompletedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(completionNotes))
        {
            item.Remarks = string.IsNullOrWhiteSpace(item.Remarks)
                ? $"[Completed] {completionNotes}"
                : $"{item.Remarks}\n[Completed] {completionNotes}";
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "Anonymous",
            "Update",
            "FollowUp",
            id.ToString(),
            "Planned",
            "Completed");

        TempData["SuccessMessage"] = "Follow-up marked as Completed.";
        return RedirectToAction(nameof(Index));
    }

    // POST /FollowUp/Reschedule/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? remarks)
    {
        var item = await _context.FollowUps.FindAsync(id);
        if (item == null) return NotFound();

        if (newDate.Date < DateTime.UtcNow.Date)
        {
            TempData["ErrorMessage"] = "New date cannot be earlier than today.";
            return RedirectToAction(nameof(Index));
        }

        var oldDate = item.FollowUpDate;
        item.FollowUpDate = newDate;
        item.Status = "Planned";
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            item.Remarks = string.IsNullOrWhiteSpace(item.Remarks)
                ? $"[Rescheduled] {remarks}"
                : $"{item.Remarks}\n[Rescheduled] {remarks}";
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "Anonymous",
            "Reschedule",
            "FollowUp",
            id.ToString(),
            oldDate.ToString("g"),
            newDate.ToString("g"));

        TempData["SuccessMessage"] = "Follow-up rescheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST /FollowUp/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var item = await _context.FollowUps.FindAsync(id);
        if (item == null) return NotFound();

        item.Status = "Cancelled";
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "Anonymous",
            "Cancel",
            "FollowUp",
            id.ToString(),
            "Planned",
            "Cancelled");

        TempData["SuccessMessage"] = "Follow-up cancelled.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync()
    {
        var customers = await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName");

        var leads = await _context.Leads.OrderBy(l => l.LeadName).ToListAsync();
        ViewBag.LeadsList = new SelectList(leads, "LeadId", "LeadName");

        var opps = await _context.Opportunities.OrderBy(o => o.OpportunityName).ToListAsync();
        ViewBag.OpportunitiesList = new SelectList(opps, "OpportunityId", "OpportunityName");

        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Email", "Email");
    }
}
