using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels;

public class LeadConvertViewModel
{
    public int LeadId { get; set; }

    [Required]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    [Display(Name = "Company")]
    public string? CompanyName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Create Sales Opportunity")]
    public bool CreateOpportunity { get; set; } = true;

    [Display(Name = "Opportunity Name")]
    public string? OpportunityName { get; set; }

    [Display(Name = "Deal Amount")]
    [Range(0, 1000000000, ErrorMessage = "Amount must be positive")]
    public decimal OpportunityAmount { get; set; }

    [Display(Name = "Expected Close Date")]
    [DataType(DataType.Date)]
    public DateTime? ExpectedCloseDate { get; set; }
}
