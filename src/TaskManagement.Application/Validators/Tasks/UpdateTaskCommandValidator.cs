using FluentValidation;
using TaskManagement.Application.UseCases.Tasks.Commands;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Validators.Tasks;

/// <summary>
/// Validator para UpdateTaskCommand.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE UPDATE VALIDATION:
///
/// UpdateTaskCommand valida datos para actualizar tarea existente:
/// - TaskId: requerido, debe ser Guid válido
/// - Title: requerido, longitud
/// - Description: opcional, longitud
/// - DueDate: opcional, debe ser futura
/// - Priority: valor enum válido
/// - Status: valor enum válido
///
/// DIFERENCIA CON CREATE:
///
/// CreateTaskCommand:
/// - No tiene TaskId (se genera en handler)
/// - Status siempre es Pending (no se valida)
/// - UserId viene del token
///
/// UpdateTaskCommand:
/// - Requiere TaskId (para saber qué tarea actualizar)
/// - Status es actualizable (Pending → InProgress → Completed)
/// - UserId NO se puede cambiar (ownership no transferible)
///
/// VALIDACIÓN DE GUID:
///
/// TaskId es Guid en C#.
/// Si cliente envía JSON con Guid inválido:
///
/// { "taskId": "invalid-guid", "title": "..." }
///
/// System.Text.Json falla en deserialización antes de llegar a validator.
/// Response: 400 Bad Request "The JSON value could not be converted to System.Guid"
///
/// Si deserialización pasa:
/// RuleFor(x => x.TaskId).NotEmpty();
///
/// Valida que Guid no sea Guid.Empty (00000000-0000-0000-0000-000000000000).
///
/// Guid.Empty es valor por defecto de Guid:
/// var command = new UpdateTaskCommand();  // TaskId = Guid.Empty
///
/// NotEmpty() detecta esto y rechaza.
///
/// VALIDACIÓN DE STATUS:
///
/// Status es actualizable en UpdateTask:
///
/// RuleFor(x => x.Status)
///     .IsInEnum()
///     .WithMessage("Status must be a valid value (Pending, InProgress, Completed)");
///
/// Permite cualquier transición:
/// - Pending → InProgress ✅
/// - InProgress → Completed ✅
/// - Completed → Pending ✅ (reabrir tarea)
/// - InProgress → Pending ✅ (pausar tarea)
/// - Pending → Completed ✅ (marcar directo como completa)
///
/// VALIDACIÓN ESTRICTA DE WORKFLOW (opcional):
///
/// Si requisito de negocio requiere workflow estricto:
///
/// public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
/// {
///     public UpdateTaskCommandValidator()
///     {
///         // ... otras validaciones
///
///         RuleFor(x => x.Status)
///             .IsInEnum()
///             .Must((command, status) => BeValidStatusTransition(command.CurrentStatus, status))
///             .WithMessage("Invalid status transition");
///     }
///
///     private bool BeValidStatusTransition(TaskStatus from, TaskStatus to)
///     {
///         return (from, to) switch
///         {
///             (TaskStatus.Pending, TaskStatus.InProgress) => true,
///             (TaskStatus.InProgress, TaskStatus.Completed) => true,
///             (TaskStatus.Completed, TaskStatus.Pending) => true,  // Reabrir
///             (TaskStatus.InProgress, TaskStatus.Pending) => true,  // Pausar
///             _ when from == to => true,  // Sin cambio
///             _ => false  // Todas las demás transiciones inválidas
///         };
///     }
/// }
///
/// Problema: Necesitas CurrentStatus en el command.
/// Pero UpdateTaskRequest no tiene CurrentStatus (solo nuevo Status).
///
/// Solución: Validar en handler, no en validator.
///
/// public async Task<Result> Handle(UpdateTaskCommand request, CancellationToken ct)
/// {
///     var task = await _context.Tasks.FindAsync(request.TaskId);
///
///     // Validar transición
///     if (!IsValidTransition(task.Status, request.Status))
///         return Result.Failure($"Cannot transition from {task.Status} to {request.Status}");
///
///     task.UpdateStatus(request.Status);
///     // ...
/// }
///
/// Para MVP: Permitir cualquier transición (más flexible).
/// Si negocio requiere workflow estricto, validar en domain entity.
///
/// PUT vs PATCH:
///
/// Este proyecto usa PUT (reemplazar recurso completo).
/// Cliente debe enviar TODOS los campos, incluso si no cambian.
///
/// PUT /api/tasks/123
/// {
///   "title": "Updated Title",
///   "description": "Same description",  // ← Debe incluirse
///   "dueDate": null,  // ← Debe incluirse
///   "priority": "Medium",  // ← Debe incluirse
///   "status": "InProgress"
/// }
///
/// Si usáramos PATCH (actualización parcial):
/// PATCH /api/tasks/123
/// {
///   "title": "Updated Title",  // Solo lo que cambió
///   "status": "InProgress"
/// }
///
/// PATCH es más eficiente en bandwidth pero más complejo.
/// Requiere JSON Patch (RFC 6902):
/// [
///   { "op": "replace", "path": "/title", "value": "New Title" },
///   { "op": "replace", "path": "/status", "value": "InProgress" }
/// ]
///
/// Para simplicidad, usamos PUT.
///
/// CAMPOS NO ACTUALIZABLES:
///
/// UserId NO se valida ni actualiza:
/// - Ownership no es transferible
/// - Solo dueño puede actualizar (validado en handler)
///
/// Si necesitas transferir tarea, crear endpoint separado:
/// POST /api/tasks/{id}/transfer
/// { "newUserId": "guid" }
///
/// Con validación de permisos (solo Admin puede transferir).
///
/// AUTORIZACIÓN EN HANDLER:
///
/// Validator NO valida ownership (no tiene contexto de usuario autenticado).
/// Handler valida:
///
/// public async Task<Result> Handle(UpdateTaskCommand request, CancellationToken ct)
/// {
///     var task = await _context.Tasks.FindAsync(request.TaskId);
///
///     if (task == null)
///         return Result.Failure("Task not found");
///
///     // IMPORTANTE: Validar ownership
///     if (task.UserId != _currentUser.UserId && !_currentUser.IsInRole("Admin"))
///         return Result.Failure("You don't have permission to update this task");
///
///     // Actualizar campos...
/// }
///
/// Sin esto, cualquier usuario autenticado podría modificar cualquier tarea.
///
/// OPTIMISTIC CONCURRENCY (opcional):
///
/// Para prevenir conflictos de actualización concurrente:
///
/// public class TaskItem
/// {
///     [Timestamp]
///     public byte[] RowVersion { get; set; }
/// }
///
/// public class UpdateTaskRequest
/// {
///     public byte[] RowVersion { get; set; }  // Incluir en request
/// }
///
/// Handler:
/// try
/// {
///     await _context.SaveChangesAsync();
/// }
/// catch (DbUpdateConcurrencyException)
/// {
///     return Result.Failure("Task was modified by another user. Please refresh and try again.");
/// }
///
/// Para MVP, no implementamos concurrency control.
/// Para aplicaciones colaborativas, considerar agregar.
///
/// TESTING:
///
/// [Fact]
/// public void Validate_EmptyTaskId_ReturnsError()
/// {
///     var validator = new UpdateTaskCommandValidator();
///     var command = new UpdateTaskCommand
///     {
///         TaskId = Guid.Empty,  // ← Inválido
///         Title = "Valid Title",
///         Priority = TaskPriority.Medium,
///         Status = TaskStatus.Pending
///     };
///
///     var result = validator.Validate(command);
///
///     Assert.False(result.IsValid);
///     Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTaskCommand.TaskId));
/// }
///
/// [Fact]
/// public void Validate_EmptyTitle_ReturnsError()
/// {
///     var validator = new UpdateTaskCommandValidator();
///     var command = new UpdateTaskCommand
///     {
///         TaskId = Guid.NewGuid(),
///         Title = "",  // ← Inválido
///         Priority = TaskPriority.Medium,
///         Status = TaskStatus.Pending
///     };
///
///     var result = validator.Validate(command);
///
///     Assert.False(result.IsValid);
/// }
///
/// [Fact]
/// public void Validate_InvalidStatus_ReturnsError()
/// {
///     var validator = new UpdateTaskCommandValidator();
///     var command = new UpdateTaskCommand
///     {
///         TaskId = Guid.NewGuid(),
///         Title = "Valid",
///         Priority = TaskPriority.Medium,
///         Status = (TaskStatus)99  // ← Inválido
///     };
///
///     var result = validator.Validate(command);
///
///     Assert.False(result.IsValid);
/// }
///
/// [Fact]
/// public void Validate_ValidCommand_Passes()
/// {
///     var validator = new UpdateTaskCommandValidator();
///     var command = new UpdateTaskCommand
///     {
///         TaskId = Guid.NewGuid(),
///         Title = "Updated Task",
///         Description = "Updated description",
///         DueDate = DateTime.UtcNow.AddDays(7),
///         Priority = TaskPriority.High,
///         Status = TaskStatus.InProgress
///     };
///
///     var result = validator.Validate(command);
///
///     Assert.True(result.IsValid);
/// }
/// </remarks>
public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    /// <summary>
    /// Constructor que define reglas de validación.
    /// </summary>
    public UpdateTaskCommandValidator()
    {
        // Validación para TaskId (requerido, identificador de tarea a actualizar)
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("Task ID is required");
        // NotEmpty para Guid: verifica que no sea Guid.Empty
        // Guid.Empty = 00000000-0000-0000-0000-000000000000

        // Validaciones para Title (requerido)
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(200)
            .WithMessage("Title must not exceed 200 characters");

        // Validaciones para Description (opcional)
        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description must not exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
        // Mismo que CreateTask: validar solo si tiene valor

        // Validaciones para DueDate (opcional)
        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Due date must be in the future")
            .When(x => x.DueDate.HasValue);
        // Permitir null (limpiar due date)
        // Si tiene valor, debe ser futuro

        // Validación para Priority (requerido, enum)
        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Priority must be a valid value (Low, Medium, High)");

        // Validación para Status (requerido, enum)
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status must be a valid value (Pending, InProgress, Completed)");
        // A diferencia de CreateTask, aquí SÍ validamos Status
        // Porque es actualizable (cambiar estado de tarea)
        // Permitimos cualquier transición (sin validación de workflow)
    }
}
