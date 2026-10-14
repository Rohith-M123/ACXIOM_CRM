using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

/// <summary>
/// Admin-only area. Accessible only to users with the Admin role.
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ILogger<AdminController> _logger;

    public AdminController(ILogger<AdminController> logger)
    {
        _logger = logger;
    }

    // GET /Admin
    public IActionResult Index()
    {
        _logger.LogInformation("Admin panel accessed by {User}", User.Identity?.Name);
        return View();
    }
}
