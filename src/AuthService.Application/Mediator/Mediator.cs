namespace AuthService.Application.Mediator
{
    public class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;

        public Mediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            // Get the concrete handler type
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));

            // Resolve the handler from DI container
            var handler = _serviceProvider.GetService(handlerType)
                          ?? throw new InvalidOperationException($"Handler for {request.GetType().Name} not registered");

            // Use reflection to call Handle
            var methodInfo = handlerType.GetMethod("HandleAsync")
                             ?? throw new InvalidOperationException("HandleAsync method not found");

            var task = (Task<TResponse>)methodInfo.Invoke(handler, new object[] { request, cancellationToken })!;
            return await task;
        }
    }
}
