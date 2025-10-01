using AuthService.Application.Mediator;
using Microsoft.Extensions.DependencyInjection;

public static class MediatorExtensions
{
    public static IServiceCollection AddCustomMediator(this IServiceCollection services)
    {
        // Register Mediator
        services.AddScoped<IMediator, Mediator>();

        // Automatically register all handlers in this assembly
        var handlerType = typeof(IRequestHandler<,>);
        var assemblies = new[] { typeof(MediatorExtensions).Assembly };

        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            var interfaces = type.GetInterfaces()
                                 .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType);

            foreach (var @interface in interfaces)
            {
                services.AddScoped(@interface, type);
            }
        }

        return services;
    }
}
