using System.Globalization;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Infrastructure.Maps;

/// <summary>
/// Key-free IMapProvider implementation (§14): both 2GIS and OpenStreetMap support plain
/// coordinate deep links with no API key, so this stays functional without a paid/registered
/// mapping subscription — a deliberate hackathon-pragmatic choice, not a stand-in for a real
/// integration. 2GIS is used for Kazakhstan (better local POI/transit coverage for the product's
/// committed persona's home market); OpenStreetMap is the universal fallback everywhere else,
/// matching the spec's "2GIS preferred, MapLibre+OSM fallback" instruction.
/// </summary>
public sealed class DeterministicMapProvider : IMapProvider
{
    public MapLinkResult GetMapLink(GeoCoordinates coordinates, string city, string country)
    {
        var lat = coordinates.Latitude.ToString(CultureInfo.InvariantCulture);
        var lon = coordinates.Longitude.ToString(CultureInfo.InvariantCulture);

        if (string.Equals(country, "Kazakhstan", StringComparison.OrdinalIgnoreCase))
        {
            return new MapLinkResult(
                "2GIS",
                $"https://2gis.kz/geo/{lon},{lat}",
                "Map data © 2GIS");
        }

        return new MapLinkResult(
            "OpenStreetMap",
            $"https://www.openstreetmap.org/?mlat={lat}&mlon={lon}#map=14/{lat}/{lon}",
            "© OpenStreetMap contributors");
    }
}
