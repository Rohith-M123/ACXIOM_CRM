using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.DTOs;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
public class OpportunitiesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public OpportunitiesApiController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET: api/opportunities
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OpportunityDto>>> GetOpportunities([FromQuery] string? stage)
    {
        var query = _context.Opportunities.Include(o => o.Customer).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(stage))
        {
            query = query.Where(o => o.Stage == stage);
        }

        var list = await query.Select(o => new OpportunityDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer != null ? o.Customer.CustomerName : null,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            WeightedAmount = (o.Amount * o.Probability) / 100m,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            AssignedTo = o.AssignedTo
        }).ToListAsync();

        return Ok(list);
    }

    // POST: api/opportunities
    [HttpPost]
    public async Task<ActionResult<OpportunityDto>> CreateOpportunity([FromBody] OpportunityCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.OpportunityName))
            return BadRequest(new { message = "Opportunity Name is required." });

        if (dto.Amount <= 0)
            return BadRequest(new { message = "Opportunity Amount must be greater than 0." });

        if (dto.Probability < 0 || dto.Probability > 100)
            return BadRequest(new { message = "Probability must be between 0 and 100." });

        var opp = new Opportunity
        {
            OpportunityName = dto.OpportunityName,
            CustomerId = dto.CustomerId,
            Amount = dto.Amount,
            Stage = string.IsNullOrWhiteSpace(dto.Stage) ? "Qualification" : dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate == default ? DateTime.UtcNow.AddDays(30) : dto.ExpectedCloseDate,
            Status = "Open",
            AssignedTo = dto.AssignedTo,
            CreatedDate = DateTime.UtcNow
        };

        _context.Opportunities.Add(opp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "API User",
            "Create",
            "Opportunity",
            opp.OpportunityId.ToString(),
            null,
            $"API Created Opportunity {opp.OpportunityName}");

        var resultDto = new OpportunityDto
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            WeightedAmount = (opp.Amount * opp.Probability) / 100m,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            AssignedTo = opp.AssignedTo
        };

        return CreatedAtAction(nameof(GetOpportunities), new { id = opp.OpportunityId }, resultDto);
    }
}
