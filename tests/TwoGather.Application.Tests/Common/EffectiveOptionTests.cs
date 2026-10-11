using TwoGather.Application.Common.Pricing;

namespace TwoGather.Application.Tests.Common;

public class EffectiveOptionTests
{
    private static OptionPriceInfo Option(string title, decimal? price, bool isFinal = false, bool isSelected = false)
        => new(title, price, "TRY", isFinal, isSelected);

    [Fact]
    public void Pick_NoDecision_ReturnsMostExpensiveAlternative()
    {
        var options = new[] { Option("A", 1000), Option("B", 3000), Option("C", 2000) };

        Assert.Equal("B", EffectiveOption.Pick(options)?.Title);
    }

    [Fact]
    public void Pick_SelectedBeatsMoreExpensiveAlternative()
    {
        var options = new[] { Option("A", 1000, isSelected: true), Option("B", 3000) };

        Assert.Equal("A", EffectiveOption.Pick(options)?.Title);
    }

    [Fact]
    public void Pick_FinalBeatsSelected()
    {
        var options = new[] { Option("A", 1000, isSelected: true), Option("B", 500, isFinal: true) };

        Assert.Equal("B", EffectiveOption.Pick(options)?.Title);
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
}
