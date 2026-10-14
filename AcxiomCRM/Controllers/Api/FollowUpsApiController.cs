using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.DTOs;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/followups")]
public class FollowUpsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public FollowUpsApiController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET: api/followups
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FollowUpDto>>> GetFollowUps([FromQuery] string? status)
    {
        var query = _context.FollowUps.Include(f => f.Customer).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(f => f.Status == status);
        }

        var list = await query.Select(f => new FollowUpDto
        {
            FollowUpId = f.FollowUpId,
            CustomerId = f.CustomerId,
            CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
            FollowUpDate = f.FollowUpDate,
            FollowUpType = f.FollowUpType,
            Subject = f.Subject,
            Remarks = f.Remarks,
            Status = f.Status,
            AssignedTo = f.AssignedTo
        }).ToListAsync();

        return Ok(list);
    }
}

[ApiController]
[Route("api/reports")]
public class ReportsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/reports/pipeline
    [HttpGet("pipeline")]
    public async Task<ActionResult<PipelineReportDto>> GetPipelineReport()
    {
        var opps = await _context.Opportunities.AsNoTracking().ToListAsync();

        var openOpps = opps.Where(o => o.Status == "Open").ToList();

        var report = new PipelineReportDto
        {
            TotalPipelineValue = openOpps.Sum(o => o.Amount),
            WeightedPipelineValue = openOpps.Sum(o => o.WeightedAmount),
            TotalDeals = opps.Count,
            ByStage = opps.GroupBy(o => o.Stage)
                          .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount))
        };

        return Ok(report);
    }
}
