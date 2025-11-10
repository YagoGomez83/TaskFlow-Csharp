using TaskManagement.Domain.Enums;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.DTOs.Tasks;

/// <summary>
/// DTO para actualizar una tarea existente.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE UpdateTaskRequest:
///
/// Este DTO contiene los datos para actualizar una tarea existente.
/// Se usa en: PUT /api/tasks/{id}
///
/// PUT vs PATCH:
///
/// PUT (este proyecto):
/// - Reemplaza el recurso completo
/// - Cliente debe enviar TODOS los campos (incluso si no cambian)
/// - Idempotente: Múltiples PUTs idénticos = mismo resultado
/// - Más simple de implementar
///
/// PATCH (alternativa):
/// - Actualización parcial
/// - Cliente solo envía campos que cambian
/// - Más complejo (JSON Patch RFC 6902)
/// - Más eficiente en ancho de banda
///
/// Para simplicidad, este proyecto usa PUT.
///
/// CAMPOS ACTUALIZABLES:
/// - Title: Título de la tarea
/// - Description: Descripción
/// - DueDate: Fecha límite
/// - Priority: Prioridad
/// - Status: Estado (Pending/InProgress/Completed)
///
/// CAMPOS NO ACTUALIZABLES:
/// - Id: Inmutable (identificador único)
/// - UserId: No se puede transferir tarea a otro usuario
/// - CreatedAt: Fecha de creación es inmutable
/// - UpdatedAt: Se actualiza automáticamente
///
/// Si necesitas transferir tarea a otro usuario, crear endpoint separado:
/// POST /api/tasks/{id}/transfer
/// { "newUserId": "guid" }
///
/// FLUJO COMPLETO:
///
/// 1. Cliente obtiene tarea actual:
///    GET /api/tasks/a1b2c3d4-e5f6-7890-abcd-ef1234567890
///    Response: {
///      "id": "a1b2c3d4-...",
///      "title": "Tarea original",
///      "description": "Descripción original",
///      "dueDate": null,
///      "priority": "Medium",
///      "status": "Pending"
///    }
///
/// 2. Cliente modifica campos y envía PUT:
///    PUT /api/tasks/a1b2c3d4-e5f6-7890-abcd-ef1234567890
///    Headers: { Authorization: "Bearer {token}" }
///    Body: {
///      "title": "Tarea actualizada",  // ← Modificado
///      "description": "Nueva descripción",  // ← Modificado
///      "dueDate": "2024-01-20T23:59:59Z",  // ← Agregado
///      "priority": "High",  // ← Modificado
///      "status": "InProgress"  // ← Modificado
///    }
///
/// 3. Controller valida:
///    - Usuario autenticado (token válido)
///    - Tarea existe
///    - Usuario es dueño de la tarea (o es Admin)
///
/// 4. ValidationBehavior valida:
///    - Title no vacío, <= 200 caracteres
///    - Description <= 2000 caracteres
///    - DueDate en el futuro (si existe)
///    - Priority valor válido
///    - Status valor válido
///
/// 5. Handler ejecuta:
///    var task = await _context.Tasks.FindAsync(request.TaskId);
///
///    if (task.UserId != _currentUser.UserId && !_currentUser.IsInRole("Admin"))
///        return Result.Failure("You don't have permission to update this task");
///
///    task.UpdateTitle(request.Title);
///    task.UpdateDescription(request.Description);
///    task.UpdateDueDate(request.DueDate);
///    task.UpdatePriority(request.Priority);
///    task.UpdateStatus(request.Status);
///
///    await _context.SaveChangesAsync();
///
/// 6. Response:
///    Status: 200 OK
///    Body: TaskDto (tarea actualizada)
///
/// AUTORIZACIÓN:
///
/// Solo el dueño de la tarea (o Admin) puede actualizarla:
///
/// public async Task<Result> Handle(UpdateTaskCommand request, CancellationToken ct)
/// {
///     var task = await _context.Tasks.FindAsync(request.TaskId);
///
///     if (task == null)
///         return Result.Failure("Task not found");
///
///     // IMPORTANTE: Verificar ownership
///     if (task.UserId != _currentUser.UserId && !_currentUser.IsInRole("Admin"))
///         return Result.Failure("You don't have permission to update this task");
///
///     // Actualizar campos...
/// }
///
/// Sin esto, cualquier usuario podría modificar tareas de otros.
///
/// VALIDACIÓN:
///
/// public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
/// {
///     public UpdateTaskCommandValidator()
///     {
///         RuleFor(x => x.TaskId)
///             .NotEmpty()
///             .WithMessage("Task ID is required");
///
///         RuleFor(x => x.Title)
///             .NotEmpty()
///             .WithMessage("Title is required")
///             .MaximumLength(200)
///             .WithMessage("Title must not exceed 200 characters");
///
///         RuleFor(x => x.Description)
///             .MaximumLength(2000)
///             .WithMessage("Description must not exceed 2000 characters")
///             .When(x => !string.IsNullOrEmpty(x.Description));
///
///         RuleFor(x => x.DueDate)
///             .GreaterThan(DateTime.UtcNow)
///             .WithMessage("Due date must be in the future")
///             .When(x => x.DueDate.HasValue);
///
///         RuleFor(x => x.Priority)
///             .IsInEnum()
///             .WithMessage("Priority must be a valid value");
///
///         RuleFor(x => x.Status)
///             .IsInEnum()
///             .WithMessage("Status must be a valid value");
///     }
/// }
///
/// IDEMPOTENCIA:
///
/// PUT es idempotente: Múltiples requests idénticos tienen el mismo efecto.
///
/// PUT /api/tasks/{id} { "title": "Tarea" }
/// PUT /api/tasks/{id} { "title": "Tarea" }  // Mismo resultado
/// PUT /api/tasks/{id} { "title": "Tarea" }  // Mismo resultado
///
/// Esto es útil para retry logic si hay fallos de red.
///
/// OPTIMISTIC CONCURRENCY (Opcional):
///
/// Si dos usuarios actualizan la misma tarea simultáneamente:
///
/// Usuario A obtiene tarea:
/// { "id": "...", "title": "Original", "version": 1 }
///
/// Usuario B obtiene tarea:
/// { "id": "...", "title": "Original", "version": 1 }
///
/// Usuario A actualiza:
/// PUT /api/tasks/{id} { "title": "Version A", "version": 1 }
/// → Success, version = 2
///
/// Usuario B actualiza:
/// PUT /api/tasks/{id} { "title": "Version B", "version": 1 }
/// → Error: "Task was modified by another user" (version mismatch)
///
/// Implementación con RowVersion:
/// public class TaskItem
/// {
///     [Timestamp]
///     public byte[] RowVersion { get; set; }
/// }
///
/// try
/// {
///     await _context.SaveChangesAsync();
/// }
/// catch (DbUpdateConcurrencyException)
/// {
///     return Result.Failure("Task was modified by another user. Please refresh and try again.");
/// }
///
/// Para MVP, no implementamos concurrency control (fuera de scope).
///
/// STATUS TRANSITIONS:
///
/// Algunas aplicaciones validan transiciones de estado:
///
/// ✅ Válido:
/// Pending → InProgress
/// InProgress → Completed
/// Completed → Pending (reabrir)
///
/// ❌ Inválido:
/// Pending → Completed (debe pasar por InProgress primero)
///
/// Implementar en TaskItem:
/// public void UpdateStatus(TaskStatus newStatus)
/// {
///     if (Status == TaskStatus.Pending && newStatus == TaskStatus.Completed)
///         throw new DomainException("Cannot mark task as completed without starting it first");
///
///     Status = newStatus;
///     UpdatedAt = DateTime.UtcNow;
/// }
///
/// Para MVP, permitimos cualquier transición (más flexible).
///
/// FRONTEND EXAMPLE:
///
/// interface UpdateTaskRequest {
///   title: string;
///   description?: string;
///   dueDate?: string;
///   priority: 'Low' | 'Medium' | 'High';
///   status: 'Pending' | 'InProgress' | 'Completed';
/// }
///
/// async function updateTask(id: string, request: UpdateTaskRequest): Promise<TaskDto> {
///   const accessToken = localStorage.getItem('accessToken');
///
///   const response = await fetch(`/api/tasks/${id}`, {
///     method: 'PUT',
///     headers: {
///       'Content-Type': 'application/json',
///       'Authorization': `Bearer ${accessToken}`
///     },
///     body: JSON.stringify(request)
///   });
///
///   if (!response.ok) {
///     const error = await response.json();
///     throw new Error(error.error);
///   }
///
///   return response.json();
/// }
///
/// // Uso
/// function EditTaskForm({ taskId }: { taskId: string }) {
///   const [task, setTask] = useState<TaskDto | null>(null);
///
///   useEffect(() => {
///     // Obtener tarea actual
///     fetch(`/api/tasks/${taskId}`)
///       .then(res => res.json())
///       .then(setTask);
///   }, [taskId]);
///
///   const handleSubmit = async (e: React.FormEvent) => {
///     e.preventDefault();
///
///     if (!task) return;
///
///     try {
///       const updated = await updateTask(taskId, {
///         title: task.title,
///         description: task.description,
///         dueDate: task.dueDate,
///         priority: task.priority,
///         status: task.status
///       });
///
///       console.log('Task updated:', updated);
///       navigate('/tasks');
///     } catch (error) {
///       console.error('Update failed:', error);
///     }
///   };
///
///   if (!task) return <div>Loading...</div>;
///
///   return (
///     <form onSubmit={handleSubmit}>
///       <input
///         value={task.title}
///         onChange={(e) => setTask({ ...task, title: e.target.value })}
///       />
///
///       <select
///         value={task.status}
///         onChange={(e) => setTask({ ...task, status: e.target.value as TaskStatus })}
///       >
///         <option value="Pending">Pending</option>
///         <option value="InProgress">In Progress</option>
///         <option value="Completed">Completed</option>
///       </select>
///
///       <button type="submit">Update Task</button>
///     </form>
///   );
/// }
///
/// TESTING:
///
/// [Fact]
/// public async Task UpdateTask_ValidRequest_UpdatesTask()
/// {
///     // Arrange
///     var task = TaskItem.Create("Original Title", null, userId);
///     _context.Tasks.Add(task);
///     await _context.SaveChangesAsync();
///
///     var request = new UpdateTaskRequest
///     {
///         Title = "Updated Title",
///         Description = "New description",
///         Priority = TaskPriority.High,
///         Status = TaskStatus.InProgress
///     };
///
///     // Act
///     var response = await _authenticatedClient.PutAsJsonAsync($"/api/tasks/{task.Id}", request);
///
///     // Assert
///     response.EnsureSuccessStatusCode();
///
///     var dto = await response.Content.ReadFromJsonAsync<TaskDto>();
///     Assert.Equal("Updated Title", dto.Title);
///     Assert.Equal("New description", dto.Description);
///     Assert.Equal(TaskPriority.High, dto.Priority);
///     Assert.Equal(TaskStatus.InProgress, dto.Status);
///
///     // Verificar en BD
///     var updatedTask = await _context.Tasks.FindAsync(task.Id);
///     Assert.Equal("Updated Title", updatedTask.Title);
/// }
///
/// [Fact]
/// public async Task UpdateTask_NotOwner_ReturnsForbidden()
/// {
///     // Arrange
///     var otherUserId = Guid.NewGuid();
///     var task = TaskItem.Create("Task", null, otherUserId);  // Otro usuario
///     _context.Tasks.Add(task);
///     await _context.SaveChangesAsync();
///
///     var request = new UpdateTaskRequest { Title = "Hacked", ... };
///
///     // Act
///     var response = await _authenticatedClient.PutAsJsonAsync($"/api/tasks/{task.Id}", request);
///
///     // Assert
///     Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
/// }
/// </remarks>
public class UpdateTaskRequest
{
    /// <summary>
    /// Título de la tarea (requerido).
    /// </summary>
    /// <example>Completar informe trimestral (actualizado)</example>
    /// <remarks>
    /// - Requerido
    /// - Máximo 200 caracteres
    ///
    /// Validación:
    /// - NotEmpty
    /// - MaximumLength(200)
    /// </remarks>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción detallada de la tarea (opcional).
    /// </summary>
    /// <example>Informe actualizado con datos del Q1 2024</example>
    /// <remarks>
    /// - Opcional (puede ser null o vacío)
    /// - Máximo 2000 caracteres
    ///
    /// Validación:
    /// - MaximumLength(2000)
    ///
    /// Para limpiar descripción: enviar string vacío o null
    /// </remarks>
    public string? Description { get; set; }

