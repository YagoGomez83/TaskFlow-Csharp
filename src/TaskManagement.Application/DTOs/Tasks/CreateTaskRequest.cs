using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Tasks;

/// <summary>
/// DTO para crear una nueva tarea.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE CreateTaskRequest:
///
/// Este DTO contiene los datos necesarios para crear una nueva tarea.
/// Se usa en: POST /api/tasks
///
/// CAMPOS REQUERIDOS:
/// - Title: Título de la tarea (requerido)
///
/// CAMPOS OPCIONALES:
/// - Description: Descripción detallada
/// - DueDate: Fecha límite
/// - Priority: Prioridad (default: Medium si no se especifica)
///
/// CAMPOS NO INCLUIDOS (se asignan automáticamente):
/// - Id: Generado como Guid.NewGuid()
/// - Status: Siempre Pending para nuevas tareas
/// - UserId: Extraído del token JWT (ICurrentUserService)
/// - CreatedAt: DateTime.UtcNow
/// - UpdatedAt: DateTime.UtcNow
///
/// IMPORTANTE - SEGURIDAD:
///
/// ❌ NO permitir que cliente especifique UserId:
/// public class CreateTaskRequest
/// {
///     public Guid UserId { get; set; }  // ← INSEGURO!
/// }
///
/// Atacante podría:
/// POST /api/tasks
/// {
///   "userId": "otro-usuario-guid",
///   "title": "Tarea maliciosa"
/// }
///
/// Y crear tareas para otros usuarios.
///
/// ✅ SEGURO - UserId del token:
/// public async Task<Result<TaskDto>> Handle(CreateTaskCommand request, CancellationToken ct)
/// {
///     var userId = _currentUser.UserId;  // Del token JWT
///     var task = TaskItem.Create(request.Title, request.Description, userId, ...);
/// }
///
/// UserId viene del token (firmado, no puede falsificarse).
///
/// FLUJO COMPLETO:
///
/// 1. Cliente envía request:
///    POST /api/tasks
///    Headers: { Authorization: "Bearer {token}" }
///    Body: {
///      "title": "Completar informe",
///      "description": "Informe trimestral Q1 2024",
///      "dueDate": "2024-01-20T23:59:59Z",
///      "priority": "High"
///    }
///
/// 2. Middleware valida token JWT y extrae userId
///
/// 3. Controller recibe request y crea Command:
///    var command = new CreateTaskCommand
///    {
///        Title = request.Title,
///        Description = request.Description,
///        DueDate = request.DueDate,
///        Priority = request.Priority
///    };
///    var result = await _mediator.Send(command);
///
/// 4. ValidationBehavior valida command:
///    - Title no vacío, <= 200 caracteres
///    - Description <= 2000 caracteres (si existe)
///    - DueDate en el futuro (si existe)
///    - Priority es valor válido
///
/// 5. Handler ejecuta lógica:
///    var userId = _currentUser.UserId;  // Del token
///    var task = TaskItem.Create(command.Title, command.Description, userId, ...);
///    _context.Tasks.Add(task);
///    await _context.SaveChangesAsync();
///
/// 6. Mapear a DTO y retornar:
///    var dto = _mapper.Map<TaskDto>(task);
///    return Result.Success(dto);
///
/// 7. Controller retorna response:
///    return Created($"/api/tasks/{dto.Id}", dto);
///
/// 8. Cliente recibe:
///    Status: 201 Created
///    Location: /api/tasks/a1b2c3d4-e5f6-7890-abcd-ef1234567890
///    Body: {
///      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
///      "title": "Completar informe",
///      "description": "Informe trimestral Q1 2024",
///      "dueDate": "2024-01-20T23:59:59Z",
///      "priority": "High",
///      "status": "Pending",
///      "userId": "user-guid",
///      "createdAt": "2024-01-15T10:30:00Z",
///      "updatedAt": "2024-01-15T10:30:00Z"
///    }
///
/// VALIDACIÓN:
///
/// public class CreateTaskRequestValidator : AbstractValidator<CreateTaskCommand>
/// {
///     public CreateTaskRequestValidator()
///     {
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
///             .WithMessage("Priority must be a valid value (Low, Medium, High)");
///     }
/// }
///
/// DEFAULTS:
///
/// Si cliente no especifica Priority:
/// POST /api/tasks
/// {
///   "title": "Task sin prioridad"
/// }
///
/// Se usa default:
/// public TaskPriority Priority { get; set; } = TaskPriority.Medium;
///
/// O en el handler:
/// var priority = request.Priority ?? TaskPriority.Medium;
///
/// STATUS:
///
/// Nuevas tareas SIEMPRE empiezan en Pending.
/// NO permitir que cliente especifique Status:
///
/// ❌ public TaskStatus Status { get; set; }  // Cliente podría crear tarea ya Completed
///
/// ✅ En el handler:
/// var task = TaskItem.Create(...);  // Status = Pending (hardcoded)
///
/// EJEMPLO FRONTEND:
///
/// interface CreateTaskRequest {
///   title: string;
///   description?: string;
///   dueDate?: string;  // ISO 8601
///   priority?: 'Low' | 'Medium' | 'High';
/// }
///
/// async function createTask(request: CreateTaskRequest): Promise<TaskDto> {
///   const accessToken = localStorage.getItem('accessToken');
///
///   const response = await fetch('/api/tasks', {
///     method: 'POST',
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
/// const newTask = await createTask({
///   title: 'Completar proyecto',
///   description: 'Finalizar módulo de autenticación',
///   dueDate: new Date('2024-01-20').toISOString(),
///   priority: 'High'
/// });
///
/// console.log('Task created:', newTask.id);
///
/// FORM VALIDATION (Frontend):
///
/// function TaskForm() {
///   const [formData, setFormData] = useState<CreateTaskRequest>({
///     title: '',
///     priority: 'Medium'
///   });
///
///   const [errors, setErrors] = useState<Record<string, string>>({});
///
///   const validate = () => {
///     const newErrors: Record<string, string> = {};
///
///     if (!formData.title || formData.title.trim() === '') {
///       newErrors.title = 'Title is required';
///     } else if (formData.title.length > 200) {
///       newErrors.title = 'Title must not exceed 200 characters';
///     }
///
///     if (formData.description && formData.description.length > 2000) {
///       newErrors.description = 'Description must not exceed 2000 characters';
///     }
///
///     if (formData.dueDate && new Date(formData.dueDate) <= new Date()) {
///       newErrors.dueDate = 'Due date must be in the future';
///     }
///
///     setErrors(newErrors);
///     return Object.keys(newErrors).length === 0;
///   };
///
///   const handleSubmit = async (e: React.FormEvent) => {
///     e.preventDefault();
///
///     if (!validate()) return;
///
///     try {
///       const newTask = await createTask(formData);
///       // Redirigir o mostrar success
///       navigate('/tasks');
///     } catch (error) {
///       // Mostrar error del servidor
///       setErrors({ submit: error.message });
///     }
///   };
///
///   return (
///     <form onSubmit={handleSubmit}>
///       <input
///         type="text"
///         value={formData.title}
///         onChange={(e) => setFormData({ ...formData, title: e.target.value })}
///         placeholder="Task title"
///       />
///       {errors.title && <span className="error">{errors.title}</span>}
///
///       <textarea
///         value={formData.description || ''}
///         onChange={(e) => setFormData({ ...formData, description: e.target.value })}
///         placeholder="Description (optional)"
///       />
///
///       <input
///         type="datetime-local"
///         value={formData.dueDate || ''}
///         onChange={(e) => setFormData({ ...formData, dueDate: e.target.value })}
///       />
///
///       <select
///         value={formData.priority}
///         onChange={(e) => setFormData({ ...formData, priority: e.target.value as TaskPriority })}
///       >
///         <option value="Low">Low</option>
///         <option value="Medium">Medium</option>
///         <option value="High">High</option>
///       </select>
///
///       <button type="submit">Create Task</button>
///     </form>
///   );
/// }
///
/// TESTING:
///
/// [Fact]
/// public async Task CreateTask_ValidRequest_CreatesTask()
/// {
///     // Arrange
///     var request = new CreateTaskRequest
///     {
///         Title = "Test Task",
///         Description = "Test Description",
///         Priority = TaskPriority.High
///     };
///
///     // Act
///     var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks", request);
///
///     // Assert
///     response.EnsureSuccessStatusCode();
///     Assert.Equal(HttpStatusCode.Created, response.StatusCode);
///
///     var dto = await response.Content.ReadFromJsonAsync<TaskDto>();
///     Assert.NotNull(dto);
///     Assert.Equal("Test Task", dto.Title);
///     Assert.Equal("Test Description", dto.Description);
///     Assert.Equal(TaskPriority.High, dto.Priority);
///     Assert.Equal(TaskStatus.Pending, dto.Status);  // Always Pending
///
///     // Verificar Location header
///     Assert.NotNull(response.Headers.Location);
///     Assert.Contains($"/api/tasks/{dto.Id}", response.Headers.Location.ToString());
/// }
///
/// [Fact]
/// public async Task CreateTask_EmptyTitle_ReturnsBadRequest()
/// {
///     // Arrange
///     var request = new CreateTaskRequest { Title = "" };
///
///     // Act
///     var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks", request);
///
///     // Assert
///     Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
/// }
///
/// [Fact]
/// public async Task CreateTask_Unauthenticated_ReturnsUnauthorized()
/// {
///     // Arrange
///     var request = new CreateTaskRequest { Title = "Test" };
///
///     // Act - Sin token
///     var response = await _client.PostAsJsonAsync("/api/tasks", request);
///
///     // Assert
///     Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
/// }
/// </remarks>
public class CreateTaskRequest
{
    /// <summary>
    /// Título de la tarea (requerido).
    /// </summary>
    /// <example>Completar informe trimestral</example>
    /// <remarks>
    /// - Requerido
    /// - Mínimo 1 carácter (no vacío)
    /// - Máximo 200 caracteres
    /// - Describe brevemente la tarea
    ///
    /// Validaciones:
    /// - NotEmpty: No puede estar vacío
    /// - MaximumLength(200): Límite de caracteres
    /// </remarks>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción detallada de la tarea (opcional).
    /// </summary>
    /// <example>Elaborar el informe financiero del Q1 2024 con análisis de ventas y proyecciones para Q2</example>
    /// <remarks>
    /// - Opcional (puede ser null o vacío)
    /// - Máximo 2000 caracteres
    /// - Detalles adicionales sobre la tarea
    ///
    /// Validación:
    /// - MaximumLength(2000): Límite de caracteres
    /// </remarks>
    public string? Description { get; set; }

