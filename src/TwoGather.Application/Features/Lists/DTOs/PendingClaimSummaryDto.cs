namespace TwoGather.Application.Features.Lists.DTOs;

public class PendingClaimSummaryDto
{
    public Guid ClaimId { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string OptionTitle { get; set; } = string.Empty;
    public string ClaimantDisplayName { get; set; } = string.Empty;
    public int Percentage { get; set; }
    public DateTime CreatedAt { get; set; }
}
