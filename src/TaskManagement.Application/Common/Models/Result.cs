namespace TaskManagement.Application.Common.Models;

/// <summary>
/// Representa el resultado de una operación que puede tener éxito o fallar.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DEL RESULT PATTERN:
///
/// El Result Pattern es una alternativa a las excepciones para manejar errores.
/// En lugar de throw/catch, retornamos un objeto Result que indica éxito o fallo.
///
/// PROBLEMA CON EXCEPCIONES:
///
/// ❌ Excepciones para flujo de control:
/// try
/// {
///     var user = await _userService.GetUserAsync(id);
///     // user puede no existir, pero no es excepcional
/// }
/// catch (UserNotFoundException ex)
/// {
///     return NotFound(); // Flujo normal, no excepcional
/// }
///
/// Problemas:
/// - Performance: Excepciones son costosas (stack unwinding)
/// - Flujo de control: Difícil seguir el flujo con try/catch anidados
/// - Semántica: "Not found" no es excepcional, es un caso de negocio válido
///
/// ✅ SOLUCIÓN CON RESULT PATTERN:
/// var result = await _userService.GetUserAsync(id);
/// if (result.IsFailure)
///     return NotFound(result.Error);
///
/// var user = result.Value;
/// // Trabajar con user...
///
/// Ventajas:
/// - Performance: Sin overhead de excepciones
/// - Legibilidad: Flujo explícito y claro
/// - Type-safe: El compilador te obliga a manejar ambos casos
/// - Composición: Fácil encadenar operaciones
///
/// CUÁNDO USAR RESULT vs EXCEPCIONES:
///
/// USAR RESULT para:
/// ✅ Errores esperados de negocio
/// ✅ Validaciones
/// ✅ Not found
/// ✅ Unauthorized
/// ✅ Conflictos de negocio
///
/// USAR EXCEPCIONES para:
/// ✅ Errores inesperados (bugs)
/// ✅ Errores de infraestructura (DB down, network error)
/// ✅ Errores irrecuperables
/// ✅ Violaciones de invariantes
///
/// EJEMPLOS DE USO:
///
/// // Crear result exitoso
/// return Result.Success();
///
/// // Crear result exitoso con valor
/// return Result.Success(user);
///
/// // Crear result fallido
/// return Result.Failure("User not found");
///
/// // Crear result fallido con múltiples errores
/// return Result.Failure(new[] { "Email required", "Password too short" });
///
/// // Verificar resultado
/// if (result.IsSuccess)
/// {
///     var data = result.Value;
///     // Procesar data...
/// }
/// else
/// {
///     var error = result.Error;
///     // Manejar error...
/// }
///
/// COMPOSICIÓN DE RESULTADOS:
///
/// Result Pattern facilita encadenar operaciones:
///
/// public async Task<Result<Order>> PlaceOrderAsync(int userId, List<Item> items)
/// {
///     var userResult = await GetUserAsync(userId);
///     if (userResult.IsFailure)
///         return Result.Failure<Order>(userResult.Error);
///
///     var validationResult = ValidateItems(items);
///     if (validationResult.IsFailure)
///         return Result.Failure<Order>(validationResult.Error);
///
///     var order = CreateOrder(userResult.Value, items);
///     return Result.Success(order);
/// }
///
/// RESULT con RAILWAY ORIENTED PROGRAMMING:
///
/// Podemos agregar métodos de extensión para composición fluida:
///
/// return await GetUserAsync(userId)
///     .Bind(user => ValidateUser(user))
///     .Bind(user => CreateOrder(user))
///     .Map(order => MapToDto(order));
///
/// Pero por simplicidad, en este proyecto usamos Result básico.
///
/// RESULT vs OPTION/MAYBE:
///
/// - Result: Indica éxito/fallo con mensaje de error
/// - Option/Maybe: Indica presencia/ausencia de valor
///
/// Option<User> user = await GetUserAsync(id);
/// if (user.IsSome)
///     DoSomething(user.Value);
///
/// Option es más limitado, solo para presencia/ausencia.
/// Result es más completo para manejo de errores.
///
/// SERIALIZACIÓN:
///
/// Result NO debe serializarse a JSON para enviar al cliente.
/// En el controller, extraer el valor o error:
///
/// var result = await _mediator.Send(command);
/// if (result.IsFailure)
///     return BadRequest(new { error = result.Error });
///
/// return Ok(result.Value);
/// </remarks>
public class Result
{
    /// <summary>
    /// Indica si la operación fue exitosa.
    /// </summary>
    /// <remarks>
    /// true = Operación completada exitosamente
    /// false = Operación falló
    ///
    /// Mutuamente exclusivo con IsFailure:
    /// - Si IsSuccess = true, entonces IsFailure = false
    /// - Si IsSuccess = false, entonces IsFailure = true
    /// </remarks>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indica si la operación falló.
    /// </summary>
    /// <remarks>
    /// Conveniente para chequeos más legibles:
    /// if (result.IsFailure) return BadRequest();
    ///
    /// Es simplemente !IsSuccess, pero más expresivo.
    /// </remarks>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Mensaje de error si la operación falló.
    /// </summary>
    /// <remarks>
    /// Solo tiene valor si IsSuccess = false.
    /// Si IsSuccess = true, este valor es string.Empty.
    ///
    /// Puede contener:
    /// - Mensaje simple: "User not found"
    /// - Múltiples errores: "Email required. Password too short."
    /// - Mensaje i18n key: "errors.user.not_found" (para internacionalización)
    ///
    /// IMPORTANTE: No incluir información sensible en el error.
    /// El error puede ser mostrado al usuario final.
    ///
    /// Bueno: "Invalid credentials"
    /// Malo: "Password abc123 is incorrect" (expone password)
    /// Malo: "SQL Error: column 'password_hash'..." (expone estructura DB)
    /// </remarks>
    public string Error { get; }

