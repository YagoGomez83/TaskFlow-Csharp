namespace TaskManagement.Domain.Exceptions;

/// <summary>
/// Excepción lanzada cuando una entidad no se encuentra en el repositorio.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE EntityNotFoundException:
///
/// Esta excepción específica se usa cuando se busca una entidad por su ID
/// (o cualquier otro identificador) y no se encuentra en la base de datos.
///
/// DIFERENCIA CON DomainException:
/// - DomainException: Genérica para reglas de negocio
/// - EntityNotFoundException: Específica para entidad no encontrada
/// - Hereda de DomainException para mantener jerarquía
///
/// POR QUÉ UNA CLASE SEPARADA:
///
/// 1. SEMÁNTICA CLARA:
///    - El nombre comunica exactamente qué sucedió
///    - Facilita debugging y logs
///
/// 2. MANEJO ESPECÍFICO:
///    - Permite catch específico de "not found"
///    - Diferente HTTP status (404 vs 400)
///
/// 3. INFORMACIÓN ESTRUCTURADA:
///    - Incluye nombre de entidad y ID
///    - Facilita logging y monitoreo
///
/// USOS COMUNES:
///
/// // En Repository
/// public async Task<TaskItem> GetByIdAsync(Guid id)
/// {
///     var task = await _context.Tasks.FindAsync(id);
///     if (task == null)
///         throw new EntityNotFoundException(nameof(TaskItem), id);
///
///     return task;
/// }
///
/// // En Handler
/// var task = await _taskRepository.GetByIdAsync(request.Id);
/// // Si no existe, lanza EntityNotFoundException automáticamente
///
/// MANEJO EN API LAYER:
///
/// // ExceptionHandlingMiddleware.cs
/// catch (EntityNotFoundException ex)
/// {
///     return new ProblemDetails
///     {
///         Status = 404,
///         Title = "Resource not found",
///         Detail = ex.Message,
///         Type = "https://httpstatuses.com/404"
///     };
/// }
///
/// RESPUESTA HTTP:
/// {
///   "type": "https://httpstatuses.com/404",
///   "title": "Resource not found",
///   "status": 404,
///   "detail": "TaskItem with ID 123e4567-e89b-12d3-a456-426614174000 was not found"
/// }
///
/// ALTERNATIVA - RESULT PATTERN (sin excepciones):
///
/// public async Task<Result<TaskItem>> GetByIdAsync(Guid id)
/// {
///     var task = await _context.Tasks.FindAsync(id);
///     if (task == null)
///         return Result.Failure<TaskItem>($"TaskItem with ID {id} not found");
///
///     return Result.Success(task);
/// }
///
/// VENTAJAS DEL RESULT PATTERN:
/// ✅ Sin overhead de excepciones
/// ✅ Flujo de control más explícito
/// ✅ Facilita composición de operaciones
///
/// VENTAJAS DE EXCEPCIONES:
/// ✅ Menos boilerplate
/// ✅ Manejo centralizado en middleware
/// ✅ Stack trace para debugging
///
/// En este proyecto usamos excepciones por simplicidad y para aprovechar
/// el manejo centralizado en ExceptionHandlingMiddleware.
/// </remarks>
public class EntityNotFoundException : DomainException
{
    /// <summary>
    /// Nombre de la entidad que no fue encontrada.
    /// </summary>
    /// <remarks>
    /// Ejemplo: "TaskItem", "User", "RefreshToken"
    ///
    /// Útil para:
    /// - Logging estructurado
    /// - Métricas (contar cuántas veces no se encuentra cada entidad)
    /// - Debugging
    /// </remarks>
    public string EntityName { get; }

    /// <summary>
    /// ID de la entidad que se buscó.
    /// </summary>
    /// <remarks>
    /// Guardado como object porque puede ser:
    /// - Guid (más común en este proyecto)
    /// - int (en otros proyectos)
    /// - string (composite keys, slugs)
    /// - object compuesto (múltiples IDs)
    ///
    /// Útil para:
    /// - Logging: saber exactamente qué ID se buscó
    /// - Debugging: reproducir el error
    /// - Auditoría: rastrear intentos de acceso
    /// </remarks>
    public object Id { get; }

    /// <summary>
    /// Crea una nueva instancia de EntityNotFoundException.
    /// </summary>
    /// <param name="entityName">Nombre de la entidad (ej: "TaskItem").</param>
    /// <param name="id">ID de la entidad que se buscó.</param>
    /// <remarks>
    /// El mensaje se construye automáticamente en formato estándar:
    /// "{EntityName} with ID {id} was not found"
    ///
    /// Ejemplo:
    /// throw new EntityNotFoundException("TaskItem", taskId);
    /// // Mensaje: "TaskItem with ID 123e4567-e89b-12d3-a456-426614174000 was not found"
    ///
    /// USO CON nameof():
    /// throw new EntityNotFoundException(nameof(TaskItem), id);
    ///
    /// Ventajas de nameof():
    /// ✅ Type-safe: error de compilación si renombras la clase
    /// ✅ Refactoring-safe: se actualiza automáticamente
    /// ✅ No hay strings hardcoded
    /// </remarks>
    public EntityNotFoundException(string entityName, object id)
        : base($"{entityName} with ID {id} was not found")
    {
        EntityName = entityName;
        Id = id;
    }

    /// <summary>
    /// Crea una nueva instancia de EntityNotFoundException con mensaje personalizado.
    /// </summary>
    /// <param name="entityName">Nombre de la entidad.</param>
    /// <param name="id">ID de la entidad.</param>
    /// <param name="message">Mensaje personalizado.</param>
    /// <remarks>
    /// Útil cuando necesitas un mensaje más específico.
    ///
    /// Ejemplo:
    /// throw new EntityNotFoundException(
    ///     "TaskItem",
    ///     id,
    ///     $"Task {id} was not found or was deleted"
    /// );
    ///
    /// También útil para internacionalización (i18n):
    /// var message = _localizer["TaskNotFound", id];
    /// throw new EntityNotFoundException("TaskItem", id, message);
    /// </remarks>
    public EntityNotFoundException(string entityName, object id, string message)
        : base(message)
    {
        EntityName = entityName;
        Id = id;
    }
}
