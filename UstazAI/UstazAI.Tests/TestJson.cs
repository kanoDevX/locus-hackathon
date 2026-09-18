using System.Text.Json;
using System.Text.Json.Serialization;

namespace UstazAI.Tests;

/// <summary>Matches the server's own JSON options (see Program.cs's ConfigureHttpJsonOptions):
/// enums as strings. HttpClient's PostAsJsonAsync/ReadFromJsonAsync fall back to
/// JsonSerializerOptions.Default when no options are passed, which does NOT know about that
/// converter — every call in the integration tests passes this explicitly instead.</summary>
internal static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
