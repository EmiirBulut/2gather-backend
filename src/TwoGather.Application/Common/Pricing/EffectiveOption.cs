namespace TwoGather.Application.Common.Pricing;

public record OptionPriceInfo(string Title, decimal? Price, string? Currency, bool IsFinal, bool IsSelected);

/// <summary>
/// Single source of truth for which option's price represents an item in budgets and reports:
/// final decision first, then the selected option, then the most expensive alternative.
/// Options without a price are ignored.
/// </summary>
public static class EffectiveOption
{
    public static OptionPriceInfo? Pick(IEnumerable<OptionPriceInfo> options)
        => options
            .Where(o => o.Price.HasValue)
            .OrderByDescending(o => o.IsFinal)
            .ThenByDescending(o => o.IsSelected)
            .ThenByDescending(o => o.Price)
            .FirstOrDefault();

    public static decimal PriceOf(IEnumerable<OptionPriceInfo> options)
        => Pick(options)?.Price ?? 0;
}
