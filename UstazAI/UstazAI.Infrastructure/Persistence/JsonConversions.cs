using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace UstazAI.Infrastructure.Persistence;

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
