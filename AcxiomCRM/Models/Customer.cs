using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

/// <summary>
/// Represents a customer record in AcxiomCRM.
/// </summary>
public class Customer
{
    [Key]
    public int CustomerId { get; set; }

    [Required]
    [StringLength(30)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer Name is required")]
    [StringLength(100, ErrorMessage = "Customer Name cannot exceed 100 characters")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Enter a valid phone number")]
    [StringLength(25)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(150)]
    public string? CompanyName { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    [StringLength(80)]
    public string? City { get; set; }

    [StringLength(80)]
    public string? State { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Active"; // Active, Inactive

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    [StringLength(450)]
    public string? CreatedBy { get; set; } // UserId or email

    [StringLength(450)]
    public string? OwnerId { get; set; } // Assigned Sales Executive UserId

    // Navigation properties
    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
