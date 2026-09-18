using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace UstazAI.Infrastructure.Persistence;

/// <summary>
/// Stores small value objects / primitive lists (interests, exam scores, provenance,
/// uncertainty estimates, ...) as JSON columns instead of separate owned tables — the
/// hackathon-pragmatic choice that keeps the schema flat and readable while still using SQL
/// Server 2022's native JSON column support.
/// </summary>
public static class JsonConversions
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> builder) where T : class, new()
    {
        var converter = new ValueConverter<T, string>(
            v => JsonSerializer.Serialize(v, Options),
            v => JsonSerializer.Deserialize<T>(v, Options) ?? new T());

        var comparer = new ValueComparer<T>(
            (a, b) => JsonSerializer.Serialize(a, Options) == JsonSerializer.Serialize(b, Options),
            v => JsonSerializer.Serialize(v, Options).GetHashCode(),
            v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, Options), Options)!);

        builder.HasConversion(converter);
        builder.Metadata.SetValueComparer(comparer);
        builder.HasColumnType("nvarchar(max)");
        return builder;
    }

    /// <summary>Same idea as <see cref="HasJsonConversion{T}"/>, but for a genuinely-nullable
    /// value object (e.g. StudentProfile.CollegeBackground, which is absent for non-college
    /// applicants). The non-nullable converter above round-trips a null CLR value through
    /// `JsonSerializer.Serialize(null)` — the literal string "null" — and coerces it back to a
    /// *new*, empty instance on read, silently turning "no college background" into "an empty
    /// one." This variant short-circuits null on both sides instead.</summary>
    public static PropertyBuilder<T?> HasNullableJsonConversion<T>(this PropertyBuilder<T?> builder) where T : class, new()
    {
        var converter = new ValueConverter<T?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, Options),
            v => v == null ? null : JsonSerializer.Deserialize<T>(v, Options));

        var comparer = new ValueComparer<T?>(
            (a, b) => (a == null && b == null) ||
                      (a != null && b != null && JsonSerializer.Serialize(a, Options) == JsonSerializer.Serialize(b, Options)),
            v => v == null ? 0 : JsonSerializer.Serialize(v, Options).GetHashCode(),
            v => v == null ? null : JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, Options), Options));

        builder.HasConversion(converter);
        builder.Metadata.SetValueComparer(comparer);
        builder.HasColumnType("nvarchar(max)");
        return builder;
    }
}
