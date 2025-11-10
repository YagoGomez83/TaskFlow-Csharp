using TaskManagement.Domain.Enums;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.DTOs.Tasks;

/// <summary>
/// DTO para representar una tarea.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE TaskDto:
///
/// Este DTO representa una tarea para lectura (output).
/// Se usa en:
/// - GET /api/tasks (listar tareas)
/// - GET /api/tasks/{id} (obtener tarea por ID)
/// - POST /api/tasks (respuesta después de crear)
/// - PUT /api/tasks/{id} (respuesta después de actualizar)
///
/// DTO vs ENTITY:
///
/// TaskDto (para API) ≠ TaskItem (entidad de dominio)
///
/// Diferencias:
///
/// TaskItem (Entity):
/// - Tiene comportamiento (métodos: UpdateTitle, Complete, etc.)
/// - Tiene lógica de validación
/// - Tiene relaciones (navegaciones a User, etc.)
/// - Propiedades privadas con setters
/// - Enfocado en reglas de negocio
///
/// TaskDto (DTO):
/// - Solo propiedades públicas
/// - Sin comportamiento (POCO - Plain Old CLR Object)
/// - Sin lógica de negocio
/// - Diseñado para serialización JSON
/// - Enfocado en representación de datos
///
/// MAPEO: TaskItem → TaskDto
///
/// Usando AutoMapper:
/// public class TaskMappingProfile : Profile
/// {
///     public TaskMappingProfile()
///     {
///         CreateMap<TaskItem, TaskDto>();
///     }
/// }
///
/// En el handler:
/// var task = await _context.Tasks.FindAsync(id);
/// var dto = _mapper.Map<TaskDto>(task);
/// return Result.Success(dto);
///
/// Sin AutoMapper (manual):
/// var dto = new TaskDto
/// {
///     Id = task.Id,
///     Title = task.Title,
///     Description = task.Description,
///     DueDate = task.DueDate,
///     Priority = task.Priority,
///     Status = task.Status,
///     UserId = task.UserId,
///     CreatedAt = task.CreatedAt,
///     UpdatedAt = task.UpdatedAt
/// };
///
/// AutoMapper es preferible:
/// - Menos código repetitivo
/// - Mapeos centralizados
/// - Fácil de mantener
/// - Proyecciones eficientes (LINQ to SQL)
///
/// PROYECCIÓN EN QUERY:
///
/// ❌ Ineficiente (cargar entidad completa y mapear):
/// var tasks = await _context.Tasks.ToListAsync();
/// var dtos = _mapper.Map<List<TaskDto>>(tasks);
///
/// ✅ Eficiente (proyectar directamente en query):
/// var dtos = await _context.Tasks
///     .ProjectTo<TaskDto>(_mapper.ConfigurationProvider)
///     .ToListAsync();
///
/// SQL generado con ProjectTo:
/// SELECT Id, Title, Description, DueDate, Priority, Status, UserId, CreatedAt, UpdatedAt
/// FROM Tasks
///
/// Solo selecciona campos necesarios, no toda la entidad.
///
/// CAMPOS INCLUIDOS:
///
/// 1. Id: Identificador único de la tarea
/// 2. Title: Título de la tarea
/// 3. Description: Descripción detallada (opcional)
/// 4. DueDate: Fecha límite (opcional)
/// 5. Priority: Prioridad (Low/Medium/High)
/// 6. Status: Estado (Pending/InProgress/Completed)
/// 7. UserId: ID del usuario dueño
/// 8. CreatedAt: Cuándo se creó
/// 9. UpdatedAt: Última actualización
///
/// CAMPOS NO INCLUIDOS:
///
/// - IsDeleted: Campo interno, no exponer
/// - DeletedAt: Campo interno, no exponer
/// - Navegaciones (User): No incluir objetos anidados por defecto
///
/// Si necesitas información del usuario:
///
/// Opción A: DTO separado con User
/// public class TaskWithUserDto
/// {
///     public Guid Id { get; set; }
///     public string Title { get; set; }
///     // ... otros campos de task
///     public UserDto User { get; set; }  // ← Objeto anidado
/// }
///
/// Opción B: Campos aplanados
/// public class TaskDto
/// {
///     public Guid Id { get; set; }
///     public string Title { get; set; }
///     public string UserEmail { get; set; }  // ← Campo del usuario
/// }
///
/// CreateMap<TaskItem, TaskDto>()
///     .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User.Email.Value));
///
/// Para este proyecto: Solo campos de Task (sin User).
/// Cliente ya tiene userId, puede obtener info de usuario por separado si necesita.
///
/// FORMATO DE FECHAS:
///
/// DateTime se serializa a JSON en formato ISO 8601:
/// {
///   "createdAt": "2024-01-15T10:30:00Z",
///   "updatedAt": "2024-01-15T14:45:00Z",
///   "dueDate": "2024-01-20T00:00:00Z"
/// }
///
/// ASP.NET Core usa System.Text.Json por defecto:
/// - UTC: "2024-01-15T10:30:00Z" (con Z)
/// - Local: "2024-01-15T10:30:00-05:00" (con offset)
///
/// Configurar formato global:
/// builder.Services.AddControllers()
///     .AddJsonOptions(options =>
///     {
///         options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
///         options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
///     });
///
/// CAMELCASE vs PASCALCASE:
///
/// C# usa PascalCase: TaskDto.Title
/// JavaScript usa camelCase: taskDto.title
///
/// ASP.NET Core automáticamente convierte:
/// C#: { Title: "My Task", Priority: "High" }
/// JSON: { "title": "My Task", "priority": "High" }
///
/// Configurado por defecto en ASP.NET Core.
///
/// ENUMS EN JSON:
///
/// Por defecto, enums se serializan como strings:
/// { "status": "InProgress", "priority": "High" }
///
/// Alternativa - Números:
/// { "status": 1, "priority": 2 }
///
/// Configurar:
/// .AddJsonOptions(options =>
/// {
///     options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
/// });
///
/// Preferir strings: más legible para frontend.
///
/// NULL vs OMIT:
///
/// Campos opcionales (Description, DueDate) pueden ser null.
///
/// Opción A: Incluir null en JSON
/// { "description": null, "dueDate": null }
///
/// Opción B: Omitir campos null
/// {} // Sin description ni dueDate
///
/// Configurar:
/// options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
///
/// Para este proyecto: Omitir nulls (JSON más limpio).
///
/// PAGINACIÓN:
///
/// Para listas, usar PaginatedList<TaskDto>:
///
/// {
///   "items": [
///     { "id": "...", "title": "Task 1", ... },
///     { "id": "...", "title": "Task 2", ... }
///   ],
///   "pageNumber": 1,
///   "totalPages": 5,
///   "totalCount": 100,
///   "hasNextPage": true,
///   "hasPreviousPage": false
/// }
///
/// FILTRADO Y ORDENAMIENTO:
///
/// GET /api/tasks?status=InProgress&priority=High&orderBy=dueDate
///
/// Query handler aplica filtros y retorna TaskDto:
/// var query = _context.Tasks.Where(t => t.UserId == userId);
///
/// if (request.Status.HasValue)
///     query = query.Where(t => t.Status == request.Status.Value);
///
/// if (request.Priority.HasValue)
///     query = query.Where(t => t.Priority == request.Priority.Value);
///
/// query = request.OrderBy switch
/// {
///     "dueDate" => query.OrderBy(t => t.DueDate),
///     "priority" => query.OrderByDescending(t => t.Priority),
///     _ => query.OrderByDescending(t => t.CreatedAt)
/// };
///
/// var tasks = await query.ProjectTo<TaskDto>(_mapper.ConfigurationProvider).ToListAsync();
///
/// SWAGGER DOCUMENTATION:
///
/// Este DTO genera documentación automática en Swagger:
///
/// GET /api/tasks
/// Response 200:
/// [
///   {
///     "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
///     "title": "string",
///     "description": "string",
///     "dueDate": "2024-01-15T10:30:00Z",
///     "priority": "Low",
///     "status": "Pending",
///     "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
///     "createdAt": "2024-01-15T10:30:00Z",
///     "updatedAt": "2024-01-15T10:30:00Z"
///   }
/// ]
///
/// Agregar XML comments para mejor documentación:
/// /// <summary>Título de la tarea</summary>
/// public string Title { get; set; }
///
/// VERSIONADO:
///
/// Si API evoluciona, puedes crear TaskDtoV2:
///
/// public class TaskDtoV2 : TaskDto
/// {
///     public List<string> Tags { get; set; }  // Nuevo campo
///     public int EstimatedHours { get; set; }
/// }
///
/// Endpoints:
/// GET /api/v1/tasks → Retorna TaskDto
/// GET /api/v2/tasks → Retorna TaskDtoV2
///
/// TESTING:
///
/// [Fact]
/// public async Task GetTask_ReturnsTaskDto()
/// {
///     // Arrange
///     var task = TaskItem.Create("Test Task", "Description", userId);
///     _context.Tasks.Add(task);
///     await _context.SaveChangesAsync();
///
///     // Act
///     var response = await _client.GetAsync($"/api/tasks/{task.Id}");
///
///     // Assert
///     response.EnsureSuccessStatusCode();
///     var dto = await response.Content.ReadFromJsonAsync<TaskDto>();
///
///     Assert.NotNull(dto);
///     Assert.Equal(task.Id, dto.Id);
///     Assert.Equal("Test Task", dto.Title);
///     Assert.Equal("Description", dto.Description);
///     Assert.Equal(TaskStatus.Pending, dto.Status);
/// }
///
/// FRONTEND USAGE:
///
/// interface TaskDto {
///   id: string;
///   title: string;
///   description?: string;
///   dueDate?: string;  // ISO 8601 string
///   priority: 'Low' | 'Medium' | 'High';
///   status: 'Pending' | 'InProgress' | 'Completed';
///   userId: string;
///   createdAt: string;
///   updatedAt: string;
/// }
///
/// async function getTasks(): Promise<TaskDto[]> {
///   const response = await fetch('/api/tasks');
///   return response.json();
/// }
///
/// function TaskCard({ task }: { task: TaskDto }) {
///   return (
///     <div>
///       <h3>{task.title}</h3>
///       <p>{task.description}</p>
///       <span>Due: {new Date(task.dueDate).toLocaleDateString()}</span>
///       <span>Priority: {task.priority}</span>
///       <span>Status: {task.status}</span>
///     </div>
///   );
/// }
/// </remarks>
public class TaskDto
{
    /// <summary>
    /// Identificador único de la tarea.
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>
    /// Título de la tarea.
    /// </summary>
    /// <example>Completar informe trimestral</example>
    /// <remarks>
    /// - Requerido
    /// - Máximo 200 caracteres
    /// - Describe brevemente la tarea
    /// </remarks>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción detallada de la tarea.
    /// </summary>
    /// <example>Elaborar el informe financiero del Q1 2024 con análisis de ventas y proyecciones</example>
    /// <remarks>
    /// - Opcional (puede ser null)
    /// - Máximo 2000 caracteres
    /// - Detalles adicionales de la tarea
    /// - Si es null, se omite del JSON (JsonIgnoreCondition.WhenWritingNull)
    /// </remarks>
    public string? Description { get; set; }

