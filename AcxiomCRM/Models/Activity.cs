using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

/// <summary>
/// Represents sales activities (Calls, Meetings, Emails, Tasks) performed by sales reps.
/// </summary>
public class Activity
{
    [Key]
    public int ActivityId { get; set; }

    [Required]
    [StringLength(40)]
    public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [Required(ErrorMessage = "Subject is required")]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    public int? CustomerId { get; set; }
    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    [ForeignKey("LeadId")]
    public Lead? Lead { get; set; }

    [StringLength(450)]
    public string? AssignedTo { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Completed"; // Completed, Pending
}
