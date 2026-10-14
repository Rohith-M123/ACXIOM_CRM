using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

/// <summary>
/// Represents a sales opportunity / deal in the CRM pipeline.
/// </summary>
public class Opportunity
{
    [Key]
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required")]
    [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters")]
    public string OpportunityName { get; set; } = string.Empty;

    public int? CustomerId { get; set; }
    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    [ForeignKey("LeadId")]
    public Lead? Lead { get; set; }

    [Required(ErrorMessage = "Opportunity Amount is required")]
    [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(40)]
    public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100")]
    public int Probability { get; set; } = 20;

    [Required(ErrorMessage = "Expected Close Date is required")]
    [DataType(DataType.Date)]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Open"; // Open, Won, Lost

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [StringLength(450)]
    public string? AssignedTo { get; set; } // Sales Executive

    [StringLength(450)]
    public string? OwnerId { get; set; }

    // Calculated Weighted Pipeline Amount
    [NotMapped]
    public decimal WeightedAmount => (Amount * Probability) / 100m;

    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}
