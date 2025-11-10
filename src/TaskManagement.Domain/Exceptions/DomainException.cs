namespace TaskManagement.Domain.Exceptions;

/// <summary>
/// Excepción base para violaciones de reglas de negocio en el dominio.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE EXCEPCIONES EN DOMAIN LAYER:
///
/// Las excepciones de dominio representan violaciones de reglas de negocio.
/// Se lanzan cuando el estado del dominio es inválido o inconsistente.
///
/// DIFERENCIA CON OTRAS EXCEPCIONES:
///
/// 1. DomainException (esta clase):
///    - Violación de reglas de negocio
///    - Ejemplo: "No se puede completar una tarea ya completada"
///    - Capa: Domain
///    - HTTP Status: 400 Bad Request o 409 Conflict
///
/// 2. ValidationException (FluentValidation):
///    - Error de validación de input
///    - Ejemplo: "El título no puede estar vacío"
///    - Capa: Application
///    - HTTP Status: 400 Bad Request
///
/// 3. EntityNotFoundException (herede de DomainException):
///    - Entidad no encontrada
///    - Ejemplo: "Task con ID xxx no existe"
///    - Capa: Domain/Application
///    - HTTP Status: 404 Not Found
///
/// 4. UnauthorizedAccessException (System):
///    - Acceso no autorizado
///    - Ejemplo: "No puedes editar tareas de otros usuarios"
///    - Capa: Application
///    - HTTP Status: 403 Forbidden
///
/// CUÁNDO LANZAR DomainException:
///
/// ✅ USAR cuando:
/// - Una operación viola reglas de negocio
/// - El estado resultante sería inválido
/// - Es responsabilidad del caller prevenir
///
/// ❌ NO USAR cuando:
/// - Es un error de validación de input (usar FluentValidation)
/// - Es un error de infraestructura (DB down, network error)
/// - Es un error de lógica de programación (usar ArgumentException)
///
/// EJEMPLOS DE USO:
///
/// // En User.cs - Regla de negocio
/// public void UpdateEmail(Email newEmail)
/// {
///     if (IsLockedOut)
///         throw new DomainException("Cannot update email of locked out user");
///
///     Email = newEmail;
/// }
///
/// // En TaskItem.cs - Regla de negocio
/// public void Complete()
/// {
///     if (Status == TaskStatus.Completed)
///         throw new DomainException("Task is already completed");
///
///     Status = TaskStatus.Completed;
/// }
///
/// MANEJO DE EXCEPCIONES EN CAPAS SUPERIORES:
///
/// Domain Layer:
/// throw new DomainException("Message");
///
/// Application Layer (Handler):
/// try
/// {
///     user.UpdateEmail(newEmail);
/// }
/// catch (DomainException ex)
/// {
///     return Result.Failure(ex.Message);
/// }
///
/// API Layer (Middleware):
/// catch (DomainException ex)
/// {
///     return new ProblemDetails
///     {
///         Status = 400,
///         Title = "Business rule violation",
///         Detail = ex.Message
///     };
/// }
///
/// MEJORA FUTURA - CÓDIGOS DE ERROR:
///
/// public class DomainException : Exception
/// {
///     public string ErrorCode { get; }
///
///     public DomainException(string errorCode, string message)
///         : base(message)
///     {
///         ErrorCode = errorCode;
///     }
/// }
///
/// Uso:
/// throw new DomainException("TASK_ALREADY_COMPLETED", "Task is already completed");
///
/// Beneficio:
/// - El cliente puede manejar errores específicos
/// - Facilita internacionalización (i18n)
/// - Permite lógica condicional por código de error
/// </remarks>
public class DomainException : Exception
{
    /// <summary>
    /// Crea una nueva instancia de DomainException con un mensaje.
    /// </summary>
    /// <param name="message">Mensaje que describe la violación de regla de negocio.</param>
    /// <remarks>
    /// El mensaje debe ser:
    /// - Descriptivo: Explicar qué regla se violó
    /// - User-friendly: Puede mostrarse al usuario final
    /// - Específico: No genérico como "Error"
    ///
    /// Buenos mensajes:
    /// ✅ "Cannot complete a task that is already completed"
    /// ✅ "Email format is invalid"
    /// ✅ "User account is locked out"
    ///
    /// Malos mensajes:
    /// ❌ "Error"
    /// ❌ "Invalid operation"
    /// ❌ "Something went wrong"
    /// </remarks>
    public DomainException(string message) : base(message)
    {
    }

    /// <summary>
    /// Crea una nueva instancia de DomainException con un mensaje y una excepción interna.
    /// </summary>
    /// <param name="message">Mensaje que describe la violación de regla de negocio.</param>
    /// <param name="innerException">Excepción que causó esta excepción.</param>
    /// <remarks>
    /// Útil cuando una DomainException es causada por otra excepción.
    ///
    /// Ejemplo:
    /// try
    /// {
    ///     // Operación que puede fallar
    /// }
    /// catch (SomeException ex)
    /// {
    ///     throw new DomainException("Failed to complete operation", ex);
    /// }
    ///
    /// Esto preserva el stack trace completo para debugging.
    /// </remarks>
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
