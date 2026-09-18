namespace UstazAI.Infrastructure.Ai;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = "";

    public string[] ExtraApiKeys { get; set; } = [];
    public string FlashModel { get; set; } = "gemini-3.5-flash";
    public string ProModel { get; set; } = "gemini-3.5-flash";
    public string ResearchModel { get; set; } = "gemini-flash-latest";
    public string[] FallbackModels { get; set; } = ["gemini-flash-latest", "gemini-flash-lite-latest", "gemini-2.5-flash", "gemini-2.5-flash-lite"];
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public int TimeoutSeconds { get; set; } = 30;
}
