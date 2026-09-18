using FluentAssertions;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.Services;

namespace UstazAI.Tests;

public class ProfileDiffCalculatorTests
{
    private static StudentProfile MakeProfile() => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Test Student",
        Grade = 11,
        Age = 17,
        Gpa = 3.5m,
        Interests = ["Computer Science"],
        TargetCountries = ["Kazakhstan"],
        BudgetBand = BudgetBand.Medium,
        TimelineMonthsToApplication = 10,
        Constraints = []
    };

    [Fact]
    public void DetectChangedFields_WhenNothingChanges_ReturnsEmpty()
    {
        var before = MakeProfile();
        var after = MakeProfile();

        ProfileDiffCalculator.DetectChangedFields(before, after).Should().BeEmpty();
    }

    [Fact]
    public void DetectChangedFields_WhenBudgetBandChanges_ReportsOnlyThatField()
    {
        var before = MakeProfile();
        var after = MakeProfile();
        after.BudgetBand = BudgetBand.Low;

        var changed = ProfileDiffCalculator.DetectChangedFields(before, after);

        changed.Should().ContainSingle().Which.Should().Be(nameof(StudentProfile.BudgetBand));
    }

    [Fact]
    public void DetectChangedFields_WhenInterestsReorderedButSameSet_ReportsNoChange()
    {
        var before = MakeProfile();
        before.Interests = ["A", "B"];
        var after = MakeProfile();
        after.Interests = ["B", "A"];

        ProfileDiffCalculator.DetectChangedFields(before, after).Should().BeEmpty();
    }

    [Fact]
    public void DescribeChange_ForBudgetBand_MentionsBothOldAndNewValues()
    {
        var before = MakeProfile();
        var after = MakeProfile();
        after.BudgetBand = BudgetBand.High;

        var description = ProfileDiffCalculator.DescribeChange(nameof(StudentProfile.BudgetBand), before, after);

        description.Should().Contain("Medium").And.Contain("High");
    }
}
