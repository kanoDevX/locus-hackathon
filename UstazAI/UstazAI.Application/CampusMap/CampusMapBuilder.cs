using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;
using UstazAI.Domain.Entities;

namespace UstazAI.Application.CampusMap;

public static class CampusMapBuilder
{
    public static CampusMapDto Build(ProgramOffering program, IMapProvider mapProvider)
    {
        var uni = program.University;

        var coordinates = uni.Coordinates is { } c ? new GeoCoordinatesDto(c.Latitude, c.Longitude) : null;

        var environment = uni.Environment is { } e
            ? new EnvironmentProfileDto(
                e.CostOfLivingIndexUsdPerMonth, e.SafetyIndex, e.ClimateSummary,
                e.PublicTransitQuality, e.InternationalStudentPercent, e.Provenance.ToDto())
            : null;

        var mapLink = uni.Coordinates is { } coord
            ? MapLinkFrom(mapProvider.GetMapLink(coord, uni.City, uni.Country))
            : null;

        return new CampusMapDto(program.Id, program.Name, uni.Name, uni.City, uni.Country, coordinates, environment, mapLink);
    }

    private static MapLinkDto MapLinkFrom(MapLinkResult r) => new(r.Provider, r.EmbedUrl, r.AttributionText);
}
