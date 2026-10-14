using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

/// <summary>
/// Represents a follow-up action scheduled against a customer, lead, or opportunity.
/// </summary>
public class FollowUp
{
    [Key]
    public int FollowUpId { get; set; }

    public int? CustomerId { get; set; }
    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    [ForeignKey("LeadId")]
    public Lead? Lead { get; set; }

    public int? OpportunityId { get; set; }
    [ForeignKey("OpportunityId")]
    public Opportunity? Opportunity { get; set; }

    [Required(ErrorMessage = "Follow-up Date is required")]
    [DataType(DataType.DateTime)]
    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);

    [Required]
    [StringLength(40)]
    public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [StringLength(200)]
    public string? Subject { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

    [StringLength(450)]
    public string? AssignedTo { get; set; } // Sales Executive

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedDate { get; set; }
}
