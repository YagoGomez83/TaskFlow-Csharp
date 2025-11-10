using FluentValidation;
using TaskManagement.Application.UseCases.Tasks.Queries;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.Validators.Tasks;

/// <summary>
/// Validator para GetTasksQuery.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE QUERY VALIDATION:
///
/// GetTasksQuery valida parámetros de consulta para listar tareas:
/// - Page: número de página (>= 1)
/// - PageSize: cantidad de items por página (1-100)
/// - Status: filtro opcional por estado
/// - Priority: filtro opcional por prioridad
///
/// VALIDACIÓN DE PAGINACIÓN:
///
/// Parámetros de paginación son críticos para performance.
/// Sin validación, cliente podría:
///
/// ❌ GET /api/tasks?page=0
/// → OFFSET -20 ROWS (error SQL)
///
/// ❌ GET /api/tasks?pageSize=10000
/// → SELECT * OFFSET 0 ROWS FETCH NEXT 10000 ROWS
/// → Query muy lento, mucha memoria, timeout
///
/// ❌ GET /api/tasks?page=-5&pageSize=-10
/// → Resultados impredecibles
///
/// ✅ CON VALIDACIÓN:
/// Page: >= 1
/// PageSize: 1-100 (límite razonable)
///
/// PAGE NUMBERING:
///
/// Dos convenciones:
///
/// 1. 1-INDEXED (este proyecto):
///    - Página 1 = primeros items
///    - Página 2 = siguientes items
///    - Más natural para usuarios
///    - OFFSET = (Page - 1) * PageSize
///
/// 2. 0-INDEXED (alternativa):
///    - Página 0 = primeros items
///    - Página 1 = siguientes items
///    - Más natural para programadores
///    - OFFSET = Page * PageSize
///
/// Elegimos 1-indexed porque:
/// - Más intuitivo para UI ("Página 1 de 10")
/// - Estándar en la mayoría de APIs
///
/// PAGE SIZE LIMITS:
///
/// Limitar pageSize previene abuse:
///
/// RuleFor(x => x.PageSize)
///     .GreaterThanOrEqualTo(1)
///     .LessThanOrEqualTo(100);
///
/// Limites comunes:
/// - GitHub: max 100
/// - Twitter: max 200
/// - Stack Overflow: max 100
///
/// Para este proyecto: max 100
///
/// Si cliente necesita todos los items:
/// - Hacer múltiples requests paginados
/// - O crear endpoint separado: GET /api/tasks/all (con rate limiting)
///
/// DEFAULT VALUES:
///
/// En el Command/Query, definir defaults:
///
/// public class GetTasksQuery : IRequest<Result<PaginatedList<TaskDto>>>
/// {
///     public int Page { get; set; } = 1;        // Default: primera página
///     public int PageSize { get; set; } = 20;   // Default: 20 items
/// }
///
/// Si cliente no especifica:
/// GET /api/tasks
/// → Page = 1, PageSize = 20 (defaults)
///
/// Si cliente especifica:
/// GET /api/tasks?page=2&pageSize=50
/// → Page = 2, PageSize = 50
///
/// FILTROS OPCIONALES:
///
/// Status y Priority son opcionales (nullable):
///
/// public TaskStatus? Status { get; set; }
/// public TaskPriority? Priority { get; set; }
///
/// Si null: No filtrar
/// Si tiene valor: Aplicar filtro
///
/// Ejemplos:
/// GET /api/tasks
/// → No filtros, retornar todas
///
/// GET /api/tasks?status=InProgress
/// → Solo tareas InProgress
///
/// GET /api/tasks?status=InProgress&priority=High
/// → Tareas InProgress Y High priority
///
/// VALIDACIÓN DE FILTROS:
///
/// RuleFor(x => x.Status)
///     .IsInEnum()
///     .When(x => x.Status.HasValue);
///
/// Solo validar si tiene valor.
/// Si null, omitir validación.
///
/// Esto permite:
/// - Status = null → OK (sin filtro)
/// - Status = TaskStatus.Pending → OK
/// - Status = (TaskStatus)99 → ERROR
///
/// QUERY STRING PARSING:
///
/// ASP.NET Core automáticamente parsea query string a objeto:
///
/// GET /api/tasks?page=2&pageSize=50&status=InProgress&priority=High
///
/// Se mapea a:
/// new GetTasksQuery
/// {
///     Page = 2,
///     PageSize = 50,
///     Status = TaskStatus.InProgress,
///     Priority = TaskPriority.High
/// }
///
/// Si parsing falla (ej: status=InvalidValue), retorna 400 antes de validator.
///
/// CASE INSENSITIVE ENUMS:
///
/// Por defecto, enum parsing es case-sensitive:
/// - ?status=InProgress → OK
/// - ?status=inprogress → ERROR 400
///
/// Para case-insensitive, configurar:
///
/// builder.Services.AddControllers()
///     .AddJsonOptions(options =>
///     {
///         options.JsonSerializerOptions.Converters.Add(
///             new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
///         );
///     });
///
/// Con esto:
/// - ?status=InProgress → TaskStatus.InProgress
/// - ?status=inprogress → TaskStatus.InProgress
/// - ?status=INPROGRESS → TaskStatus.InProgress
///
/// ORDENAMIENTO (opcional):
///
/// Si agregas ordenamiento:
///
/// public class GetTasksQuery
/// {
///     public string OrderBy { get; set; } = "createdAt";  // Default
///     public string OrderDirection { get; set; } = "desc";  // asc/desc
/// }
///
/// Validación:
/// RuleFor(x => x.OrderBy)
///     .Must(x => new[] { "createdAt", "updatedAt", "dueDate", "priority", "title" }.Contains(x))
///     .WithMessage("OrderBy must be one of: createdAt, updatedAt, dueDate, priority, title");
///
/// RuleFor(x => x.OrderDirection)
///     .Must(x => new[] { "asc", "desc" }.Contains(x.ToLower()))
///     .WithMessage("OrderDirection must be 'asc' or 'desc'");
///
/// Para MVP, no implementamos ordenamiento custom (siempre por CreatedAt DESC).
///
/// PERFORMANCE:
///
/// Validación de queries es crítica para performance:
///
/// ✅ Con límites:
/// SELECT * FROM Tasks ORDER BY CreatedAt DESC OFFSET 0 ROWS FETCH NEXT 20 ROWS;
/// → ~50ms
///
/// ❌ Sin límites:
/// SELECT * FROM Tasks ORDER BY CreatedAt DESC OFFSET 0 ROWS FETCH NEXT 10000 ROWS;
/// → ~5 segundos, mucha memoria
///
/// RATE LIMITING:
///
/// Combinar validación con rate limiting:
///
/// [RateLimit(PermitLimit = 100, Window = "1m")]
/// public async Task<IActionResult> GetTasks([FromQuery] GetTasksQuery query)
/// {
///     // Máximo 100 requests por minuto por usuario
/// }
///
/// Previene abuse de API.
///
/// TESTING:
///
/// [Fact]
/// public void Validate_PageZero_ReturnsError()
/// {
///     var validator = new GetTasksQueryValidator();
///     var query = new GetTasksQuery { Page = 0, PageSize = 20 };
///
///     var result = validator.Validate(query);
///
///     Assert.False(result.IsValid);
///     Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTasksQuery.Page));
/// }
///
/// [Fact]
/// public void Validate_PageSizeTooLarge_ReturnsError()
/// {
///     var validator = new GetTasksQueryValidator();
///     var query = new GetTasksQuery { Page = 1, PageSize = 1000 };
///
///     var result = validator.Validate(query);
///
///     Assert.False(result.IsValid);
///     Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("100"));
/// }
///
/// [Fact]
/// public void Validate_InvalidStatus_ReturnsError()
/// {
///     var validator = new GetTasksQueryValidator();
///     var query = new GetTasksQuery
///     {
///         Page = 1,
///         PageSize = 20,
///         Status = (TaskStatus)99
///     };
///
///     var result = validator.Validate(query);
///
///     Assert.False(result.IsValid);
/// }
///
/// [Fact]
/// public void Validate_ValidQuery_Passes()
/// {
///     var validator = new GetTasksQueryValidator();
///     var query = new GetTasksQuery
///     {
///         Page = 2,
///         PageSize = 50,
///         Status = TaskStatus.InProgress,
///         Priority = TaskPriority.High
///     };
///
///     var result = validator.Validate(query);
///
///     Assert.True(result.IsValid);
/// }
///
/// [Fact]
/// public void Validate_NoFilters_Passes()
/// {
///     var validator = new GetTasksQueryValidator();
///     var query = new GetTasksQuery
///     {
///         Page = 1,
///         PageSize = 20
///         // Status y Priority son null (sin filtros)
///     };
///
///     var result = validator.Validate(query);
///
///     Assert.True(result.IsValid);
/// }
///
/// FRONTEND EXAMPLE:
///
/// interface GetTasksParams {
///   page?: number;
///   pageSize?: number;
///   status?: 'Pending' | 'InProgress' | 'Completed';
///   priority?: 'Low' | 'Medium' | 'High';
/// }
///
/// async function getTasks(params: GetTasksParams = {}) {
///   const queryParams = new URLSearchParams();
///
///   if (params.page) queryParams.set('page', params.page.toString());
///   if (params.pageSize) queryParams.set('pageSize', params.pageSize.toString());
///   if (params.status) queryParams.set('status', params.status);
///   if (params.priority) queryParams.set('priority', params.priority);
///
///   const response = await fetch(`/api/tasks?${queryParams}`, {
///     headers: { 'Authorization': `Bearer ${accessToken}` }
///   });
///
///   return response.json();
/// }
///
/// // Uso
/// const tasks = await getTasks({ page: 2, pageSize: 50, status: 'InProgress' });
/// </remarks>
public class GetTasksQueryValidator : AbstractValidator<GetTasksQuery>
{
    /// <summary>
    /// Constructor que define reglas de validación.
    /// </summary>
    public GetTasksQueryValidator()
    {
        // Validación para Page (número de página)
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1");
        // Páginas empiezan en 1, no en 0
        // OFFSET = (Page - 1) * PageSize
        // Página 1: OFFSET 0
        // Página 2: OFFSET PageSize

        // Validación para PageSize (cantidad de items por página)
        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page size must be at least 1")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must not exceed 100");
        // Mínimo: 1 (al menos 1 item)
        // Máximo: 100 (prevenir queries muy grandes)
        // Cliente que necesita más debe hacer múltiples requests

        // Validación para Status (filtro opcional)
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status must be a valid value (Pending, InProgress, Completed)")
            .When(x => x.Status.HasValue);
        // Only validate if Status is not null
        // null = no filter (return all statuses)

        // Validación para Priority (filtro opcional)
        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Priority must be a valid value (Low, Medium, High)")
            .When(x => x.Priority.HasValue);
        // Only validate if Priority is not null
        // null = no filter (return all priorities)
    }
}
