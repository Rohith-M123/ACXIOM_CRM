using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class LeadController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<LeadController> _logger;

    public LeadController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IAuditService auditService,
        ILogger<LeadController> logger)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
        _logger = logger;
    }

    // GET /Lead
    public async Task<IActionResult> Index(string? search, string? status, string? priority)
    {
        var query = _context.Leads.AsNoTracking().AsQueryable();

        // Scope by role
        if (User.IsInRole("Sales Executive"))
        {
            var user = User.Identity?.Name;
            query = query.Where(l => l.AssignedTo == user || l.OwnerId == user);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(l => l.LeadName.Contains(search) ||
                                     (l.CompanyName != null && l.CompanyName.Contains(search)) ||
                                     l.Email.Contains(search) ||
                                     l.Phone.Contains(search) ||
                                     l.LeadCode.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(l => l.Priority == priority);
        }

        var leads = await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
        ViewData["CurrentSearch"] = search;
        ViewData["CurrentStatus"] = status;
        ViewData["CurrentPriority"] = priority;

        return View(leads);
    }

    // GET /Lead/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var lead = await _context.Leads
            .Include(l => l.FollowUps)
            .Include(l => l.Activities)
            .Include(l => l.Opportunities)
            .FirstOrDefaultAsync(l => l.LeadId == id);

        if (lead == null) return NotFound();

        return View(lead);
    }

    // GET /Lead/Create
    public async Task<IActionResult> Create()
    {
        await PopulateUsersDropdownAsync();
        var model = new Lead
        {
            LeadCode = "LEAD-" + new Random().Next(1000, 9999),
            Status = "New",
            Priority = "Medium",
            Source = "Website",
            ExpectedValue = 10000m,
            AssignedTo = User.Identity?.Name
        };
        return View(model);
    }

    // POST /Lead/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lead lead)
    {
        if (lead.ExpectedValue < 0)
        {
            ModelState.AddModelError("ExpectedValue", "Expected value cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(lead.LeadCode))
        {
            lead.LeadCode = "LEAD-" + new Random().Next(1000, 9999);
        }

        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync();
            return View(lead);
        }

        lead.CreatedDate = DateTime.UtcNow;
        lead.OwnerId = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(lead.AssignedTo))
        {
            lead.AssignedTo = User.Identity?.Name;
        }

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "Anonymous",
            "Create",
            "Lead",
            lead.LeadId.ToString(),
            null,
            $"Created Lead {lead.LeadName} ({lead.LeadCode}) with value ${lead.ExpectedValue:N0}");

        TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Lead/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        await PopulateUsersDropdownAsync();
        return View(lead);
    }

    // POST /Lead/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Lead model)
    {
        if (id != model.LeadId) return BadRequest();

        var existing = await _context.Leads.FindAsync(id);
        if (existing == null) return NotFound();

        if (model.ExpectedValue < 0)
        {
            ModelState.AddModelError("ExpectedValue", "Expected value cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync();
            return View(model);
        }

        var oldValue = $"Status: {existing.Status}, Value: {existing.ExpectedValue}, AssignedTo: {existing.AssignedTo}";

        existing.LeadName = model.LeadName;
        existing.Email = model.Email;
        existing.Phone = model.Phone;
        existing.CompanyName = model.CompanyName;
        existing.Source = model.Source;
        existing.Status = model.Status;
        existing.Priority = model.Priority;
        existing.ExpectedValue = model.ExpectedValue;
        existing.AssignedTo = model.AssignedTo;

        await _context.SaveChangesAsync();

        var newValue = $"Status: {existing.Status}, Value: {existing.ExpectedValue}, AssignedTo: {existing.AssignedTo}";

        await _auditService.LogAsync(
            User.Identity?.Name ?? "Anonymous",
            "Update",
            "Lead",
            existing.LeadId.ToString(),
            oldValue,
            newValue);

        TempData["SuccessMessage"] = "Lead updated successfully.";
        return RedirectToAction(nameof(Details), new { id = existing.LeadId });
    }

    // GET /Lead/Convert/5
    public async Task<IActionResult> Convert(int? id)
    {
        if (id == null) return NotFound();

        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        var vm = new LeadConvertViewModel
        {
            LeadId = lead.LeadId,
            LeadName = lead.LeadName,
            CompanyName = lead.CompanyName,
            Email = lead.Email,
            Phone = lead.Phone,
            CreateOpportunity = true,
            OpportunityName = $"{lead.LeadName} - Enterprise Deal",
            OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 25000m,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
        };

        return View(vm);
    }

    // POST /Lead/Convert
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(LeadConvertViewModel model)
    {
        var lead = await _context.Leads.FindAsync(model.LeadId);
        if (lead == null) return NotFound();

        if (model.CreateOpportunity && model.OpportunityAmount <= 0)
        {
            ModelState.AddModelError("OpportunityAmount", "Opportunity amount must be greater than 0.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // 1. Create or Find Customer
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == model.Email);
        if (customer == null)
        {
            customer = new Customer
            {
                CustomerCode = "CUST-" + new Random().Next(1000, 9999),
                CustomerName = model.LeadName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Status = "Active",
                CreatedBy = User.Identity?.Name,
                OwnerId = lead.AssignedTo ?? User.Identity?.Name,
                CreatedDate = DateTime.UtcNow
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        // 2. Optionally Create Opportunity
        Opportunity? opp = null;
        if (model.CreateOpportunity)
        {
            opp = new Opportunity
            {
                OpportunityName = string.IsNullOrWhiteSpace(model.OpportunityName) ? $"{lead.LeadName} Deal" : model.OpportunityName,
                CustomerId = customer.CustomerId,
                LeadId = lead.LeadId,
                Amount = model.OpportunityAmount,
                Stage = "Qualification",
                Probability = 25,
                ExpectedCloseDate = model.ExpectedCloseDate ?? DateTime.UtcNow.AddDays(30),
                Status = "Open",
                AssignedTo = lead.AssignedTo ?? User.Identity?.Name,
                CreatedDate = DateTime.UtcNow
            };
            _context.Opportunities.Add(opp);
            await _context.SaveChangesAsync();
        }

        // 3. Mark Lead as Converted
        lead.Status = "Converted";
        lead.ConvertedCustomerId = customer.CustomerId;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "Anonymous",
            "Lead Conversion",
            "Lead",
            lead.LeadId.ToString(),
            "Qualified",
            $"Converted Lead to Customer {customer.CustomerName} (Id: {customer.CustomerId}) and Opportunity {(opp != null ? opp.OpportunityName : "None")}");

        TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' successfully converted to Customer '{customer.CustomerName}'!";
        return RedirectToAction("Details", "Customer", new { id = customer.CustomerId });
    }

    // POST /Lead/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        lead.Status = "Lost";
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "Anonymous",
            "Delete",
            "Lead",
            id.ToString(),
            null,
            $"Marked Lead {lead.LeadName} as Lost");

        TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' status marked as Lost.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateUsersDropdownAsync()
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Email", "Email");
    }
}
