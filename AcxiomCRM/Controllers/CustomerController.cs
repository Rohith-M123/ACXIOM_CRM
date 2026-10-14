using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class CustomerController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(ApplicationDbContext context, IAuditService auditService, ILogger<CustomerController> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    // GET /Customer
    public async Task<IActionResult> Index(string? search, string? status)
    {
        var query = _context.Customers.AsNoTracking().AsQueryable();

        // Scope by role: Sales Executive only sees assigned or created customers
        if (User.IsInRole("Sales Executive"))
        {
            var user = User.Identity?.Name;
            query = query.Where(c => c.CreatedBy == user || c.OwnerId == user);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c => c.CustomerName.Contains(search) ||
                                     c.Email.Contains(search) ||
                                     c.Phone.Contains(search) ||
                                     (c.CompanyName != null && c.CompanyName.Contains(search)) ||
                                     c.CustomerCode.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        var customers = await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
        ViewData["CurrentSearch"] = search;
        ViewData["CurrentStatus"] = status;

        return View(customers);
    }

    // GET /Customer/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var customer = await _context.Customers
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
            .Include(c => c.Activities)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null) return NotFound();

        return View(customer);
    }

    // GET /Customer/Create
    public IActionResult Create()
    {
        var model = new Customer
        {
            Status = "Active",
            CustomerCode = "CUST-" + new Random().Next(1000, 9999)
        };
        return View(model);
    }

    // POST /Customer/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        // Uniqueness validation
        if (await _context.Customers.AnyAsync(c => c.Email == customer.Email))
        {
            ModelState.AddModelError("Email", "A customer with this email address already exists.");
        }

        if (await _context.Customers.AnyAsync(c => c.Phone == customer.Phone))
        {
            ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
        }

        if (string.IsNullOrWhiteSpace(customer.CustomerCode))
        {
            customer.CustomerCode = "CUST-" + new Random().Next(1000, 9999);
        }

        if (!ModelState.IsValid)
        {
            return View(customer);
        }

        customer.CreatedDate = DateTime.UtcNow;
        customer.CreatedBy = User.Identity?.Name;
        customer.OwnerId = User.Identity?.Name;

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "Anonymous",
            "Create",
            "Customer",
            customer.CustomerId.ToString(),
            null,
            $"Created Customer {customer.CustomerName} ({customer.CustomerCode})");

        TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Customer/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        return View(customer);
    }

    // POST /Customer/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer model)
    {
        if (id != model.CustomerId) return BadRequest();

        var existing = await _context.Customers.FindAsync(id);
        if (existing == null) return NotFound();

        // Unique email check excluding self
        if (await _context.Customers.AnyAsync(c => c.Email == model.Email && c.CustomerId != id))
        {
            ModelState.AddModelError("Email", "Another customer with this email address already exists.");
        }

        if (await _context.Customers.AnyAsync(c => c.Phone == model.Phone && c.CustomerId != id))
        {
            ModelState.AddModelError("Phone", "Another customer with this phone number already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var oldValue = $"Name: {existing.CustomerName}, Status: {existing.Status}, Phone: {existing.Phone}";

        existing.CustomerName = model.CustomerName;
        existing.Email = model.Email;
        existing.Phone = model.Phone;
        existing.CompanyName = model.CompanyName;
        existing.Address = model.Address;
        existing.City = model.City;
        existing.State = model.State;
        existing.Status = model.Status;
        existing.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var newValue = $"Name: {existing.CustomerName}, Status: {existing.Status}, Phone: {existing.Phone}";

        await _auditService.LogAsync(
            User.Identity?.Name ?? "Anonymous",
            "Update",
            "Customer",
            existing.CustomerId.ToString(),
            oldValue,
            newValue);

        TempData["SuccessMessage"] = "Customer details updated successfully.";
        return RedirectToAction(nameof(Details), new { id = existing.CustomerId });
    }

    // POST /Customer/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        // Soft deactivation or deletion
        customer.Status = "Inactive";
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "Anonymous",
            "Delete",
            "Customer",
            id.ToString(),
            "Active",
            "Inactive");

        TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' has been deactivated.";
        return RedirectToAction(nameof(Index));
    }
}
