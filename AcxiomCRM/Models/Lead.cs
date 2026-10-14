using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

/// <summary>
/// Represents a prospective sales lead in AcxiomCRM.
/// </summary>
public class Lead
{
    [Key]
    public int LeadId { get; set; }

    [Required]
    [StringLength(30)]
    public string LeadCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lead Name is required")]
    [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters")]
    public string LeadName { get; set; } = string.Empty;

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

    [Required]
    [StringLength(50)]
    public string Source { get; set; } = "Website"; // Website, Referral, Cold Call, Campaign, Other

    [Required]
    [StringLength(40)]
    public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

    [Required]
    [StringLength(30)]
    public string Priority { get; set; } = "Medium"; // Low, Medium, High

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1000000000, ErrorMessage = "Expected value must be a positive number")]
    public decimal ExpectedValue { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [StringLength(450)]
    public string? AssignedTo { get; set; } // UserId or email of Sales Executive

    [StringLength(450)]
    public string? OwnerId { get; set; }

    public int? ConvertedCustomerId { get; set; }

    // Navigation properties
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
}
