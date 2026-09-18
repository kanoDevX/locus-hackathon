using UstazAI.Domain.ValueObjects;

namespace UstazAI.Application.Common.Interfaces;

public sealed record MapLinkResult(string Provider, string EmbedUrl, string AttributionText);

/// <summary>
/// Provider-agnostic port for turning a campus's coordinates into a clickable map (§14). 2GIS is
/// preferred for Kazakhstan (the product's committed persona's home market, with better local POI
/// coverage); OpenStreetMap/MapLibre is the fallback everywhere else. Kept as a swappable port —
/// same shape as IAiReasoningService — rather than hard-coding a URL template inline, so a real
/// keyed 2GIS API integration (geocoding, richer POI data) can replace the deterministic
/// implementation later without touching any Application handler.
/// </summary>
public interface IMapProvider
{
    MapLinkResult GetMapLink(GeoCoordinates coordinates, string city, string country);
}