    /// <summary>
    /// Constructor protegido para forzar uso de factory methods.
    /// </summary>
    /// <remarks>
    /// private/protected para evitar: new Result(true, "error")
    /// Forzamos uso de: Result.Success() o Result.Failure()
    ///
    /// Garantiza que:
    /// - Success siempre tiene Error = string.Empty
    /// - Failure siempre tiene Error con valor
    /// </remarks>
    protected Result(bool isSuccess, string error)
    {
        // Validar invariante: Si es éxito, no debe haber error
        if (isSuccess && !string.IsNullOrEmpty(error))
        {
            throw new ArgumentException("Success result cannot have an error message", nameof(error));
        }

        // Validar invariante: Si es fallo, debe haber error
        if (!isSuccess && string.IsNullOrEmpty(error))
        {
            throw new ArgumentException("Failure result must have an error message", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error ?? string.Empty;
    }

    /// <summary>
    /// Crea un resultado exitoso sin valor de retorno.
    /// </summary>
    /// <returns>Result indicando éxito.</returns>
    /// <remarks>
    /// Usar para operaciones que no retornan valor:
    /// - Delete operations
    /// - Update operations que no retornan el objeto
    /// - Void operations
    ///
    /// Ejemplo:
    /// public async Task<Result> DeleteTaskAsync(Guid taskId)
    /// {
    ///     var task = await _context.Tasks.FindAsync(taskId);
    ///     if (task == null)
    ///         return Result.Failure("Task not found");
    ///
    ///     _context.Tasks.Remove(task);
    ///     await _context.SaveChangesAsync();
    ///
    ///     return Result.Success(); // Sin valor de retorno
    /// }
    /// </remarks>
    public static Result Success()
    {
        return new Result(true, string.Empty);
    }

    /// <summary>
    /// Crea un resultado fallido con un mensaje de error.
    /// </summary>
    /// <param name="error">Mensaje de error describiendo la falla.</param>
    /// <returns>Result indicando fallo.</returns>
    /// <remarks>
    /// Ejemplo:
    /// if (user == null)
    ///     return Result.Failure("User not found");
    ///
    /// if (!isValidPassword)
    ///     return Result.Failure("Invalid password format");
    ///
    /// MENSAJES DE ERROR:
    /// - Deben ser descriptivos pero no demasiado técnicos
    /// - Pueden mostrarse al usuario
    /// - No incluir información sensible
    /// - Preferir mensajes genéricos para seguridad cuando sea necesario
    ///
    /// Seguridad:
    /// ✅ "Invalid credentials" (genérico, no revela si email existe)
    /// ❌ "Email not found" (revela si email está registrado)
    /// ❌ "Incorrect password" (revela que email existe)
    /// </remarks>
    public static Result Failure(string error)
    {
        return new Result(false, error);
    }
}

/// <summary>
/// Representa el resultado de una operación que retorna un valor.
/// </summary>
/// <typeparam name="T">Tipo del valor de retorno.</typeparam>
/// <remarks>
/// Result&lt;T&gt; extiende Result para incluir un valor en caso de éxito.
///
/// DIFERENCIA CON Result:
/// - Result: Para operaciones void (delete, update sin retorno)
/// - Result&lt;T&gt;: Para operaciones que retornan valor (get, create)
///
/// EJEMPLOS:
///
/// // Retornar un usuario
/// public async Task<Result<User>> GetUserAsync(Guid id)
/// {
///     var user = await _context.Users.FindAsync(id);
///     if (user == null)
///         return Result.Failure<User>("User not found");
///
///     return Result.Success(user);
/// }
///
/// // Retornar una lista paginada
/// public async Task<Result<PaginatedList<TaskDto>>> GetTasksAsync(int page, int size)
/// {
///     var tasks = await _context.Tasks
///         .Skip((page - 1) * size)
///         .Take(size)
///         .ToListAsync();
///
///     var paginatedList = new PaginatedList<TaskDto>(tasks, totalCount, page, size);
///     return Result.Success(paginatedList);
/// }
///
/// // Uso en controller
/// var result = await _mediator.Send(query);
/// if (result.IsFailure)
///     return NotFound(result.Error);
///
/// return Ok(result.Value); // Retornar el valor al cliente
///
/// TYPE SAFETY:
///
/// El compilador garantiza que solo accedas a Value si verificaste IsSuccess:
///
/// if (result.IsSuccess)
/// {
///     var user = result.Value; // Seguro, sabemos que Value tiene valor
/// }
///
/// Intentar acceder a Value sin verificar lanzará excepción en tiempo de ejecución.
///
/// CONVERSIÓN IMPLÍCITA:
///
/// Result&lt;T&gt; puede convertirse implícitamente a Result:
/// public async Task<Result> DoSomethingAsync()
/// {
///     var result = await GetUserAsync(id); // Result<User>
///     if (result.IsFailure)
///         return result; // Conversión implícita a Result
///
///     // Hacer algo con result.Value...
///     return Result.Success();
/// }
/// </remarks>
public class Result<T> : Result
{
    /// <summary>
    /// Valor de retorno si la operación fue exitosa.
    /// </summary>
    /// <remarks>
    /// Solo debe accederse si IsSuccess = true.
    /// Si IsSuccess = false, Value será default(T) (null para clases, 0 para int, etc.)
    ///
    /// IMPORTANTE: Verificar siempre IsSuccess antes de acceder a Value.
    ///
    /// Correcto:
    /// if (result.IsSuccess)
    /// {
    ///     var value = result.Value; // Seguro
    /// }
    ///
    /// Incorrecto:
    /// var value = result.Value; // Puede ser null si IsFailure = true
    /// value.DoSomething(); // NullReferenceException!
    ///
    /// ALTERNATIVA - Exception on Access:
    /// Algunas implementaciones lanzan excepción si accedes Value cuando IsFailure:
    ///
    /// public T Value
    /// {
    ///     get
    ///     {
    ///         if (IsFailure)
    ///             throw new InvalidOperationException("Cannot access Value of failed result");
    ///         return _value;
    ///     }
    /// }
    ///
    /// Esto hace que el acceso sea fail-fast, pero va contra la filosofía
    /// de "no excepciones". Por eso lo dejamos sin validación.
    /// </remarks>
    public T Value { get; }

    /// <summary>
    /// Constructor protegido para forzar uso de factory methods.
    /// </summary>
    protected Result(T value, bool isSuccess, string error) : base(isSuccess, error)
    {
        Value = value;
    }

    /// <summary>
    /// Crea un resultado exitoso con un valor.
    /// </summary>
    /// <param name="value">Valor de retorno.</param>
    /// <returns>Result&lt;T&gt; indicando éxito con el valor.</returns>
    /// <remarks>
    /// Ejemplo:
    /// var user = await _context.Users.FindAsync(id);
    /// return Result.Success(user);
    ///
    /// var tasks = await GetTasksAsync();
    /// return Result.Success(tasks);
    ///
    /// VALIDACIÓN:
    /// El valor puede ser null. Si necesitas garantizar non-null, valida antes:
    /// if (user == null)
    ///     return Result.Failure<User>("User not found");
    /// return Result.Success(user); // Aquí garantizamos non-null
    /// </remarks>
    public static Result<T> Success(T value)
    {
        return new Result<T>(value, true, string.Empty);
    }

    /// <summary>
    /// Crea un resultado fallido con un mensaje de error.
    /// </summary>
    /// <param name="error">Mensaje de error.</param>
    /// <returns>Result&lt;T&gt; indicando fallo.</returns>
    /// <remarks>
    /// Ejemplo:
    /// if (task == null)
    ///     return Result.Failure<TaskDto>("Task not found");
    ///
    /// if (!isValid)
    ///     return Result.Failure<TaskDto>("Invalid task data");
    ///
    /// VALOR EN FALLO:
    /// Value será default(T):
    /// - Clases: null
    /// - Structs: valores por defecto (0, false, etc.)
    /// - Nullable<T>: null
    ///
    /// No debes acceder a Value si IsFailure = true.
    /// </remarks>
    public static Result<T> Failure(string error)
    {
        return new Result<T>(default!, false, error);
    }

}
