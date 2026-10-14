using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.DTOs;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/customers")]
public class CustomersApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CustomersApiController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET: api/customers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers([FromQuery] string? search)
    {
        var query = _context.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.CustomerName.Contains(search) || c.Email.Contains(search));
        }

        var list = await query.Select(c => new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            City = c.City,
            State = c.State,
            Status = c.Status,
            CreatedDate = c.CreatedDate
        }).ToListAsync();

        return Ok(list);
    }

    // GET: api/customers/5
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
    {
        var c = await _context.Customers.FindAsync(id);
        if (c == null) return NotFound(new { message = $"Customer with ID {id} not found." });

        return Ok(new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            City = c.City,
            State = c.State,
            Status = c.Status,
            CreatedDate = c.CreatedDate
        });
    }

    // POST: api/customers
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CustomerCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CustomerName))
            return BadRequest(new { message = "Customer Name is required." });

        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { message = "Email is required." });

        if (await _context.Customers.AnyAsync(c => c.Email == dto.Email))
            return Conflict(new { message = "A customer with this email already exists." });

        var customer = new Customer
        {
            CustomerCode = "CUST-" + new Random().Next(1000, 9999),
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Status = "Active",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "API"
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "API User",
            "Create",
            "Customer",
            customer.CustomerId.ToString(),
            null,
            $"API Created Customer {customer.CustomerName}");

        var resultDto = new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            CreatedDate = customer.CreatedDate
        };

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, resultDto);
    }

    // PUT: api/customers/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerCreateDto dto)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { message = $"Customer with ID {id} not found." });

        customer.CustomerName = dto.CustomerName;
        customer.Email = dto.Email;
        customer.Phone = dto.Phone;
        customer.CompanyName = dto.CompanyName;
        customer.Address = dto.Address;
        customer.City = dto.City;
        customer.State = dto.State;
        customer.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "API User",
            "Update",
            "Customer",
            id.ToString(),
            null,
            $"API Updated Customer {customer.CustomerName}");

        return Ok(new { message = "Customer updated successfully." });
    }

    // DELETE: api/customers/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { message = $"Customer with ID {id} not found." });

        customer.Status = "Inactive";
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            User.Identity?.Name ?? "API User",
            "Delete",
            "Customer",
            id.ToString(),
            "Active",
            "Inactive");

        return Ok(new { message = "Customer deactivated successfully." });
    }
}
