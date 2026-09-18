using System.Collections;
using MediatR;
using UstazAI.Application.Common.Interfaces;

namespace UstazAI.Application.Common.Behaviors;

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
