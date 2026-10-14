using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

/// <summary>
/// Manager-only area. Accessible only to users with the Manager role.
/// </summary>
[Authorize(Roles = "Manager")]
public class ManagerController : Controller
{
    private readonly ILogger<ManagerController> _logger;

    public ManagerController(ILogger<ManagerController> logger)
    {
        _logger = logger;
    }

    // GET /Manager
    public IActionResult Index()
    {
        _logger.LogInformation("Manager panel accessed by {User}", User.Identity?.Name);
        return View();
    }
}
