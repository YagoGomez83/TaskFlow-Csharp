using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace TaskManagement.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior que loguea información sobre requests y responses de MediatR.
/// </summary>
/// <typeparam name="TRequest">Tipo del request (Command o Query).</typeparam>
/// <typeparam name="TResponse">Tipo de respuesta.</typeparam>
/// <remarks>
/// EXPLICACIÓN DE LOGGING:
///
/// Logging es crucial para:
/// - Debugging en producción
/// - Monitoreo de performance
/// - Auditoría de acciones de usuarios
/// - Detección de errores
/// - Análisis de uso
///
/// NIVELES DE LOG:
///
/// 1. TRACE (muy detallado):
///    - Información extremadamente detallada
///    - Ej: "Entering method X with parameter Y"
///    - Solo para debugging profundo
///    - NO usar en producción (mucho volumen)
///
/// 2. DEBUG:
///    - Información de debugging
///    - Ej: "Query returned 10 results"
///    - Útil en desarrollo
///    - Desactivar en producción (performance)
///
/// 3. INFORMATION:
///    - Flujo normal de la aplicación
///    - Ej: "User 123 created task 456"
///    - Eventos importantes pero normales
///    - Activar en producción
///
/// 4. WARNING:
///    - Situaciones anormales que NO son errores
///    - Ej: "Task query took 2 seconds (slow)"
///    - La aplicación funciona pero algo no es óptimo
///    - Investigar periódicamente
///
/// 5. ERROR:
///    - Errores que impiden completar operación
///    - Ej: "Failed to create task: Database connection failed"
///    - Requiere atención
///    - Alertas automáticas
///
/// 6. CRITICAL:
///    - Errores que afectan toda la aplicación
///    - Ej: "Database is down"
///    - Requiere atención INMEDIATA
///    - Alertas críticas (SMS, llamadas)
///
/// ESTE BEHAVIOR:
///
/// - Loguea INFORMACIÓN al inicio de cada request
/// - Loguea INFORMACIÓN al final de request exitoso
/// - Loguea WARNING si el request tarda mucho (> 500ms)
/// - NO loguea errores (eso lo hace un ExceptionBehavior o middleware)
///
/// EJEMPLO DE LOGS GENERADOS:
///
/// [Information] Handling CreateTaskCommand
/// [Information] Handled CreateTaskCommand in 45ms
///
/// [Information] Handling GetTasksQuery
/// [Warning] GetTasksQuery took 1250ms (> 500ms threshold)
///
/// [Information] Handling UpdateTaskCommand
/// [Information] Handled UpdateTaskCommand in 30ms
///
/// STRUCTURED LOGGING:
///
/// En lugar de logs como strings:
/// ❌ _logger.LogInformation($"User {userId} created task {taskId}");
///
/// Usar structured logging con placeholders:
/// ✅ _logger.LogInformation("User {UserId} created task {TaskId}", userId, taskId);
///
/// Ventajas:
/// - Logs son parseables (JSON output)
/// - Fácil buscar por campo específico
/// - Compatible con herramientas de análisis (ELK, Splunk, Application Insights)
///
/// Ejemplo de output JSON:
/// {
///   "timestamp": "2024-01-15T10:30:00Z",
///   "level": "Information",
///   "message": "User {UserId} created task {TaskId}",
///   "userId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
///   "taskId": "x9y8z7w6-v5u4-3210-tsr-qponmlkjihgf"
/// }
///
/// SERILOG:
///
/// Serilog es la biblioteca de logging más popular para .NET.
/// Ventajas sobre Microsoft.Extensions.Logging:
/// - Structured logging nativo
/// - Múltiples sinks (console, file, database, cloud)
/// - Filtros avanzados
/// - Performance superior
/// - Enrichers (agregar contexto automáticamente)
///
/// Instalación:
/// dotnet add package Serilog.AspNetCore
/// dotnet add package Serilog.Sinks.Console
/// dotnet add package Serilog.Sinks.File
/// dotnet add package Serilog.Sinks.Seq  // Para logs centralizados
///
/// Configuración:
/// Log.Logger = new LoggerConfiguration()
///     .MinimumLevel.Information()
///     .Enrich.FromLogContext()
///     .Enrich.WithMachineName()
///     .Enrich.WithThreadId()
///     .WriteTo.Console()
///     .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
///     .CreateLogger();
///
/// Para este proyecto, usar Serilog (se configurará en API Layer).
///
/// LOG CONTEXT:
///
/// Agregar contexto a logs automáticamente:
///
/// using (LogContext.PushProperty("UserId", userId))
/// {
///     _logger.LogInformation("Creating task");
///     // Log incluye UserId automáticamente
/// }
///
/// Output:
/// {
///   "message": "Creating task",
///   "userId": "a1b2c3d4-...",  // ← Agregado automáticamente
/// }
///
/// PERFORMANCE LOGGING:
///
/// Este behavior mide tiempo de ejecución con Stopwatch:
///
/// var stopwatch = Stopwatch.StartNew();
/// var response = await next();
/// stopwatch.Stop();
///
/// Si tarda > 500ms, loguea warning.
///
/// THRESHOLDS:
/// - < 100ms: Rápido (no loguear)
/// - 100-500ms: Normal (loguear información)
/// - 500-2000ms: Lento (loguear warning)
/// - > 2000ms: Muy lento (loguear error)
///
/// Para este proyecto: warning si > 500ms.
///
/// SENSITIVE DATA:
///
/// NUNCA loguear datos sensibles:
/// ❌ Contraseñas
/// ❌ Tokens
/// ❌ Números de tarjeta
/// ❌ SSN, documentos de identidad
/// ❌ Información médica
/// ❌ Datos bancarios
///
/// Si necesitas loguear request completo, sanitizar:
///
/// public class LoginCommand
/// {
///     public string Email { get; set; }
///     [SensitiveData]  // Atributo custom
///     public string Password { get; set; }
/// }
///
/// En el behavior:
/// var sanitized = SanitizeRequest(request);
/// _logger.LogInformation("Handling {RequestType}: {Request}", requestName, sanitized);
///
/// Output:
/// {
///   "email": "user@example.com",
///   "password": "***REDACTED***"  // ← Sanitizado
/// }
///
/// Para simplicidad, este proyecto NO loguea request/response completos,
/// solo el nombre del request y tiempo de ejecución.
///
/// CORRELATION IDs:
///
/// En sistemas distribuidos, usar correlation ID para trackear requests:
///
/// Client request → API → Database
///                      → Cache
///                      → External API
///
/// Todos los logs del mismo request tienen mismo correlation ID.
///
/// Ejemplo:
/// [Info] [CorrelationId: abc123] Handling CreateTaskCommand
/// [Info] [CorrelationId: abc123] Querying database
/// [Info] [CorrelationId: abc123] Task created successfully
///
/// Puedes filtrar logs por correlation ID para ver todo el flujo.
///
/// Implementación:
/// - Generar Guid en middleware al inicio del request
/// - Agregar a HttpContext.Items
/// - Enricher de Serilog lo agrega automáticamente a logs
///
/// builder.Services.AddHttpContextAccessor();
/// app.Use(async (context, next) =>
/// {
///     var correlationId = Guid.NewGuid().ToString();
///     context.Items["CorrelationId"] = correlationId;
///     using (LogContext.PushProperty("CorrelationId", correlationId))
///     {
///         await next();
///     }
/// });
///
/// EJEMPLOS DE USO:
///
/// Este behavior se ejecuta automáticamente para TODOS los requests MediatR.
///
/// Ejemplo 1 - Request rápido:
/// await _mediator.Send(new CreateTaskCommand { Title = "New Task" });
///
/// Logs:
/// [Information] Handling CreateTaskCommand
/// [Information] Handled CreateTaskCommand in 45ms
///
/// Ejemplo 2 - Request lento:
/// await _mediator.Send(new GetAllTasksQuery());
///
/// Logs:
/// [Information] Handling GetAllTasksQuery
/// [Warning] GetAllTasksQuery took 1250ms (> 500ms threshold)
///
/// TESTING:
///
/// [Fact]
/// public async Task Handle_LogsRequestAndResponse()
/// {
///     // Arrange
///     var mockLogger = new Mock<ILogger<LoggingBehavior<CreateTaskCommand, Result>>>();
///     var behavior = new LoggingBehavior<CreateTaskCommand, Result>(mockLogger.Object);
///     var request = new CreateTaskCommand { Title = "Test" };
///
///     // Act
///     await behavior.Handle(request, () => Task.FromResult(Result.Success()), CancellationToken.None);
///
///     // Assert
///     mockLogger.Verify(
///         x => x.Log(
///             LogLevel.Information,
///             It.IsAny<EventId>(),
///             It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Handling CreateTaskCommand")),
///             null,
///             It.IsAny<Func<It.IsAnyType, Exception, string>>()
///         ),
///         Times.Once
///     );
/// }
///
/// REGISTRO EN DI:
///
/// services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
///
/// ORDEN CON OTROS BEHAVIORS:
///
/// Logging debe ser PRIMERO para capturar todo:
///
/// services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));      // 1º
/// services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));  // 2º
///
/// Flujo:
/// Request → LoggingBehavior (log inicio) → ValidationBehavior → Handler → ValidationBehavior → LoggingBehavior (log fin) → Response
///
/// Si ValidationBehavior falla, LoggingBehavior aún loguea.
///
/// ALTERNATIVAS:
///
/// 1. Loguear en cada Handler manualmente:
///    ❌ Repetitivo, inconsistente
///
/// 2. Loguear en Middleware:
///    ✅ Bueno para HTTP requests
///    ❌ No tiene contexto de MediatR (request type, etc.)
///
/// 3. LoggingBehavior (este):
///    ✅ Automático para todos los MediatR requests
///    ✅ Contexto completo de request type
///    ✅ Mide performance específica de handler
///
/// Combinar #2 y #3 es ideal:
/// - Middleware loguea HTTP (método, path, status)
/// - LoggingBehavior loguea MediatR (request type, tiempo)
///
/// LOG AGGREGATION:
///
/// En producción, enviar logs a servicio centralizado:
///
/// - Seq: Self-hosted, gratis, excelente UI
/// - ELK Stack: Elasticsearch + Logstash + Kibana
/// - Splunk: Comercial, muy potente
/// - Application Insights: Azure, integración nativa
/// - Datadog: Multi-cloud, APM completo
///
/// Configurar sink de Serilog:
/// .WriteTo.Seq("http://localhost:5341")
/// .WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces)
///
/// GDPR Y PRIVACIDAD:
///
/// En Europa (GDPR), cuidado con loguear información personal:
/// - Email, nombre, dirección pueden ser PII
/// - Loguear IDs en lugar de datos personales
/// - Implementar retención de logs (ej: 90 días)
/// - Permitir eliminación de logs de usuario (right to be forgotten)
///
/// ✅ _logger.LogInformation("User {UserId} created task", userId);
/// ❌ _logger.LogInformation("User {Email} created task", email);  // PII
///
/// RETENCIÓN:
///
/// Configurar cuánto tiempo mantener logs:
/// - Development: 7 días
/// - Staging: 30 días
/// - Production: 90 días (o según compliance)
///
/// .WriteTo.File("logs/app-.txt",
///     rollingInterval: RollingInterval.Day,
///     retainedFileCountLimit: 90)  // Mantener solo últimos 90 archivos
///
/// MONITORING Y ALERTAS:
///
/// Configurar alertas basadas en logs:
/// - Error rate > 1% → Alert
/// - Request duration > 2s → Warning
/// - 5xx responses → Alert
/// - Specific error pattern → Alert
///
/// Herramientas:
/// - Application Insights: Alertas automáticas
/// - Seq: Alertas por query
/// - ELK + ElastAlert: Alertas customizables
/// - PagerDuty: Escalación de incidentes
/// </remarks>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Constructor que inyecta el logger.
    /// </summary>
    /// <param name="logger">Logger genérico para este behavior.</param>
    /// <remarks>
    /// ILogger&lt;LoggingBehavior&lt;TRequest, TResponse&gt;&gt; crea un logger específico
    /// para cada combinación de TRequest y TResponse.
    ///
    /// Ejemplo:
    /// - LoggingBehavior&lt;CreateTaskCommand, Result&gt;
    /// - LoggingBehavior&lt;GetTasksQuery, Result&lt;PaginatedList&lt;TaskDto&gt;&gt;&gt;
    ///
    /// Esto permite filtrar logs por request type en configuración.
    ///
    /// ALTERNATIVA - ILogger&lt;LoggingBehavior&gt; (no genérico):
    /// Todos los logs irían bajo misma categoría, más difícil filtrar.
    /// </remarks>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta logging antes y después del handler.
    /// </summary>
    /// <param name="request">Request a procesar.</param>
    /// <param name="next">Siguiente behavior o handler.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Response del handler.</returns>
    /// <remarks>
    /// Flujo:
    /// 1. Log inicio de request
    /// 2. Iniciar timer
    /// 3. Ejecutar handler
    /// 4. Detener timer
    /// 5. Log fin de request con tiempo
    /// 6. Si tardó mucho (> 500ms), log warning
    /// 7. Retornar response
    ///
    /// IMPORTANTE: Este behavior NO maneja excepciones.
    /// Si handler lanza excepción, el behavior la propaga.
    /// Manejo de excepciones debe hacerse en otro behavior o middleware.
    /// </remarks>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // 1. Obtener nombre del request type (ej: "CreateTaskCommand")
        var requestName = typeof(TRequest).Name;

        // 2. Log inicio de procesamiento
        // Structured logging: {RequestName} es un placeholder
        _logger.LogInformation("Handling {RequestName}", requestName);

        // 3. Iniciar medición de tiempo
        var stopwatch = Stopwatch.StartNew();

        // 4. Ejecutar siguiente behavior o handler
        var response = await next();

        // 5. Detener timer
        stopwatch.Stop();

        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

        // 6. Log fin de procesamiento con tiempo
        _logger.LogInformation(
            "Handled {RequestName} in {ElapsedMilliseconds}ms",
            requestName,
            elapsedMilliseconds
        );

        // 7. Si tardó más de 500ms, loguear warning
        // Threshold configurable según necesidades
        if (elapsedMilliseconds > 500)
        {
            _logger.LogWarning(
                "{RequestName} took {ElapsedMilliseconds}ms (> 500ms threshold)",
                requestName,
                elapsedMilliseconds
            );
        }

        // 8. Retornar response
        return response;
    }
}
