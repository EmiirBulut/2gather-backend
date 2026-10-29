using TwoGather.Domain.Enums;

namespace TwoGather.Application.Common.Pricing;

public record OptionPriceInfo(
    Guid Id,
    string Title,
    decimal? Price,
    string? Currency,
    bool IsFinal,
    bool IsSelected,
    bool IsPurchased);

/// <summary>
/// Single source of truth for which option's price represents an item in budgets and reports
/// (worst-case budgeting): the option that was bought, otherwise the final decision,
/// otherwise the most expensive alternative. Options without a price are ignored.
/// </summary>
public static class EffectiveOption
{
    public static OptionPriceInfo? Pick(IEnumerable<OptionPriceInfo> options, bool isLegacyPurchase = false)
    {
        var priced = options.Where(o => o.Price.HasValue).ToList();

        var purchased = priced.FirstOrDefault(o => o.IsPurchased);
        if (purchased is not null)
            return purchased;

        var ordered = priced.OrderByDescending(o => o.IsFinal);
        // Items purchased before the bought option was recorded keep the old rule, where
        // the selected option counted, so their spent amounts do not change
        if (isLegacyPurchase)
            ordered = ordered.ThenByDescending(o => o.IsSelected);

        return ordered.ThenByDescending(o => o.Price).FirstOrDefault();
    }

    public static decimal PriceOf(IEnumerable<OptionPriceInfo> options, bool isLegacyPurchase = false)
        => Pick(options, isLegacyPurchase)?.Price ?? 0;

    public static bool IsLegacyPurchase(ItemStatus status, Guid? purchasedOptionId)
        => status == ItemStatus.Purchased && purchasedOptionId is null;
}
