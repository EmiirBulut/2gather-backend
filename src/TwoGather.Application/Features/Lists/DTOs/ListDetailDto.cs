using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Lists.DTOs;

public class ListDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MemberRole CurrentUserRole { get; set; }
    public decimal CompletionPercentage { get; set; }
    public FinancialSummaryDto Financial { get; set; } = new();
    public List<PendingClaimSummaryDto> PendingClaims { get; set; } = new();
    public List<CategorySummaryDto> CategorySummaries { get; set; } = new();
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
