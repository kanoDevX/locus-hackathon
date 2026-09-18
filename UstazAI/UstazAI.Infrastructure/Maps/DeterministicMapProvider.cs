using System.Globalization;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Infrastructure.Maps;

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
