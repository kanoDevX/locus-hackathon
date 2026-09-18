namespace UstazAI.Application.Common.Interfaces;

/// <summary>
/// Implemented by any MediatR response that carries free-text produced (even partly) by Gemini.
/// GuardrailBehavior calls <see cref="SanitizeAiText"/> on every response before it leaves the
/// pipeline, so forbidden guarantee/certainty language is rewritten as a last-resort safety net
/// even if a prompt regression lets it through (§5.3).
/// </summary>
public interface ISanitizableAiResponse
{
    void SanitizeAiText();
}
