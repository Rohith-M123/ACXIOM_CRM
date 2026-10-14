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
public class OpportunityController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAuditService _auditService;

    public OpportunityController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    // GET /Opportunity
    public async Task<IActionResult> Index(string? search, string? stage, string? status)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .AsNoTracking()
            .AsQueryable();

        // Scope by role
        if (User.IsInRole("Sales Executive"))
        {
            var user = User.Identity?.Name;
            query = query.Where(o => o.AssignedTo == user || o.OwnerId == user);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(o => o.OpportunityName.Contains(search) ||
                                     (o.Customer != null && o.Customer.CustomerName.Contains(search)) ||
                                     (o.Lead != null && o.Lead.LeadName.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(stage))
        {
            query = query.Where(o => o.Stage == stage);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        var opportunities = await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
        ViewData["CurrentSearch"] = search;
        ViewData["CurrentStage"] = stage;
        ViewData["CurrentStatus"] = status;

        ViewData["TotalValue"] = opportunities.Sum(o => o.Amount);
        ViewData["WeightedValue"] = opportunities.Sum(o => o.WeightedAmount);

        return View(opportunities);
    }

    // GET /Opportunity/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var opportunity = await _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.FollowUps)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opportunity == null) return NotFound();

        return View(opportunity);
    }

    // GET /Opportunity/Create
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        await PopulateDropdownsAsync();
        var model = new Opportunity
        {
            CustomerId = customerId,
            LeadId = leadId,
            Stage = "Qualification",
            Status = "Open",
            Probability = 20,
            Amount = 10000m,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
            AssignedTo = User.Identity?.Name
        };
        return View(model);
    }

    // POST /Opportunity/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Opportunity opportunity)
    {
        // Business Validations
        if (opportunity.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
        }

        if (opportunity.Probability < 0 || opportunity.Probability > 100)
        {
            ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
        }

        if (opportunity.Status == "Open" && opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past for an active opportunity.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(opportunity);
        }

        opportunity.CreatedDate = DateTime.UtcNow;
        opportunity.OwnerId = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(opportunity.AssignedTo))
        {
            opportunity.AssignedTo = User.Identity?.Name;
        }

        // Automatic Status update based on stage
        if (opportunity.Stage == "Won") opportunity.Status = "Won";
        else if (opportunity.Stage == "Lost") opportunity.Status = "Lost";
        else opportunity.Status = "Open";

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "Anonymous",
            "Create",
            "Opportunity",
            opportunity.OpportunityId.ToString(),
            null,
            $"Created Opportunity {opportunity.OpportunityName} for ${opportunity.Amount:N2} (Stage: {opportunity.Stage})");

        TempData["SuccessMessage"] = $"Opportunity '{opportunity.OpportunityName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Opportunity/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var opportunity = await _context.Opportunities.FindAsync(id);
        if (opportunity == null) return NotFound();

        await PopulateDropdownsAsync();
        return View(opportunity);
    }

    // POST /Opportunity/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Opportunity model)
    {
        if (id != model.OpportunityId) return BadRequest();

        var existing = await _context.Opportunities.FindAsync(id);
        if (existing == null) return NotFound();

        if (model.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
        }

        if (model.Probability < 0 || model.Probability > 100)
        {
            ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(model);
        }

        var oldValue = $"Stage: {existing.Stage}, Amount: {existing.Amount}, Probability: {existing.Probability}%";

        existing.OpportunityName = model.OpportunityName;
        existing.CustomerId = model.CustomerId;
        existing.LeadId = model.LeadId;
        existing.Amount = model.Amount;
        existing.Stage = model.Stage;
        existing.Probability = model.Probability;
        existing.ExpectedCloseDate = model.ExpectedCloseDate;
        existing.Notes = model.Notes;
        existing.AssignedTo = model.AssignedTo;

        if (existing.Stage == "Won") existing.Status = "Won";
        else if (existing.Stage == "Lost") existing.Status = "Lost";
        else existing.Status = "Open";

        await _context.SaveChangesAsync();

        var newValue = $"Stage: {existing.Stage}, Amount: {existing.Amount}, Probability: {existing.Probability}%";

        await _auditService.LogAsync(
            User.Identity?.Name ?? "Anonymous",
            "Update",
            "Opportunity",
            existing.OpportunityId.ToString(),
            oldValue,
            newValue);

        TempData["SuccessMessage"] = "Opportunity updated successfully.";
        return RedirectToAction(nameof(Details), new { id = existing.OpportunityId });
    }

    // POST /Opportunity/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var opp = await _context.Opportunities.FindAsync(id);
        if (opp == null) return NotFound();

        opp.Status = "Lost";
        opp.Stage = "Lost";
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "Anonymous",
            "Delete",
            "Opportunity",
            id.ToString(),
            null,
            $"Marked Opportunity {opp.OpportunityName} as Lost");

        TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' marked as Lost.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync()
    {
        var customers = await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName");

        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Email", "Email");
    }
}
