using FluentAssertions;
using UstazAI.Domain.ValueObjects;
using UstazAI.Infrastructure.Maps;

namespace UstazAI.Tests;

public class DeterministicMapProviderTests
{
    private readonly DeterministicMapProvider _provider = new();

    [Fact]
    public void Kazakhstan_UsesTwoGis()
    {
        var link = _provider.GetMapLink(new GeoCoordinates { Latitude = 51.09, Longitude = 71.41 }, "Astana", "Kazakhstan");

        link.Provider.Should().Be("2GIS");
        link.EmbedUrl.Should().StartWith("https://2gis.kz/");
        link.EmbedUrl.Should().Contain("71.41").And.Contain("51.09");
    }

    [Fact]
    public void NonKazakhstan_FallsBackToOpenStreetMap()
    {
        var link = _provider.GetMapLink(new GeoCoordinates { Latitude = 48.14, Longitude = 11.56 }, "Munich", "Germany");

        link.Provider.Should().Be("OpenStreetMap");
        link.EmbedUrl.Should().StartWith("https://www.openstreetmap.org/");
        link.AttributionText.Should().Contain("OpenStreetMap");
    }
}
