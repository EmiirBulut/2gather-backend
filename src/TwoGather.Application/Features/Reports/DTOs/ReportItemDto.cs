using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Reports.DTOs;

public class ReportItemDto
{
    public Guid Id { get; set; }
    public string? ImageUrl { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SelectedOptionTitle { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal? EstimatedPrice { get; set; }
    public string? Currency { get; set; }
    public ItemStatus Status { get; set; }
}
