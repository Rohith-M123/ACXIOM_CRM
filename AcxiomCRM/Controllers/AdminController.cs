using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

/// <summary>
/// Administration Panel: User Management, Role Assignment, and Audit Logs.
/// Restricted strictly to users with the Admin role.
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
        _logger = logger;
    }

    // GET /Admin
    public async Task<IActionResult> Index()
    {
        ViewBag.TotalUsers = await _userManager.Users.CountAsync();
        ViewBag.TotalAuditLogs = await _context.AuditLogs.CountAsync();
        ViewBag.TotalCustomers = await _context.Customers.CountAsync();
        ViewBag.TotalOpportunities = await _context.Opportunities.CountAsync();

        return View();
    }

    // GET /Admin/Users
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users.ToListAsync();
        var userList = new List<UserItemViewModel>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            bool isLocked = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow;

            userList.Add(new UserItemViewModel
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                Role = string.Join(", ", roles),
                IsLockedOut = isLocked,
                LockoutEnd = u.LockoutEnd
            });
        }

        return View(userList);
    }

    // GET /Admin/CreateUser
    public IActionResult CreateUser()
    {
        var model = new CreateUserViewModel
        {
            Role = "Sales Executive"
        };
        return View(model);
    }

    // POST /Admin/CreateUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
        {
            ModelState.AddModelError("Email", "A user with this email address already exists.");
            return View(model);
        }

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }
            return View(model);
        }

        // Ensure role exists and add
        if (!await _roleManager.RoleExistsAsync(model.Role))
        {
            await _roleManager.CreateAsync(new IdentityRole(model.Role));
        }
        await _userManager.AddToRoleAsync(user, model.Role);

        await _auditService.LogAsync(
            User.Identity?.Name ?? "Admin",
            "Create",
            "User",
            user.Id,
            null,
            $"Created user {user.Email} with role {model.Role}");

        TempData["SuccessMessage"] = $"User '{model.Email}' created successfully with role '{model.Role}'.";
        return RedirectToAction(nameof(Users));
    }

    // POST /Admin/ToggleLockout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLockout(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        bool isCurrentlyLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;

        if (isCurrentlyLocked)
        {
            // Unlock
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            await _auditService.LogAsync(
                User.Identity?.Name ?? "Admin",
                "Security",
                "User",
                user.Id,
                "Locked",
                "Unlocked");

            TempData["SuccessMessage"] = $"User '{user.Email}' has been unlocked.";
        }
        else
        {
            // Lock out for 30 days
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddDays(30));

            await _auditService.LogAsync(
                User.Identity?.Name ?? "Admin",
                "Security",
                "User",
                user.Id,
                "Active",
                "Locked");

            TempData["SuccessMessage"] = $"User '{user.Email}' has been locked out.";
        }

        return RedirectToAction(nameof(Users));
    }

    // GET /Admin/AuditLogs
    public async Task<IActionResult> AuditLogs(string? user, string? action, string? entity)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(user))
        {
            query = query.Where(a => a.UserId.Contains(user));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(entity))
        {
            query = query.Where(a => a.EntityName == entity);
        }

        var logs = await query.OrderByDescending(a => a.CreatedDate).Take(100).ToListAsync();

        ViewData["CurrentUser"] = user;
        ViewData["CurrentAction"] = action;
        ViewData["CurrentEntity"] = entity;

        return View(logs);
    }
}
