namespace UstazAI.Infrastructure.Ai;

/// <summary>Bound from configuration (user-secrets / environment variables in dev, real secret
/// store in prod) — the API key is never committed to source control (see README).</summary>
public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = "";

    /// <summary>Optional additional keys (other Google accounts / projects). Google Search grounding
    /// has its own free quota per project; when one key is exhausted the research call tries the next.</summary>
    public string[] ExtraApiKeys { get; set; } = [];
    // gemini-3.5-flash is the current stable model (May 2026). ProModel is reserved for a
    // heavier-reasoning call this build doesn't currently make (every call goes through
    // FlashModel — see GeminiReasoningService); it's pointed at the same model rather than at
    // gemini-3.5-pro, which was announced but never actually shipped (delayed indefinitely as
    // of August 2026) — pointing it at a nonexistent model id would silently fail every call.
    public string FlashModel { get; set; } = "gemini-3.5-flash";
    public string ProModel { get; set; } = "gemini-3.5-flash";
    // Live web research (Google Search grounding) runs on its own model so it draws on a separate
    // per-model quota: on the free tier gemini-3.5-flash allows only 20 requests/day, which the
    // narration/diagnostics calls exhaust, leaving research (and everything else) on 429.
    public string ResearchModel { get; set; } = "gemini-flash-latest";
    // Tried in order when the primary model answers 429 (per-model quota exhausted).
    public string[] FallbackModels { get; set; } = ["gemini-flash-latest", "gemini-flash-lite-latest", "gemini-2.5-flash", "gemini-2.5-flash-lite"];
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public int TimeoutSeconds { get; set; } = 30;
}
