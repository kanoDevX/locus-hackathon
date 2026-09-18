using UstazAI.Domain.ValueObjects;

namespace UstazAI.Application.Common.Interfaces;

public sealed record MapLinkResult(string Provider, string EmbedUrl, string AttributionText);

public interface IMapProvider
{
    MapLinkResult GetMapLink(GeoCoordinates coordinates, string city, string country);
}
