using TwoGather.Application.Common.Pricing;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Tests.Common;

public class EffectiveOptionTests
{
    private static OptionPriceInfo Option(string title, decimal? price, bool isFinal = false, bool isSelected = false, bool isPurchased = false)
        => new(Guid.NewGuid(), title, price, "TRY", isFinal, isSelected, isPurchased);

    [Fact]
    public void Pick_NoDecision_ReturnsMostExpensiveAlternative()
    {
        var options = new[] { Option("A", 1000), Option("B", 3000), Option("C", 2000) };

        Assert.Equal("B", EffectiveOption.Pick(options)?.Title);
    }

    [Fact]
    public void Pick_SelectedDoesNotLowerTheWorstCase()
    {
        var options = new[] { Option("A", 1000, isSelected: true), Option("B", 3000) };

        Assert.Equal("B", EffectiveOption.Pick(options)?.Title);
    }

    [Fact]
    public void Pick_FinalBeatsMoreExpensiveAlternative()
    {
        var options = new[] { Option("A", 3000), Option("B", 500, isFinal: true) };

        Assert.Equal("B", EffectiveOption.Pick(options)?.Title);
    }

    [Fact]
    public void Pick_PurchasedBeatsFinal()
    {
        var options = new[] { Option("A", 3000, isFinal: true), Option("B", 800, isPurchased: true) };

        Assert.Equal("B", EffectiveOption.Pick(options)?.Title);
    }

    [Fact]
    public void Pick_LegacyPurchase_KeepsSelectedRule()
    {
        var options = new[] { Option("A", 1000, isSelected: true), Option("B", 3000) };

        Assert.Equal("A", EffectiveOption.Pick(options, isLegacyPurchase: true)?.Title);
    }

    [Fact]
    public void Pick_IgnoresOptionsWithoutPrice()
    {
        var options = new[] { Option("A", null, isFinal: true), Option("B", 750) };

        Assert.Equal("B", EffectiveOption.Pick(options)?.Title);
    }

    [Fact]
    public void PriceOf_NoPricedOptions_ReturnsZero()
    {
        Assert.Equal(0, EffectiveOption.PriceOf(new[] { Option("A", null) }));
        Assert.Equal(0, EffectiveOption.PriceOf(Array.Empty<OptionPriceInfo>()));
    }

    [Theory]
    [InlineData(ItemStatus.Purchased, false, true)]
    [InlineData(ItemStatus.Purchased, true, false)]
    [InlineData(ItemStatus.Pending, false, false)]
    public void IsLegacyPurchase_OnlyForPurchasesWithoutRecordedOption(ItemStatus status, bool hasOption, bool expected)
        => Assert.Equal(expected, EffectiveOption.IsLegacyPurchase(status, hasOption ? Guid.NewGuid() : null));
}
