using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TaskManagement.Application.Common.Behaviors;

namespace TaskManagement.Application;

/// <summary>
/// Extension methods para registrar servicios de Application Layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra todos los servicios de Application Layer en el contenedor de DI.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>Colección de servicios para encadenar.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Registrar MediatR (Handlers, Behaviors, Pipeline)
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Registrar AutoMapper
        services.AddAutoMapper(assembly);

        // Registrar FluentValidation Validators
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
