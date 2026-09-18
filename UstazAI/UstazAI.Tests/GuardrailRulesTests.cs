using FluentAssertions;
using UstazAI.Domain.Services;

namespace UstazAI.Tests;

public class GuardrailRulesTests
{
    [Theory]
    [InlineData("This university guarantees your admission.")]
    [InlineData("You will definitely be admitted next year.")]
    [InlineData("We promise assured admission for strong students.")]
    [InlineData("There is a 100% chance you get in.")]
    public void ContainsForbiddenLanguage_DetectsGuaranteeAndCertaintyPhrasing(string text)
    {
        GuardrailRules.ContainsForbiddenLanguage(text).Should().BeTrue();
    }

    [Fact]
    public void ContainsForbiddenLanguage_AllowsProperlyHedgedText()
    {
        var text = "Based on available data, you appear to have a strong chance, though nothing is certain.";

        GuardrailRules.ContainsForbiddenLanguage(text).Should().BeFalse();
    }

    [Fact]
    public void Sanitize_RewritesGuaranteeLanguageInsteadOfLeavingItUnchanged()
    {
        var text = "This program guarantees admission for top students.";

        var sanitized = GuardrailRules.Sanitize(text);

        sanitized.Should().NotBe(text);
        GuardrailRules.ContainsForbiddenLanguage(sanitized).Should().BeFalse();
    }

    [Fact]
    public void Sanitize_LeavesAlreadySafeTextUnchanged()
    {
        var text = "Based on available data, you appear to have a strong chance.";

        GuardrailRules.Sanitize(text).Should().Be(text);
    }
}
