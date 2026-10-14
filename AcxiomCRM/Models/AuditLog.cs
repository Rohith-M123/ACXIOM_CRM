using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

/// <summary>
/// Append-only audit record tracking security and business events.
/// </summary>
public class AuditLog
{
    [Key]
    public int AuditLogId { get; set; }

    [Required]
    [StringLength(256)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string Action { get; set; } = string.Empty; // Login, Failed Login, Logout, Create, Update, Delete, Role Change, Security

    [Required]
    [StringLength(100)]
    public string EntityName { get; set; } = string.Empty; // Customer, Lead, Opportunity, FollowUp, User

    [StringLength(100)]
    public string? RecordId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    public string? IpAddress { get; set; }
}
