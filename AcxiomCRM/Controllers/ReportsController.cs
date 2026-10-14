using AcxiomCRM.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET /Reports
    public async Task<IActionResult> Index(string? reportType)
    {
        ViewData["ReportType"] = reportType ?? "Pipeline";

        // Preload summaries
        ViewBag.Customers = await _context.Customers.AsNoTracking().OrderByDescending(c => c.CreatedDate).ToListAsync();
        ViewBag.Leads = await _context.Leads.AsNoTracking().OrderByDescending(l => l.CreatedDate).ToListAsync();
        ViewBag.Opportunities = await _context.Opportunities.Include(o => o.Customer).AsNoTracking().OrderByDescending(o => o.Amount).ToListAsync();
        ViewBag.FollowUps = await _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).AsNoTracking().OrderBy(f => f.FollowUpDate).ToListAsync();
        ViewBag.AuditLogs = await _context.AuditLogs.AsNoTracking().OrderByDescending(a => a.CreatedDate).Take(50).ToListAsync();

        return View();
    }
}