    /// <summary>
    /// Fecha y hora límite para completar la tarea.
    /// </summary>
    /// <example>2024-01-20T23:59:59Z</example>
    /// <remarks>
    /// - Opcional (puede ser null)
    /// - Formato ISO 8601: "2024-01-20T23:59:59Z"
    /// - UTC timestamp
    /// - Frontend debe convertir a zona horaria local para display
    /// - Si es null, la tarea no tiene deadline
    /// </remarks>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Prioridad de la tarea.
    /// </summary>
    /// <example>High</example>
    /// <remarks>
    /// Valores posibles:
    /// - Low (0): Baja prioridad
    /// - Medium (1): Prioridad media (default)
    /// - High (2): Alta prioridad
    ///
    /// Se serializa como string en JSON: "Low", "Medium", "High"
    /// Frontend puede usar para:
    /// - Ordenar tareas
    /// - Aplicar colores/badges
    /// - Filtrar por prioridad
    /// </remarks>
    public TaskPriority Priority { get; set; }

    /// <summary>
    /// Estado actual de la tarea.
    /// </summary>
    /// <example>InProgress</example>
    /// <remarks>
    /// Valores posibles:
    /// - Pending (0): No iniciada
    /// - InProgress (1): En progreso
    /// - Completed (2): Completada
    ///
    /// Se serializa como string en JSON: "Pending", "InProgress", "Completed"
    ///
    /// Workflow:
    /// Pending → InProgress → Completed
    ///
    /// Puede volver atrás:
    /// Completed → Pending (reabrir tarea)
    /// </remarks>
    public TaskStatus Status { get; set; }

