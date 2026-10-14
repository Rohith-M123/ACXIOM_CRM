using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

/// <summary>
/// Sales Executive-only area. Accessible only to users with the Sales Executive role.
/// </summary>
[Authorize(Roles = "Sales Executive")]
public class SalesController : Controller
{
    private readonly ILogger<SalesController> _logger;

    public SalesController(ILogger<SalesController> logger)
    {
        _logger = logger;
    }

    // GET /Sales
    public IActionResult Index()
    {
        _logger.LogInformation("Sales panel accessed by {User}", User.Identity?.Name);
        return View();
    }
}
