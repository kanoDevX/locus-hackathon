using System.Collections;
using MediatR;
using UstazAI.Application.Common.Interfaces;

namespace UstazAI.Application.Common.Behaviors;

/// <summary>
/// Last-resort safety net (§5.3): after a response is produced, any AI-derived text it carries
/// is re-scanned for forbidden guarantee/certainty language and rewritten if necessary — even
/// though the Gemini system instructions and the fallback deterministic path are already
/// designed never to produce it.
///
/// Also recurses one level into a bare enumerable response (e.g. `IRequest&lt;List&lt;
/// RecommendationDto&gt;&gt;`) and sanitizes each element that implements the interface — a
/// response whose OWN type is `List&lt;T&gt;` can never implement `ISanitizableAiResponse`
/// itself (you cannot add an interface to a BCL collection type), so without this, any handler
/// returning a bare list of AI-carrying DTOs directly (as opposed to a wrapping result record)
/// would silently bypass this net entirely — exactly what happened before this recursion existed:
/// `GetLatestRecommendationsQuery : IRequest&lt;List&lt;RecommendationDto&gt;&gt;` shipped with
/// no sanitization path at all.
/// </summary>
public sealed class GuardrailBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);
        Sanitize(response);
        return response;
    }

    private static void Sanitize(object? response)
    {
        switch (response)
        {
            case ISanitizableAiResponse sanitizable:
                sanitizable.SanitizeAiText();
                break;
            case IEnumerable enumerable and not string:
                foreach (var item in enumerable) Sanitize(item);
                break;
        }
    }
}
