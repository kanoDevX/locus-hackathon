using FluentAssertions;
using UstazAI.Domain.Services;

namespace UstazAI.Tests;

public class DecisionLedgerTests
{
    [Fact]
    public void ComputeHash_IsDeterministicForSameInputs()
    {
        var hash1 = DecisionLedger.ComputeHash(DecisionLedger.GenesisHash, "{\"a\":1}");
        var hash2 = DecisionLedger.ComputeHash(DecisionLedger.GenesisHash, "{\"a\":1}");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHash_ChangesWhenPayloadChanges()
    {
        var hash1 = DecisionLedger.ComputeHash(DecisionLedger.GenesisHash, "{\"a\":1}");
        var hash2 = DecisionLedger.ComputeHash(DecisionLedger.GenesisHash, "{\"a\":2}");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeHash_ChainBreaksIfAnEarlierPayloadIsTampered()
    {
        var hash1 = DecisionLedger.ComputeHash(DecisionLedger.GenesisHash, "{\"step\":1}");
        var hash2 = DecisionLedger.ComputeHash(hash1, "{\"step\":2}");
        var hash3 = DecisionLedger.ComputeHash(hash2, "{\"step\":3}");

        var tamperedHash1 = DecisionLedger.ComputeHash(DecisionLedger.GenesisHash, "{\"step\":1,\"tampered\":true}");
        var recomputedHash2 = DecisionLedger.ComputeHash(tamperedHash1, "{\"step\":2}");
        var recomputedHash3 = DecisionLedger.ComputeHash(recomputedHash2, "{\"step\":3}");

        recomputedHash3.Should().NotBe(hash3);
    }
}
