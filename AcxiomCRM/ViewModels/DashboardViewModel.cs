using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class DashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public int PendingFollowUpsCount { get; set; }

    public List<FollowUp> UpcomingFollowUps { get; set; } = new();
    public List<Opportunity> RecentOpportunities { get; set; } = new();
    public List<Lead> RecentLeads { get; set; } = new();

    // Chart Data
    public Dictionary<string, int> LeadStatusCounts { get; set; } = new();
    public Dictionary<string, decimal> PipelineStageAmounts { get; set; } = new();
    public Dictionary<string, decimal> MonthlySalesOutcomes { get; set; } = new();
}
