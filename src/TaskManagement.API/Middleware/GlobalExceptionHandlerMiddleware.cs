using System.Net;
using System.Text.Json;

namespace TaskManagement.API.Middleware;

/// <summary>
/// Middleware para manejo global de excepciones.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE MIDDLEWARE:
///
/// Middleware es un componente que forma parte del pipeline de ASP.NET Core.
/// Cada request pasa por una cadena de middlewares antes de llegar al Controller.
///
/// PIPELINE DE ASP.NET CORE:
///
/// Request → Middleware 1 → Middleware 2 → ... → Controller → Response
///           ↑______________|______________|_____|____________|
///                         Pipeline
///
/// Cada middleware puede:
/// 1. Procesar el request
/// 2. Llamar al siguiente middleware (await next(context))
/// 3. Procesar la response
/// 4. Cortocircuitar el pipeline (no llamar a next)
///
/// ORDEN DE MIDDLEWARES:
///
/// El orden ES CRÍTICO:
///
/// app.UseExceptionHandler();     // 1. Primero (captura todas las excepciones)
/// app.UseHttpsRedirection();     // 2. HTTPS redirect
/// app.UseRouting();              // 3. Routing
/// app.UseCors();                 // 4. CORS
/// app.UseAuthentication();       // 5. Autenticación (valida JWT)
/// app.UseAuthorization();        // 6. Autorización (valida permisos)
/// app.MapControllers();          // 7. Endpoints (Controllers)
///
/// ¿POR QUÉ ESTE ORDEN?
///
/// 1. ExceptionHandler primero para capturar TODAS las excepciones
/// 2. Authentication antes de Authorization (necesitas saber quién eres antes de verificar permisos)
/// 3. CORS antes de Authentication para permitir preflight requests
///
/// MANEJO DE EXCEPCIONES:
///
/// Sin middleware:
/// - Exception no manejada → 500 Internal Server Error
/// - Stack trace expuesto al cliente (riesgo de seguridad)
/// - Formato inconsistente
/// - No logging
///
/// Con middleware:
/// - Exception capturada y loggeada
/// - Response consistente (JSON con error details)
/// - Sin información sensible al cliente
/// - Status codes apropiados
///
/// IMPLEMENTACIÓN:
///
/// public class GlobalExceptionHandlerMiddleware
/// {
///     private readonly RequestDelegate _next;
///     private readonly ILogger _logger;
///
///     public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger logger)
///     {
///         _next = next;          // Siguiente middleware
///         _logger = logger;      // Logger para registrar errores
///     }
///
///     public async Task InvokeAsync(HttpContext context)
///     {
///         try
///         {
///             // Llamar al siguiente middleware/controller
///             await _next(context);
///         }
///         catch (Exception ex)
///         {
///             // Capturar y manejar excepción
///             await HandleExceptionAsync(context, ex);
///         }
///     }
/// }
///
/// REQUEST DELEGATE:
///
/// RequestDelegate es un delegate que representa el siguiente middleware:
/// public delegate Task RequestDelegate(HttpContext context);
///
/// await _next(context) invoca el siguiente middleware en el pipeline.
///
/// HTTPCONTEXT:
///
/// HttpContext contiene toda la información del request/response:
/// - context.Request → HTTP request (headers, body, query, etc.)
/// - context.Response → HTTP response (status code, headers, body)
/// - context.User → Usuario autenticado (ClaimsPrincipal)
/// - context.RequestServices → Service provider (DI)
///
/// LOGGING:
///
/// ILogger registra errores para monitoreo:
///
/// _logger.LogError(exception, "Error message: {ErrorDetails}", details);
///
/// Log levels:
/// - Trace: Muy detallado, solo debugging
/// - Debug: Información de debugging
/// - Information: Eventos normales (request completado)
/// - Warning: Eventos inesperados pero manejables
/// - Error: Errores que causan fallo de request
/// - Critical: Errores que causan fallo de aplicación
///
/// En producción, generalmente:
/// - Minimum level: Information
/// - Errors/Critical van a log aggregator (Serilog, ELK, Application Insights)
///
/// STATUS CODES SEGÚN TIPO DE EXCEPCIÓN:
///
/// Puedes mapear diferentes excepciones a diferentes status codes:
///
/// switch (exception)
/// {
///     case ValidationException:
///         statusCode = 400;    // Bad Request
///         break;
///     case NotFoundException:
///         statusCode = 404;    // Not Found
///         break;
///     case UnauthorizedException:
///         statusCode = 401;    // Unauthorized
///         break;
///     case ForbiddenException:
///         statusCode = 403;    // Forbidden
///         break;
///     default:
///         statusCode = 500;    // Internal Server Error
///         break;
/// }
///
/// Para este proyecto, usamos Result Pattern en lugar de excepciones
/// para errores de negocio, así que la mayoría de excepciones aquí
/// son errores no esperados (bugs, DB timeout, etc.) → 500.
///
/// ERROR RESPONSE FORMAT:
///
/// Respuesta consistente en formato JSON:
///
/// {
///   "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
///   "title": "An error occurred while processing your request",
///   "status": 500,
///   "traceId": "00-abc123...",
///   "errors": {
///     "message": "Error description"
///   }
/// }
///
/// Sigue RFC 7807 (Problem Details for HTTP APIs).
///
/// EN DESARROLLO vs PRODUCCIÓN:
///
/// Desarrollo:
/// - Incluir stack trace completo
/// - Incluir inner exceptions
/// - Logging detallado
///
/// Producción:
/// - NO incluir stack trace (expone código)
/// - Mensaje genérico ("An error occurred")
/// - Solo logging de detalles
///
/// CONTENT-TYPE:
///
/// Importante establecer Content-Type correcto:
/// context.Response.ContentType = "application/json";
///
/// Sin esto, cliente puede no parsear response correctamente.
///
/// TESTING:
///
/// Testing de middleware:
///
/// [Fact]
/// public async Task InvokeAsync_ThrowsException_Returns500()
/// {
///     var context = new DefaultHttpContext();
///     context.Response.Body = new MemoryStream();
///
///     var middleware = new GlobalExceptionHandlerMiddleware(
///         next: (ctx) => throw new Exception("Test error"),
///         logger: mockLogger.Object);
///
///     await middleware.InvokeAsync(context);
///
///     Assert.Equal(500, context.Response.StatusCode);
///
///     context.Response.Body.Seek(0, SeekOrigin.Begin);
///     var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
///     var error = JsonSerializer.Deserialize<ErrorResponse>(body);
///
///     Assert.Equal("An error occurred", error.Title);
/// }
///
/// ALTERNATIVA: UseExceptionHandler:
///
/// ASP.NET Core tiene middleware integrado:
///
/// app.UseExceptionHandler(errorApp =>
/// {
///     errorApp.Run(async context =>
///     {
///         context.Response.StatusCode = 500;
///         context.Response.ContentType = "application/json";
///
///         var exceptionHandlerPathFeature =
///             context.Features.Get<IExceptionHandlerPathFeature>();
///
///         await context.Response.WriteAsJsonAsync(new
///         {
///             error = "An error occurred"
///         });
///     });
/// });
///
/// Custom middleware da más control sobre formato y logging.
///
/// CORRELACIÓN DE REQUESTS:
///
/// TraceId permite correlacionar logs de un mismo request:
///
/// var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
///
/// Todos los logs del request tienen mismo traceId:
/// [12:00:01] [traceId:abc123] Request started GET /api/tasks
/// [12:00:02] [traceId:abc123] Executing handler
/// [12:00:03] [traceId:abc123] Error: Database timeout
///
/// Facilita debugging en producción.
///
/// MEJORES PRÁCTICAS:
///
/// 1. ✅ Siempre loggear excepciones
/// 2. ✅ NO exponer stack trace en producción
/// 3. ✅ Usar formato consistente (Problem Details)
/// 4. ✅ Incluir TraceId para correlación
/// 5. ✅ Retornar status codes apropiados
/// 6. ✅ Registrar middleware al inicio del pipeline
/// 7. ❌ NO usar excepciones para control de flujo
/// 8. ❌ NO retornar información sensible al cliente
///
/// SEGURIDAD:
///
/// Errores pueden exponer información sensible:
/// - ❌ "User 'admin' not found in database 'production_db'"
/// - ✅ "Authentication failed"
///
/// - ❌ "Connection to sql-server.internal.company.com:1433 failed"
/// - ✅ "Database connection failed"
///
/// - ❌ Stack trace con paths: C:\Projects\MyApi\Services\UserService.cs:45
/// - ✅ Generic error sin detalles de implementación
/// </remarks>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Invoca el middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Llamar al siguiente middleware/controller
            await _next(context);
        }
        catch (Exception exception)
        {
            // Capturar cualquier excepción no manejada
            _logger.LogError(
                exception,
                "An unhandled exception occurred. TraceId: {TraceId}",
                context.TraceIdentifier);

            // Manejar la excepción y retornar response apropiada
            await HandleExceptionAsync(context, exception);
        }
    }

    /// <summary>
    /// Maneja la excepción y escribe la response.
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Default: 500 Internal Server Error
        // Puedes mapear diferentes excepciones a diferentes status codes aquí
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        // Crear response según RFC 7807 (Problem Details)
        var response = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "An error occurred while processing your request",
            status = context.Response.StatusCode,
            traceId = context.TraceIdentifier,
            errors = CreateErrorDetails(exception)
        };

        // Serializar y escribir response
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Crea detalles del error según entorno.
    /// </summary>
    private object CreateErrorDetails(Exception exception)
    {
        // En desarrollo, incluir detalles completos
        if (_environment.IsDevelopment())
        {
            return new
            {
                message = exception.Message,
                stackTrace = exception.StackTrace,
                innerException = exception.InnerException?.Message
            };
        }

        // En producción, mensaje genérico
        // NO exponer detalles de implementación
        return new
        {
            message = "An unexpected error occurred. Please try again later."
        };
    }
}

/// <summary>
/// Extension method para registrar el middleware.
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