    /// <summary>
    /// ID del usuario dueño de la tarea.
    /// </summary>
    /// <example>a1b2c3d4-e5f6-7890-abcd-ef1234567890</example>
    /// <remarks>
    /// - Usuario que creó la tarea
    /// - Solo el dueño (o Admin) puede modificar/eliminar
    /// - No incluimos objeto User completo para evitar circular references
    /// - Cliente puede obtener info de usuario por separado si necesita
    /// </remarks>
    public Guid UserId { get; set; }

    /// <summary>
    /// Fecha y hora de creación de la tarea.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    /// <remarks>
    /// - Asignado automáticamente por BaseEntity
    /// - UTC timestamp
    /// - Formato ISO 8601
    /// - Inmutable (no cambia)
    /// - Útil para ordenar por "más reciente"
    /// </remarks>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Fecha y hora de última actualización de la tarea.
    /// </summary>
    /// <example>2024-01-15T14:45:00Z</example>
    /// <remarks>
    /// - Actualizado automáticamente por BaseEntity
    /// - UTC timestamp
    /// - Formato ISO 8601
    /// - Cambia cada vez que se modifica la tarea
    /// - Útil para detectar cambios
    /// - Puede usarse para optimistic concurrency (si implementas)
    /// </remarks>
    public DateTime UpdatedAt { get; set; }
}
