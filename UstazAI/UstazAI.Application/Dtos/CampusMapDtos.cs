using UstazAI.Domain.Enums;

namespace UstazAI.Application.Dtos;

public sealed record GeoCoordinatesDto(double Latitude, double Longitude);

public sealed record EnvironmentProfileDto(
    decimal CostOfLivingIndexUsdPerMonth, int SafetyIndex, string ClimateSummary,
    PublicTransitQuality PublicTransitQuality, decimal? InternationalStudentPercent, DataProvenanceDto Provenance);

public sealed record MapLinkDto(string Provider, string EmbedUrl, string AttributionText);

public sealed record CampusMapDto(
    int ProgramId, string ProgramName, string UniversityName, string City, string Country,
    GeoCoordinatesDto? Coordinates, EnvironmentProfileDto? Environment, MapLinkDto? MapLink);