    /// <summary>
    /// Fecha y hora límite para completar la tarea (opcional).
    /// </summary>
    /// <example>2024-01-20T23:59:59Z</example>
    /// <remarks>
    /// - Opcional (puede ser null)
    /// - Formato ISO 8601: "2024-01-20T23:59:59Z"
    /// - Debe ser en el futuro (no pasado)
    /// - UTC timestamp (servidor convierte a UTC)
    ///
    /// Validación:
    /// - GreaterThan(DateTime.UtcNow): Debe ser futura
    ///
    /// Frontend:
    /// - Input: datetime-local
    /// - Convertir a ISO 8601: new Date(input).toISOString()
    /// - Mostrar en zona horaria local: new Date(dto.dueDate).toLocaleString()
    /// </remarks>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Prioridad de la tarea (opcional, default: Medium).
    /// </summary>
    /// <example>High</example>
    /// <remarks>
    /// - Opcional (si no se especifica, default: Medium)
    /// - Valores válidos: Low (0), Medium (1), High (2)
    ///
    /// Validación:
    /// - IsInEnum: Debe ser valor válido del enum
    ///
    /// Se serializa como string en JSON: "Low", "Medium", "High"
    ///
    /// Frontend:
    /// - Select dropdown con 3 opciones
    /// - Default seleccionado: Medium
    /// </remarks>
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
}