    /// <summary>
    /// Fecha y hora límite para completar la tarea (opcional).
    /// </summary>
    /// <example>2024-01-25T23:59:59Z</example>
    /// <remarks>
    /// - Opcional (puede ser null)
    /// - Debe ser en el futuro
    /// - Formato ISO 8601
    ///
    /// Validación:
    /// - GreaterThan(DateTime.UtcNow) cuando no es null
    ///
    /// Para limpiar due date: enviar null
    /// </remarks>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Prioridad de la tarea.
    /// </summary>
    /// <example>High</example>
    /// <remarks>
    /// - Requerido
    /// - Valores: Low (0), Medium (1), High (2)
    ///
    /// Validación:
    /// - IsInEnum
    ///
    /// Se serializa como string: "Low", "Medium", "High"
    /// </remarks>
    public TaskPriority Priority { get; set; }

    /// <summary>
    /// Estado actual de la tarea.
    /// </summary>
    /// <example>InProgress</example>
    /// <remarks>
    /// - Requerido
    /// - Valores: Pending (0), InProgress (1), Completed (2)
    ///
    /// Validación:
    /// - IsInEnum
    ///
    /// Transiciones comunes:
    /// - Pending → InProgress (empezar tarea)
    /// - InProgress → Completed (completar tarea)
    /// - Completed → Pending (reabrir tarea)
    /// - InProgress → Pending (pausar tarea)
    ///
    /// Todas las transiciones son permitidas (flexible).
    ///
    /// Para MVP, no validamos workflow de estados.
    /// Si necesitas validación estricta, implementar en dominio.
    /// </remarks>
    public TaskStatus Status { get; set; }
}
