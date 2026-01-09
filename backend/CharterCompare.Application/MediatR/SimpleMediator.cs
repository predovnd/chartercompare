using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CharterCompare.Application.MediatR;

public class SimpleMediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public SimpleMediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        // Find the handler interface type
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);

        // Get the handler from DI
        var handler = _serviceProvider.GetRequiredService(handlerType);

        // Invoke the Handle method
        var handleMethod = handlerType.GetMethod("Handle") 
            ?? throw new InvalidOperationException($"Handler for {requestType.Name} does not implement Handle method.");

        var result = handleMethod.Invoke(handler, new object[] { request, cancellationToken });

        if (result is Task<TResponse> task)
        {
            return await task;
        }

        if (result is TResponse response)
        {
            return response;
        }

        throw new InvalidOperationException($"Handler for {requestType.Name} returned unexpected result type.");
    }
}
