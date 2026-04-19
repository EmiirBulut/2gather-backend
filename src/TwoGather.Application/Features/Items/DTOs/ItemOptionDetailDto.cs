using TwoGather.Application.Features.Claims.DTOs;

namespace TwoGather.Application.Features.Items.DTOs;

public class ItemOptionDetailDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public string? Link { get; set; }
    public string? Notes { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public bool IsSelected { get; set; }
    public bool IsFinal { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal? AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int? CurrentUserScore { get; set; }
    public int ApprovedClaimsTotal { get; set; }
    public int RemainingClaimPercentage { get; set; }
    public List<ClaimDto> Claims { get; set; } = new();
}
