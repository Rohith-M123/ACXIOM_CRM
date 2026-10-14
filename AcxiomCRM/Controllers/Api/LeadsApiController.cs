using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.DTOs;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/leads")]
public class LeadsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public LeadsApiController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET: api/leads
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeadDto>>> GetLeads([FromQuery] string? status)
    {
        var query = _context.Leads.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        var list = await query.Select(l => new LeadDto
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status,
            Priority = l.Priority,
            ExpectedValue = l.ExpectedValue,
            AssignedTo = l.AssignedTo,
            CreatedDate = l.CreatedDate
        }).ToListAsync();

        return Ok(list);
    }

    // POST: api/leads
    [HttpPost]
    public async Task<ActionResult<LeadDto>> CreateLead([FromBody] LeadCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.LeadName))
            return BadRequest(new { message = "Lead Name is required." });

        if (dto.ExpectedValue < 0)
            return BadRequest(new { message = "Expected Value cannot be negative." });

        var lead = new Lead
        {
            LeadCode = "LEAD-" + new Random().Next(1000, 9999),
            LeadName = dto.LeadName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Source = string.IsNullOrWhiteSpace(dto.Source) ? "Website" : dto.Source,
            Priority = string.IsNullOrWhiteSpace(dto.Priority) ? "Medium" : dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            Status = "New",
            AssignedTo = dto.AssignedTo,
            CreatedDate = DateTime.UtcNow
        };

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "API User",
            "Create",
            "Lead",
            lead.LeadId.ToString(),
            null,
            $"API Created Lead {lead.LeadName}");

        var resultDto = new LeadDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            AssignedTo = lead.AssignedTo,
            CreatedDate = lead.CreatedDate
        };

        return CreatedAtAction(nameof(GetLeads), new { id = lead.LeadId }, resultDto);
    }
}
