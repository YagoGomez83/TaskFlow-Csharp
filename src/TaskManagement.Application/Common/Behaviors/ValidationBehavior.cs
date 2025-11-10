using FluentValidation;
using MediatR;
using TaskManagement.Application.Common.Models;

namespace TaskManagement.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior que valida requests usando FluentValidation antes de ejecutar el handler.
/// </summary>
/// <typeparam name="TRequest">Tipo del request (Command o Query).</typeparam>
/// <typeparam name="TResponse">Tipo de respuesta (Result o Result&lt;T&gt;).</typeparam>
/// <remarks>
/// EXPLICACIÓN DE MEDIATR PIPELINE BEHAVIORS:
///
/// MediatR permite interceptar requests antes y después de ejecutar handlers.
/// Esto se llama "pipeline" o "middleware pattern".
///
/// FLUJO SIN BEHAVIORS:
/// Controller → MediatR → Handler → Response
///
/// FLUJO CON BEHAVIORS:
/// Controller → MediatR → [Behavior 1] → [Behavior 2] → Handler → Response
///                           ↑              ↑
///                      Logging       Validation
///
/// Cada behavior puede:
/// - Ejecutar lógica ANTES del handler (pre-processing)
/// - Ejecutar lógica DESPUÉS del handler (post-processing)
/// - Cortocircuitar el pipeline (no llamar handler)
/// - Modificar request o response
/// - Manejar excepciones
///
/// ORDEN DE EJECUCIÓN:
///
/// Se ejecutan en orden de registro en DI:
///
/// services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
/// services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
///
/// Request → LoggingBehavior → ValidationBehavior → Handler
///                 ↓                    ↓
///            Log request          Validate
///
/// Response ← Log response ← Validación OK ← Handler
///
/// IMPORTANTE: Orden importa!
/// - Logging ANTES de validation (para loguear requests inválidos)
/// - Validation ANTES de handler (para evitar ejecutar lógica con datos inválidos)
///
/// VALIDATION BEHAVIOR:
///
/// Este behavior automáticamente valida todos los Commands y Queries
/// usando FluentValidation validators registrados en DI.
///
/// SIN ValidationBehavior (manual en cada handler):
///
/// ❌ Repetitivo y propenso a errores:
///
/// public class CreateTaskCommandHandler
/// {
///     private readonly IValidator<CreateTaskCommand> _validator;
///
///     public async Task<Result> Handle(CreateTaskCommand request, CancellationToken ct)
///     {
///         // Validar manualmente
///         var validationResult = await _validator.ValidateAsync(request, ct);
///         if (!validationResult.IsValid)
///         {
///             var errors = string.Join(". ", validationResult.Errors.Select(e => e.ErrorMessage));
///             return Result.Failure(errors);
///         }
///
///         // Lógica de negocio...
///     }
/// }
///
/// Problemas:
/// - Código de validación repetido en CADA handler
/// - Fácil olvidar validar
/// - Inconsistente (algunos handlers validan, otros no)
/// - Mezcla validación con lógica de negocio
///
/// ✅ CON ValidationBehavior (automático):
///
/// public class CreateTaskCommandHandler
/// {
///     public async Task<Result> Handle(CreateTaskCommand request, CancellationToken ct)
///     {
///         // Validación ya ejecutada por behavior!
///         // Si llegamos aquí, request es válido
///
///         // Solo lógica de negocio
///         var task = TaskItem.Create(request.Title, request.Description, ...);
///         _context.Tasks.Add(task);
///         await _context.SaveChangesAsync(ct);
///
///         return Result.Success();
///     }
/// }
///
/// Ventajas:
/// - Sin código de validación en handlers (más limpios)
/// - Validación garantizada (automática para todos)
/// - Consistente (mismo comportamiento en todos los handlers)
/// - Separación de concerns (validación vs lógica de negocio)
///
/// CÓMO FUNCIONA:
///
/// 1. Request llega al behavior
/// 2. Behavior busca IValidator<TRequest> en DI
/// 3. Si NO hay validator → continúa al handler (no todos los requests necesitan validación)
/// 4. Si HAY validator → ejecuta validación
/// 5. Si validación FALLA → retorna Result.Failure con errores (cortocircuito)
/// 6. Si validación PASA → continúa al handler
///
/// EJEMPLO COMPLETO:
///
/// // 1. Definir Command
/// public class CreateTaskCommand : IRequest<Result<TaskDto>>
/// {
///     public string Title { get; set; } = string.Empty;
///     public string? Description { get; set; }
///     public DateTime? DueDate { get; set; }
/// }
///
/// // 2. Definir Validator
/// public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
/// {
///     public CreateTaskCommandValidator()
///     {
///         RuleFor(x => x.Title)
///             .NotEmpty()
///             .WithMessage("Title is required")
///             .MaximumLength(200)
///             .WithMessage("Title must not exceed 200 characters");
///
///         RuleFor(x => x.Description)
///             .MaximumLength(2000)
///             .WithMessage("Description must not exceed 2000 characters");
///
///         RuleFor(x => x.DueDate)
///             .GreaterThan(DateTime.UtcNow)
///             .When(x => x.DueDate.HasValue)
///             .WithMessage("Due date must be in the future");
///     }
/// }
///
/// // 3. Registrar Validator en DI
/// services.AddValidatorsFromAssembly(typeof(CreateTaskCommandValidator).Assembly);
///
/// // 4. Registrar ValidationBehavior en DI
/// services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
///
/// // 5. Handler (SIN código de validación)
/// public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Result<TaskDto>>
/// {
///     public async Task<Result<TaskDto>> Handle(CreateTaskCommand request, CancellationToken ct)
///     {
///         // request.Title ya está validado (no vacío, <= 200 chars)
///         // request.Description ya está validado (<= 2000 chars)
///         // request.DueDate ya está validado (futuro si existe)
///
///         var task = TaskItem.Create(request.Title, request.Description, ...);
///         // ... resto de la lógica
///     }
/// }
///
/// // 6. Uso en Controller
/// [HttpPost]
/// public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
/// {
///     var command = new CreateTaskCommand
///     {
///         Title = request.Title,
///         Description = request.Description,
///         DueDate = request.DueDate
///     };
///
///     var result = await _mediator.Send(command);
///
///     if (result.IsFailure)
///         return BadRequest(new { error = result.Error });
///         // Si validación falló, result.Error = "Title is required. Description must not exceed 2000 characters."
///
///     return Ok(result.Value);
/// }
///
/// FORMATO DE ERRORES:
///
/// Si múltiples validaciones fallan:
///
/// ValidationResult.Errors:
/// - "Title is required"
/// - "Description must not exceed 2000 characters"
/// - "Due date must be in the future"
///
/// Se concatenan con ". ":
/// "Title is required. Description must not exceed 2000 characters. Due date must be in the future."
///
/// Y se retornan en Result.Failure().
///
/// ALTERNATIVA - Retornar Lista de Errores:
///
/// Si prefieres retornar lista en lugar de string concatenado:
///
/// public class Result
/// {
///     public List<string> Errors { get; }
///
///     public static Result Failure(List<string> errors) => new Result(false, errors);
/// }
///
/// En ValidationBehavior:
/// var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
/// return Result.Failure(errors);
///
/// Para este proyecto usamos string concatenado (más simple).
///
/// VALIDACIONES ASÍNCRONAS:
///
/// FluentValidation soporta validaciones async (ej: verificar en BD):
///
/// public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
/// {
///     private readonly IApplicationDbContext _context;
///
///     public CreateTaskCommandValidator(IApplicationDbContext context)
///     {
///         _context = context;
///
///         RuleFor(x => x.Title)
///             .MustAsync(BeUniqueTitle)
///             .WithMessage("Task with this title already exists");
///     }
///
///     private async Task<bool> BeUniqueTitle(string title, CancellationToken ct)
///     {
///         return !await _context.Tasks.AnyAsync(t => t.Title == title, ct);
///     }
/// }
///
/// ValidationBehavior automáticamente usa ValidateAsync(), soportando async.
///
/// PERFORMANCE:
///
/// Validación es rápida (< 1ms para reglas simples).
/// Validaciones async (queries a BD) pueden ser más lentas (~10-50ms).
///
/// Para validaciones costosas, considerar:
/// - Cachear resultados
/// - Usar índices en BD
/// - Limitar validaciones async a casos críticos
///
/// TESTING:
///
/// Validator tests (unit):
///
/// [Fact]
/// public void Validate_EmptyTitle_ReturnsError()
/// {
///     var validator = new CreateTaskCommandValidator();
///     var command = new CreateTaskCommand { Title = "" };
///
///     var result = validator.Validate(command);
///
///     Assert.False(result.IsValid);
///     Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskCommand.Title));
/// }
///
/// Integration test (con behavior):
///
/// [Fact]
/// public async Task CreateTask_InvalidTitle_ReturnsFailure()
/// {
///     var command = new CreateTaskCommand { Title = "" };
///
///     var result = await _mediator.Send(command);
///
///     Assert.True(result.IsFailure);
///     Assert.Contains("Title is required", result.Error);
/// }
///
/// VALIDACIÓN EN CLIENTE vs SERVIDOR:
///
/// Siempre validar en SERVIDOR (backend):
/// - Cliente puede ser manipulado
/// - Validación de servidor es la única garantía
/// - Protege contra ataques maliciosos
///
/// Validación en cliente (frontend) es opcional:
/// - Mejora UX (feedback inmediato)
/// - Reduce requests inválidos
/// - Pero NO es suficiente para seguridad
///
/// NUNCA confiar solo en validación de cliente.
///
/// ALTERNATIVAS:
///
/// 1. Data Annotations (built-in .NET):
///    [Required]
///    [MaxLength(200)]
///    public string Title { get; set; }
///
///    Pros: Simple, built-in
///    Cons: Limitado, difícil de testear, mezcla validación con modelo
///
/// 2. FluentValidation (este proyecto):
///    RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
///
///    Pros: Fluido, testeable, flexible, separado del modelo
///    Cons: Dependencia extra
///
/// 3. Manual en Handler:
///    if (string.IsNullOrEmpty(request.Title))
///        return Result.Failure("Title is required");
///
///    Pros: Control total
///    Cons: Repetitivo, inconsistente
///
/// FluentValidation es el estándar de facto para aplicaciones complejas.
///
/// COMPORTAMIENTO CON QUERIES:
///
/// También funciona con Queries:
///
/// public class GetTasksQuery : IRequest<Result<PaginatedList<TaskDto>>>
/// {
///     public int Page { get; set; }
///     public int PageSize { get; set; }
/// }
///
/// public class GetTasksQueryValidator : AbstractValidator<GetTasksQuery>
/// {
///     public GetTasksQueryValidator()
///     {
///         RuleFor(x => x.Page)
///             .GreaterThanOrEqualTo(1)
///             .WithMessage("Page must be at least 1");
///
///         RuleFor(x => x.PageSize)
///             .GreaterThanOrEqualTo(1)
///             .LessThanOrEqualTo(100)
///             .WithMessage("Page size must be between 1 and 100");
///     }
/// }
///
/// ValidationBehavior valida automáticamente antes de ejecutar query handler.
/// </remarks>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Constructor que inyecta todos los validators para TRequest.
    /// </summary>
    /// <param name="validators">Colección de validators (puede estar vacía si no hay validators para TRequest).</param>
    /// <remarks>
    /// MediatR automáticamente inyecta todos los IValidator&lt;TRequest&gt; registrados en DI.
    ///
    /// Si NO hay validators para TRequest:
    /// - _validators será colección vacía
    /// - Behavior simplemente continúa al siguiente (sin validación)
    ///
    /// Si HAY validators:
    /// - _validators contiene todos los validators
    /// - Se ejecutan todos y se agregan errores
    ///
    /// Múltiples validators (raro, pero posible):
    /// public class CreateTaskCommandValidator1 : AbstractValidator<CreateTaskCommand> { }
    /// public class CreateTaskCommandValidator2 : AbstractValidator<CreateTaskCommand> { }
    ///
    /// Ambos se ejecutarán y errores se combinarán.
    ///
    /// Generalmente solo hay 1 validator por request.
    /// </remarks>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Ejecuta validación antes de llamar al handler.
    /// </summary>
    /// <param name="request">Request a validar (Command o Query).</param>
    /// <param name="next">Siguiente behavior o handler en el pipeline.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Response del handler o Result.Failure si validación falla.</returns>
    /// <remarks>
    /// Flujo:
    /// 1. Si NO hay validators → continuar a next() (handler)
    /// 2. Si HAY validators → ejecutar validación
    /// 3. Si validación FALLA → retornar Result.Failure (cortocircuito, no llamar handler)
    /// 4. Si validación PASA → continuar a next() (handler)
    ///
    /// IMPORTANTE: next() puede ser:
    /// - Otro behavior (si hay más en el pipeline)
    /// - El handler final
    ///
    /// Example pipeline:
    /// ValidationBehavior → LoggingBehavior → Handler
    ///
    /// Cuando ValidationBehavior llama next():
    /// - Va a LoggingBehavior
    /// - LoggingBehavior llama next()
    /// - Va al Handler
    /// - Handler retorna response
    /// - Response vuelve a LoggingBehavior
    /// - LoggingBehavior lo loguea y retorna
    /// - Response vuelve a ValidationBehavior
    /// - ValidationBehavior retorna al MediatR
    /// - MediatR retorna al Controller
    /// </remarks>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // 1. Si no hay validators, continuar al siguiente behavior/handler
        // No todos los requests necesitan validación
        if (!_validators.Any())
        {
            return await next();
        }

        // 2. Ejecutar todos los validators
        // Usamos ValidationContext para pasar metadata si es necesario
        var context = new ValidationContext<TRequest>(request);

        // 3. Ejecutar validaciones en paralelo (performance)
        // ValidateAsync() soporta validaciones async (ej: queries a BD)
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        // 4. Recolectar todos los errores de todos los validators
        var failures = validationResults
            .SelectMany(result => result.Errors)  // Flatten
            .Where(error => error != null)        // Filter nulls
            .ToList();

        // 5. Si hay errores, retornar Result.Failure (cortocircuito)
        if (failures.Any())
        {
            // Concatenar todos los mensajes de error separados por ". "
            var errorMessage = string.Join(". ", failures.Select(f => f.ErrorMessage));

            // Crear Result.Failure usando reflexión para soportar Result y Result<T>
            // TResponse puede ser Result o Result<T>, necesitamos crear instancia del tipo correcto
            return CreateFailureResult(errorMessage);
        }

        // 6. Si no hay errores, continuar al siguiente behavior/handler
        return await next();
    }

    /// <summary>
    /// Crea un Result.Failure del tipo TResponse usando reflexión.
    /// </summary>
    /// <param name="errorMessage">Mensaje de error.</param>
    /// <returns>Result.Failure o Result&lt;T&gt;.Failure según TResponse.</returns>
    /// <remarks>
    /// PROBLEMA:
    /// TResponse puede ser:
    /// - Result (para Commands que no retornan valor)
    /// - Result&lt;TaskDto&gt; (para Commands/Queries que retornan valor)
    /// - Result&lt;PaginatedList&lt;TaskDto&gt;&gt;
    /// - Etc.
    ///
    /// No podemos hacer simplemente:
    /// return Result.Failure(errorMessage);  // ❌ No compila si TResponse = Result<T>
    ///
    /// SOLUCIÓN:
    /// Usar reflexión para llamar al método Failure correcto según TResponse.
    ///
    /// Si TResponse = Result:
    /// → Llamar Result.Failure(errorMessage)
    ///
    /// Si TResponse = Result&lt;TaskDto&gt;:
    /// → Llamar Result&lt;TaskDto&gt;.Failure(errorMessage)
    ///
    /// ALTERNATIVA (sin reflexión):
    /// Hacer ValidationBehavior no genérico y tener 2 versiones:
    /// - ValidationBehavior&lt;TRequest&gt; para Result
    /// - ValidationBehavior&lt;TRequest, T&gt; para Result&lt;T&gt;
    ///
    /// Pero eso es más complejo y repetitivo.
    /// Reflexión es más simple aquí (y performance no es problema, solo se ejecuta en errores).
    /// </remarks>
    private static TResponse CreateFailureResult(string errorMessage)
    {
        // Obtener el tipo de TResponse
        var responseType = typeof(TResponse);

        // Si es Result (no genérico), usar Result.Failure(errorMessage)
        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(errorMessage);
        }

        // Si es Result<T> (genérico), usar Result<T>.Failure(errorMessage)
        // Obtener el tipo T de Result<T>
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];

            // Obtener método estático Result<T>.Failure(string)
            var failureMethod = responseType.GetMethod(
                nameof(Result.Failure),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null
            );

            if (failureMethod != null)
            {
                // Invocar Result<T>.Failure(errorMessage)
                return (TResponse)failureMethod.Invoke(null, new object[] { errorMessage })!;
            }
        }

        // Fallback (no debería llegar aquí si TResponse es Result o Result<T>)
        throw new InvalidOperationException($"Cannot create failure result for type {responseType.Name}");
    }
}
