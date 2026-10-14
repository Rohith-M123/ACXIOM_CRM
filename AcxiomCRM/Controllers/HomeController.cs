using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET / — Public landing page
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(Dashboard));
        return View();
    }

    // GET /Home/Dashboard — Authenticated users only
    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var vm = new DashboardViewModel();
        var currentUserName = User.Identity?.Name;
        bool isSalesExec = User.IsInRole("Sales Executive");

        // Base queries scoped by role
        var customerQuery = _context.Customers.AsNoTracking().AsQueryable();
        var leadQuery = _context.Leads.AsNoTracking().AsQueryable();
        var oppQuery = _context.Opportunities.Include(o => o.Customer).AsNoTracking().AsQueryable();
        var followUpQuery = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).AsNoTracking().AsQueryable();

        if (isSalesExec && !string.IsNullOrWhiteSpace(currentUserName))
        {
            customerQuery = customerQuery.Where(c => c.CreatedBy == currentUserName || c.OwnerId == currentUserName);
            leadQuery = leadQuery.Where(l => l.AssignedTo == currentUserName || l.OwnerId == currentUserName);
            oppQuery = oppQuery.Where(o => o.AssignedTo == currentUserName || o.OwnerId == currentUserName);
            followUpQuery = followUpQuery.Where(f => f.AssignedTo == currentUserName);
        }

        // Metrics
        vm.TotalCustomers = await customerQuery.CountAsync();
        vm.TotalLeads = await leadQuery.CountAsync();
        vm.OpenLeads = await leadQuery.CountAsync(l => l.Status != "Converted" && l.Status != "Lost");

        vm.TotalOpportunities = await oppQuery.CountAsync();
        vm.OpenOpportunities = await oppQuery.CountAsync(o => o.Status == "Open");
        vm.WonOpportunities = await oppQuery.CountAsync(o => o.Status == "Won");
        vm.LostOpportunities = await oppQuery.CountAsync(o => o.Status == "Lost");

        var openOpps = await oppQuery.Where(o => o.Status == "Open").ToListAsync();
        vm.TotalPipelineValue = openOpps.Sum(o => o.Amount);
        vm.WeightedPipelineValue = openOpps.Sum(o => o.WeightedAmount);

        var now = DateTime.UtcNow;
        vm.PendingFollowUpsCount = await followUpQuery.CountAsync(f => f.Status == "Planned");

        // Upcoming Follow-ups
        vm.UpcomingFollowUps = await followUpQuery
            .Where(f => f.Status == "Planned")
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToListAsync();

        // Recent Opportunities
        vm.RecentOpportunities = await oppQuery
            .OrderByDescending(o => o.CreatedDate)
            .Take(5)
            .ToListAsync();

        // Recent Leads
        vm.RecentLeads = await leadQuery
            .OrderByDescending(l => l.CreatedDate)
            .Take(5)
            .ToListAsync();

        // Chart 1: Lead Status Counts
        var allLeads = await leadQuery.ToListAsync();
        vm.LeadStatusCounts = new Dictionary<string, int>
        {
            ["New"] = allLeads.Count(l => l.Status == "New"),
            ["Contacted"] = allLeads.Count(l => l.Status == "Contacted"),
            ["Qualified"] = allLeads.Count(l => l.Status == "Qualified"),
            ["Converted"] = allLeads.Count(l => l.Status == "Converted"),
            ["Lost"] = allLeads.Count(l => l.Status == "Lost")
        };

        // Chart 2: Pipeline Stages Amounts
        var allOpps = await oppQuery.ToListAsync();
        vm.PipelineStageAmounts = new Dictionary<string, decimal>
        {
            ["Qualification"] = allOpps.Where(o => o.Stage == "Qualification").Sum(o => o.Amount),
            ["Proposal"] = allOpps.Where(o => o.Stage == "Proposal").Sum(o => o.Amount),
            ["Negotiation"] = allOpps.Where(o => o.Stage == "Negotiation").Sum(o => o.Amount),
            ["Won"] = allOpps.Where(o => o.Stage == "Won").Sum(o => o.Amount),
            ["Lost"] = allOpps.Where(o => o.Stage == "Lost").Sum(o => o.Amount)
        };

        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
