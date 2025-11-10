using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.Domain.Entities;

/// <summary>
/// Entidad de dominio que representa una tarea del usuario.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE LA ENTIDAD TASKITEM:
///
/// Esta es la entidad central del sistema. Representa una tarea que un usuario
/// necesita completar, con toda su información y reglas de negocio asociadas.
///
/// POR QUÉ "TaskItem" Y NO SOLO "Task":
///
/// En C#, "Task" es una clase del framework (System.Threading.Tasks.Task) usada
/// para programación asíncrona. Para evitar confusión y conflictos de nombres,
/// nombramos nuestra entidad "TaskItem".
///
/// Alternativas consideradas:
/// - TodoItem (pero es más específico para listas de tareas simples)
/// - WorkItem (muy genérico)
/// - TaskEntity (sufijo redundante)
/// - UserTask (prefijo innecesario, la relación está en UserId)
///
/// CICLO DE VIDA DE UNA TAREA:
///
/// 1. CREACIÓN:
///    - Usuario crea tarea con título, descripción, prioridad, fecha límite
///    - Estado inicial: Pending
///    - Se asigna automáticamente al usuario creador (UserId)
///
/// 2. EN PROGRESO:
///    - Usuario comienza a trabajar → Status cambia a InProgress
///    - Puede pausarse → Status vuelve a Pending
///
/// 3. COMPLETADA:
///    - Usuario termina → Status cambia a Completed
///    - Puede reabrirse si es necesario → Status vuelve a Pending o InProgress
///
/// 4. ELIMINACIÓN:
///    - Soft delete: IsDeleted = true (hereda de BaseEntity)
///    - La tarea no se elimina físicamente de la DB
///    - Admin puede ver tareas eliminadas con query filter disabled
///
/// REGLAS DE NEGOCIO IMPLEMENTADAS:
///
/// 1. OWNERSHIP (Propiedad):
///    - Cada tarea pertenece a un único usuario (UserId)
///    - Solo el owner puede modificar/eliminar (verificado en Application layer)
///    - Admin puede ver/modificar todas las tareas (RBAC)
///
/// 2. VALIDACIONES:
///    - Title: requerido, max 200 caracteres
///    - Description: opcional, max 2000 caracteres
///    - DueDate: debe ser futura (validado en Application)
///    - Priority: Low, Medium, High
///    - Status: Pending, InProgress, Completed
///
/// 3. TRANSICIONES DE ESTADO:
///    - Cualquier estado puede cambiar a cualquier otro (flexibilidad)
///    - No hay restricciones de transición (el usuario decide)
///    - Se podría agregar validación si el negocio lo requiere
///
/// MÉTRICAS Y ANÁLISIS:
///
/// Con esta entidad podemos calcular:
/// - Lead time: CreatedAt → CompletedAt
/// - Cycle time: InProgress start → CompletedAt
/// - Tasa de completitud: Completed / Total
/// - Tareas vencidas: DueDate < Now && Status != Completed
/// - Distribución por prioridad
/// - Productividad por usuario
///
/// FUTURAS EXPANSIONES:
///
/// - Tags/Labels: Categorización flexible
/// - Subtasks: Tareas anidadas
/// - Attachments: Archivos adjuntos
/// - Comments: Conversaciones sobre la tarea
/// - Recurrence: Tareas recurrentes
/// - Estimates: Tiempo estimado vs real
/// - Assignees: Asignar a múltiples usuarios (colaboración)
/// - Projects: Agrupar tareas en proyectos
///
/// AGGREGATE ROOT:
///
/// TaskItem es un aggregate root en DDD.
/// Coordina todas las operaciones sobre sus datos y garantiza invariantes.
/// </remarks>
public class TaskItem : BaseEntity
{
    /// <summary>
    /// Título de la tarea (requerido, max 200 caracteres).
    /// </summary>
    /// <remarks>
    /// EXPLICACIÓN DEL TÍTULO:
    ///
    /// El título es un resumen breve y conciso de lo que hay que hacer.
    /// Es lo primero que ve el usuario en listados.
    ///
    /// REGLAS:
    /// - Requerido: Toda tarea debe tener título
    /// - Max 200 caracteres: Suficiente para descripción breve
    /// - Se valida con FluentValidation en Application layer
    /// - Se previene XSS (HTML encoding en presentación)
    ///
    /// EJEMPLOS DE BUENOS TÍTULOS:
    /// ✅ "Implementar autenticación JWT"
    /// ✅ "Revisar PR #123"
    /// ✅ "Llamar a cliente sobre proyecto X"
    /// ✅ "Comprar leche"
    ///
    /// EJEMPLOS DE MALOS TÍTULOS:
    /// ❌ "Tarea" (demasiado genérico)
    /// ❌ "" (vacío, no permitido)
    /// ❌ "Lorem ipsum dolor sit amet..." (muy largo, usar Description)
    ///
    /// PREVENCIÓN XSS:
    /// El título NO debe contener HTML tags.
    /// Validación en CreateTaskRequestValidator:
    /// RuleFor(x => x.Title).Must(NotContainHtmlTags)
    ///
    /// En presentación (frontend), usar encoding:
    /// <div>{HtmlEncoder.Default.Encode(task.Title)}</div>
    ///
    /// BÚSQUEDA:
    /// El título es parte de la búsqueda full-text.
    /// Se indexa en DB para búsquedas rápidas:
    /// WHERE Title LIKE '%search%' OR Description LIKE '%search%'
    ///
    /// private set: Solo modificable via UpdateTitle() que valida reglas.
    /// </remarks>
    public string Title { get; private set; }

    /// <summary>
    /// Descripción detallada de la tarea (opcional, max 2000 caracteres).
    /// </summary>
    /// <remarks>
    /// EXPLICACIÓN DE LA DESCRIPCIÓN:
    ///
    /// La descripción proporciona contexto adicional sobre la tarea.
    /// Es opcional pero recomendada para tareas complejas.
    ///
    /// REGLAS:
    /// - Opcional: Puede ser null o vacía
    /// - Max 2000 caracteres: Suficiente para detalles, no un ensayo
    /// - Se valida en Application layer
    /// - Se previene XSS
    ///
    /// USOS:
    /// - Pasos detallados para completar la tarea
    /// - Contexto adicional
    /// - Links a recursos
    /// - Notas importantes
    ///
    /// EJEMPLOS:
    /// "Implementar autenticación JWT siguiendo estos pasos:
    /// 1. Instalar paquete System.IdentityModel.Tokens.Jwt
    /// 2. Crear TokenService con método GenerateToken
    /// 3. Configurar en Program.cs
    /// Ver docs: https://jwt.io"
    ///
    /// FORMATO:
    /// - Plain text (no HTML por seguridad)
    /// - Puede contener URLs (serán linkificadas en frontend)
    /// - Preserva saltos de línea (white-space: pre-wrap en CSS)
    ///
    /// MARKDOWN (FUTURO):
    /// Considerar soportar Markdown para formato rico:
    /// - **Bold**, *italic*
    /// - Listas, checkboxes
    /// - Código con syntax highlighting
    /// Librería: Markdig
    ///
    /// null vs string.Empty:
    /// Permitimos null para diferenciar "sin descripción" de "descripción vacía".
    /// Aunque en la práctica se comportan igual.
    /// </remarks>
    public string? Description { get; private set; }

    /// <summary>
    /// Fecha y hora límite para completar la tarea (UTC, opcional).
    /// </summary>
    /// <remarks>
    /// EXPLICACIÓN DE DUE DATE:
    ///
    /// La fecha límite ayuda a priorizar y planificar.
    /// Es opcional porque no todas las tareas tienen deadline.
    ///
    /// NULLABLE (DateTime?):
    /// - null = Sin fecha límite (puede hacerse cuando sea)
    /// - DateTime = Debe completarse antes de esta fecha
    ///
    /// ZONA HORARIA:
    /// Siempre UTC en la base de datos.
    /// - Evita problemas con zonas horarias
    /// - Facilita comparaciones y ordenamiento
    /// - Se convierte a zona local en presentación
    ///
    /// Conversión en frontend:
    /// const localDate = new Date(task.dueDate).toLocaleDateString();
    ///
    /// VALIDACIÓN:
    /// - Debe ser fecha futura (validado en Application layer)
    /// - Si es pasada al crear, advertir pero permitir (puede ser intencional)
    /// - No puede ser más de 10 años en el futuro (sanity check)
    ///
    /// ALERTAS Y NOTIFICACIONES:
    /// Basándonos en DueDate podemos:
    /// - Alertar cuando está cerca (ej: 1 día antes)
    /// - Marcar como "overdue" si pasó y no está completada
    /// - Ordenar por urgencia
    /// - Calcular "slack time" (tiempo restante)
    ///
    /// CÁLCULOS ÚTILES:
    /// - Días restantes: (DueDate - DateTime.UtcNow).TotalDays
    /// - Está vencida: DueDate < DateTime.UtcNow && Status != Completed
    /// - Urgente: DueDate - DateTime.UtcNow < TimeSpan.FromDays(1)
    ///
    /// ORDENAMIENTO:
    /// Tareas con DueDate más cercano primero:
    /// ORDER BY DueDate ASC NULLS LAST
    /// (NULLS LAST pone las sin fecha límite al final)
    ///
    /// FUTURAS MEJORAS:
    /// - DueTime además de DueDate (hora específica)
    /// - Recordatorios configurables (1 día, 1 hora antes)
    /// - Recurrencia (repetir cada N días)
    /// - Timezone del usuario (almacenar y mostrar en su zona)
    /// </remarks>
    public DateTime? DueDate { get; private set; }

    /// <summary>
    /// Prioridad de la tarea (Low, Medium, High).
    /// </summary>
    /// <remarks>
    /// EXPLICACIÓN DE PRIORIDAD:
    ///
    /// La prioridad ayuda a decidir QUÉ tarea trabajar primero.
    /// Ver TaskPriority enum para explicación detallada de cada nivel.
    ///
    /// POR DEFECTO: Medium
    /// La mayoría de tareas son de prioridad media.
    /// - Low: 20% de tareas
    /// - Medium: 60% de tareas
    /// - High: 20% de tareas
    ///
    /// Si todo es High, nada es realmente High.
    /// Educar al usuario sobre uso correcto de prioridades.
    ///
    /// COMBINACIÓN CON DUEDATE:
    /// La prioridad efectiva combina prioridad + fecha límite:
    ///
    /// Matriz de priorización:
    ///                 │ DueDate Soon  │ DueDate Later
    /// ────────────────┼───────────────┼──────────────
    /// Priority High   │ URGENT        │ Important
    /// Priority Medium │ Important     │ Normal
    /// Priority Low    │ Normal        │ Later
    ///
    /// ORDENAMIENTO RECOMENDADO:
    /// ORDER BY
    ///   CASE
    ///     WHEN Priority = 'High' AND DueDate < NOW() + INTERVAL '1 day' THEN 1
    ///     WHEN Priority = 'High' THEN 2
    ///     WHEN Priority = 'Medium' AND DueDate < NOW() + INTERVAL '3 days' THEN 3
    ///     WHEN Priority = 'Medium' THEN 4
    ///     ELSE 5
    ///   END,
    ///   DueDate ASC NULLS LAST,
    ///   CreatedAt ASC
    ///
    /// CAMBIOS DE PRIORIDAD:
    /// Es común que la prioridad cambie con el tiempo:
    /// - Medium → High cuando se acerca el deadline
    /// - High → Low si ya no es urgente
    ///
    /// ANALYTICS:
    /// - ¿Cuántas tareas High se completan a tiempo?
    /// - ¿Cuánto tiempo pasan en cada prioridad?
    /// - ¿Hay usuario que marca todo como High? (educación necesaria)
    ///
    /// COLOR CODING (UI):
    /// - High: Rojo (#EF4444)
    /// - Medium: Amarillo (#F59E0B)
    /// - Low: Azul (#3B82F6)
    /// </remarks>
    public TaskPriority Priority { get; private set; }

    /// <summary>
    /// Estado actual de la tarea (Pending, InProgress, Completed).
    /// </summary>
    /// <remarks>
    /// EXPLICACIÓN DEL ESTADO:
    ///
    /// El estado representa en qué punto del flujo de trabajo está la tarea.
    /// Ver TaskStatus enum para explicación detallada de cada estado.
    ///
    /// ESTADO INICIAL: Pending
    /// Toda tarea nueva se crea en estado Pending.
    ///
    /// TRANSICIONES:
    /// Este diseño permite transiciones flexibles:
    /// - Usuario puede cambiar de cualquier estado a cualquier otro
    /// - No hay restricciones (máxima flexibilidad)
    ///
    /// Si el negocio requiere restricciones, podríamos agregar:
    /// public void MarkAsInProgress()
    /// {
    ///     if (Status == TaskStatus.Completed)
    ///         throw new DomainException("Cannot reopen completed task");
    ///     Status = TaskStatus.InProgress;
    /// }
    ///
    /// Pero por ahora, dejamos libertad al usuario.
    ///
    /// MÉTRICAS POR ESTADO:
    ///
    /// KPIs importantes:
    /// - Work In Progress (WIP): Cuántas tareas en InProgress
    ///   * WIP alto → Usuario sobrecargado
    ///   * WIP bajo → Usuario puede tomar más tareas
    ///   * Límite recomendado: 3-5 tareas simultáneas
    ///
    /// - Throughput: Tareas completadas por período
    ///   * Ej: 10 tareas/semana
    ///   * Indica productividad
    ///
    /// - Lead Time: Tiempo de Pending → Completed
    ///   * Ej: 3 días promedio
    ///   * Incluye tiempo en backlog
    ///
    /// - Cycle Time: Tiempo de InProgress → Completed
    ///   * Ej: 1 día promedio
    ///   * Solo tiempo activo trabajando
    ///
    /// FILTROS COMUNES:
    /// - Tareas activas: Status IN (Pending, InProgress)
    /// - Tareas completadas hoy: Status = Completed AND UpdatedAt >= TODAY
    /// - Tareas estancadas: Status = InProgress AND UpdatedAt < NOW() - 7 days
    ///
    /// KANBAN BOARD:
    /// El estado mapea directamente a columnas de Kanban:
    /// - Columna "To Do": Status = Pending
    /// - Columna "In Progress": Status = InProgress
    /// - Columna "Done": Status = Completed
    ///
    /// EVENTOS DE DOMINIO (FUTURO):
    /// Podríamos disparar eventos al cambiar estado:
    /// - TaskStarted (Pending → InProgress)
    /// - TaskCompleted (InProgress → Completed)
    /// - TaskReopened (Completed → Pending/InProgress)
    ///
    /// Estos eventos pueden trigger:
    /// - Notificaciones
    /// - Actualizaciones de métricas
    /// - Integraciones con otros sistemas
    /// </remarks>
    public Enums.TaskStatus Status { get; private set; }

    /// <summary>
    /// ID del usuario propietario de la tarea.
    /// </summary>
    /// <remarks>
    /// EXPLICACIÓN DE OWNERSHIP:
    ///
    /// Cada tarea pertenece a un único usuario (el que la creó).
    /// Este es el concepto de OWNERSHIP en el sistema.
    ///
    /// REGLAS DE OWNERSHIP:
    ///
    /// 1. CREACIÓN:
    ///    - UserId se asigna automáticamente al crear la tarea
    ///    - Es el ID del usuario autenticado (_currentUser.UserId)
    ///    - No puede ser Guid.Empty
    ///
    /// 2. MODIFICACIÓN:
    ///    - Solo el owner puede editar/eliminar su tarea
    ///    - Admin puede editar/eliminar cualquier tarea (RBAC)
    ///    - No se puede cambiar el owner (la tarea no se "transfiere")
    ///
    /// 3. VISUALIZACIÓN:
    ///    - User solo ve sus propias tareas
    ///    - Admin ve todas las tareas
    ///
    /// VALIDACIÓN DE OWNERSHIP (Application Layer):
    ///
    /// En handlers de Update/Delete:
    /// var task = await _context.Tasks.FindAsync(request.Id);
    /// if (task.UserId != _currentUser.UserId && _currentUser.Role != UserRole.Admin)
    /// {
    ///     throw new UnauthorizedAccessException("Cannot modify tasks of other users");
    /// }
    ///
    /// QUERIES FILTRADAS:
    ///
    /// User ve solo sus tareas:
    /// var tasks = await _context.Tasks
    ///     .Where(t => t.UserId == _currentUser.UserId)
    ///     .ToListAsync();
    ///
    /// Admin ve todas:
    /// var tasks = _currentUser.Role == UserRole.Admin
    ///     ? await _context.Tasks.ToListAsync()
    ///     : await _context.Tasks.Where(t => t.UserId == _currentUser.UserId).ToListAsync();
    ///
    /// FOREIGN KEY:
    /// UserId es una Foreign Key a la tabla Users.
    /// Configuración en EF Core:
    /// builder.HasOne<User>()
    ///     .WithMany()
    ///     .HasForeignKey(t => t.UserId)
    ///     .OnDelete(DeleteBehavior.Cascade);
    ///
    /// OnDelete(Cascade):
    /// Si se elimina un User, todas sus Tasks se eliminan automáticamente.
    /// Alternativa: OnDelete(Restrict) previene eliminar User si tiene Tasks.
    ///
    /// ÍNDICE DE BASE DE DATOS:
    /// UserId debe tener índice para búsquedas rápidas:
    /// CREATE INDEX IX_Tasks_UserId ON Tasks(UserId);
    ///
    /// Especialmente importante para:
    /// WHERE UserId = @userId
    /// WHERE UserId = @userId AND Status = 'Pending'
    ///
    /// FUTURAS EXPANSIONES:
    ///
    /// 1. COMPARTIR TAREAS:
    ///    - SharedWith: List<Guid> (IDs de usuarios con acceso)
    ///    - Permisos: View, Edit, Delete
    ///
    /// 2. ASIGNACIÓN:
    ///    - CreatedBy: Quién la creó
    ///    - AssignedTo: A quién se asignó
    ///    - Permitir delegar tareas a otros usuarios
    ///
    /// 3. EQUIPOS:
    ///    - TeamId: Tarea pertenece a un equipo
    ///    - Miembros del equipo pueden ver/editar
    ///
    /// private set: Solo puede asignarse en constructor (inmutable después de creación).
    /// </remarks>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Constructor privado para EF Core.
    /// </summary>
    /// <remarks>
    /// EF Core necesita un constructor sin parámetros para hidratar entidades desde DB.
    /// private: No se puede usar directamente en código de negocio.
    /// Para crear tareas, usar el factory method Create().
    /// </remarks>
    private TaskItem()
    {
        // Constructor vacío para EF Core
        Title = string.Empty; // Inicializar para evitar warning de nullable
    }

    /// <summary>
    /// Factory method para crear una nueva tarea.
    /// </summary>
    /// <param name="title">Título de la tarea (requerido).</param>
    /// <param name="description">Descripción detallada (opcional).</param>
    /// <param name="userId">ID del usuario propietario.</param>
    /// <param name="dueDate">Fecha límite (opcional).</param>
    /// <param name="priority">Prioridad (default: Medium).</param>
    /// <returns>Nueva instancia de TaskItem válida.</returns>
    /// <exception cref="DomainException">Si los parámetros son inválidos.</exception>
    /// <remarks>
    /// PATRÓN FACTORY METHOD:
    ///
    /// Centralizamos la creación para:
    /// ✅ Validar parámetros requeridos
    /// ✅ Inicializar con valores por defecto seguros
    /// ✅ Garantizar que el objeto siempre está en estado válido
    /// ✅ Facilitar cambios futuros en lógica de creación
    ///
    /// VALIDACIONES:
    /// - title: no puede ser null o vacío (validación básica aquí)
    /// - userId: no puede ser Guid.Empty
    /// - Validaciones detalladas (longitud, formato) en Application layer
    ///
    /// VALORES POR DEFECTO:
    /// - Status: Pending (toda tarea nueva comienza aquí)
    /// - Priority: Medium (si no se especifica)
    /// - DueDate: null (si no se especifica)
    /// - Description: null (opcional)
    ///
    /// EJEMPLO DE USO:
    ///
    /// // En CreateTaskCommandHandler
    /// var task = TaskItem.Create(
    ///     title: request.Title,
    ///     description: request.Description,
    ///     userId: _currentUser.UserId,
    ///     dueDate: request.DueDate,
    ///     priority: request.Priority
    /// );
    ///
    /// _context.Tasks.Add(task);
    /// await _context.SaveChangesAsync();
    ///
    /// RESPONSABILIDADES:
    /// - Domain layer: Validaciones básicas (not null, not empty)
    /// - Application layer: Validaciones de negocio (longitud, formato, XSS)
    /// - Infrastructure layer: Persistencia
    ///
    /// ALTERNATIVA - BUILDER PATTERN:
    ///
    /// Para entidades con muchos parámetros opcionales:
    /// var task = TaskItem.Builder()
    ///     .WithTitle("Title")
    ///     .WithDescription("Desc")
    ///     .WithUserId(userId)
    ///     .WithPriority(Priority.High)
    ///     .Build();
    ///
    /// Pero para TaskItem, factory method es suficiente.
    /// </remarks>
    public static TaskItem Create(
        string title,
        string? description,
        Guid userId,
        DateTime? dueDate = null,
        TaskPriority priority = TaskPriority.Medium)
    {
        // Validación 1: Título requerido
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Task title cannot be empty");
        }

        // Validación 2: UserId válido
        if (userId == Guid.Empty)
        {
            throw new DomainException("Task must have a valid user ID");
        }

        // Crear tarea con valores por defecto seguros
        return new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(), // Limpiar espacios
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            UserId = userId,
            DueDate = dueDate,
            Priority = priority,
            Status = Enums.TaskStatus.Pending, // Estado inicial
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Actualiza el título de la tarea.
    /// </summary>
    /// <param name="newTitle">Nuevo título (requerido).</param>
    /// <exception cref="DomainException">Si el título es inválido.</exception>
    /// <remarks>
    /// ENCAPSULACIÓN:
    ///
    /// En lugar de permitir task.Title = newTitle, forzamos el uso de este método.
    /// Beneficios:
    /// ✅ Validación centralizada
    /// ✅ UpdatedAt se actualiza automáticamente
    /// ✅ Posibilidad de agregar lógica adicional (eventos, logging)
    /// ✅ Previene estados inválidos
    ///
    /// VALIDACIÓN:
    /// Validación básica aquí (not null/empty).
    /// Validaciones detalladas en Application layer (longitud, XSS).
    ///
    /// EJEMPLO DE USO:
    /// var task = await _context.Tasks.FindAsync(taskId);
    /// task.UpdateTitle(request.NewTitle);
    /// await _context.SaveChangesAsync();
    /// </remarks>
    public void UpdateTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
        {
            throw new DomainException("Task title cannot be empty");
        }

        Title = newTitle.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza la descripción de la tarea.
    /// </summary>
    /// <param name="newDescription">Nueva descripción (puede ser null).</param>
    /// <remarks>
    /// Permite establecer descripción a null (eliminar descripción existente).
    /// Espacios en blanco se convierten a null.
    /// </remarks>
    public void UpdateDescription(string? newDescription)
    {
        Description = string.IsNullOrWhiteSpace(newDescription) ? null : newDescription.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza la fecha límite de la tarea.
    /// </summary>
    /// <param name="newDueDate">Nueva fecha límite (puede ser null para quitar).</param>
    /// <remarks>
    /// Permite:
    /// - Establecer fecha límite si no tenía
    /// - Cambiar fecha límite existente
    /// - Quitar fecha límite (newDueDate = null)
    ///
    /// Validación de fecha futura se hace en Application layer.
    /// </remarks>
    public void UpdateDueDate(DateTime? newDueDate)
    {
        DueDate = newDueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza la prioridad de la tarea.
    /// </summary>
    /// <param name="newPriority">Nueva prioridad.</param>
    /// <remarks>
    /// Común que la prioridad cambie:
    /// - Medium → High cuando se acerca deadline
    /// - High → Low si ya no es urgente
    ///
    /// LOGGING RECOMENDADO:
    /// Si la prioridad cambia, considerar loguear para análisis:
    /// _logger.LogInformation(
    ///     "Task {TaskId} priority changed from {Old} to {New}",
    ///     Id, Priority, newPriority);
    /// </remarks>
    public void UpdatePriority(TaskPriority newPriority)
    {
        Priority = newPriority;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza el estado de la tarea.
    /// </summary>
    /// <param name="newStatus">Nuevo estado.</param>
    /// <remarks>
    /// TRANSICIONES SIN RESTRICCIONES:
    ///
    /// Permitimos cambiar de cualquier estado a cualquier otro.
    /// El usuario decide el flujo de su tarea.
    ///
    /// Si el negocio requiere validaciones:
    /// public void UpdateStatus(TaskStatus newStatus)
    /// {
    ///     if (Status == TaskStatus.Completed && newStatus == TaskStatus.InProgress)
    ///         throw new DomainException("Cannot reopen completed task to in-progress. Set to Pending first.");
    ///
    ///     Status = newStatus;
    ///     UpdatedAt = DateTime.UtcNow;
    /// }
    ///
    /// EVENTOS DE DOMINIO (FUTURO):
    /// if (Status != newStatus)
    /// {
    ///     AddDomainEvent(new TaskStatusChangedEvent(Id, Status, newStatus));
    ///     Status = newStatus;
    /// }
    ///
    /// MÉTRICAS:
    /// Rastrear transiciones para calcular cycle time, lead time.
    /// Considerar tabla TaskStatusHistory para historial completo.
    /// </remarks>
    public void UpdateStatus(Enums.TaskStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza múltiples propiedades de la tarea a la vez.
    /// </summary>
    /// <remarks>
    /// MÉTODO DE CONVENIENCIA:
    ///
    /// En lugar de llamar a 5 métodos individuales, este método permite
    /// actualizar todo en una sola operación.
    ///
    /// Útil para endpoints de Update completo (PUT).
    ///
    /// Ventajas:
    /// ✅ Menos código en handlers
    /// ✅ UpdatedAt se actualiza solo una vez
    /// ✅ Validaciones centralizadas
    ///
    /// EJEMPLO DE USO:
    /// task.Update(
    ///     title: request.Title,
    ///     description: request.Description,
    ///     dueDate: request.DueDate,
    ///     priority: request.Priority,
    ///     status: request.Status
    /// );
    ///
    /// ALTERNATIVA - PATCH:
    /// Para updates parciales (PATCH), usar métodos individuales:
    /// if (request.Title != null) task.UpdateTitle(request.Title);
    /// if (request.Priority.HasValue) task.UpdatePriority(request.Priority.Value);
    /// </remarks>
    public void Update(
        string title,
        string? description,
        DateTime? dueDate,
        TaskPriority priority,
        Enums.TaskStatus status)
    {
        UpdateTitle(title); // Valida que no sea vacío
        UpdateDescription(description);
        UpdateDueDate(dueDate);
        UpdatePriority(priority);
        UpdateStatus(status);
    }

    /// <summary>
    /// Marca la tarea como completada.
    /// </summary>
    /// <remarks>
    /// MÉTODO DE CONVENIENCIA:
    ///
    /// Más expresivo que task.UpdateStatus(TaskStatus.Completed).
    ///
    /// FUTURAS EXPANSIONES:
    /// - Guardar CompletedAt timestamp (para métricas)
    /// - Disparar evento TaskCompletedEvent
    /// - Verificar que todas las subtasks están completadas
    /// - Calcular puntos/recompensas (gamification)
    ///
    /// EJEMPLO CON VALIDACIÓN:
    /// public void Complete()
    /// {
    ///     if (Status == TaskStatus.Completed)
    ///         throw new DomainException("Task is already completed");
    ///
    ///     Status = TaskStatus.Completed;
    ///     CompletedAt = DateTime.UtcNow;
    ///     UpdatedAt = DateTime.UtcNow;
    ///
    ///     AddDomainEvent(new TaskCompletedEvent(Id, UserId));
    /// }
    /// </remarks>
    public void Complete()
    {
        UpdateStatus(Enums.TaskStatus.Completed);
    }

    /// <summary>
    /// Marca la tarea como en progreso.
    /// </summary>
    /// <remarks>
    /// Más expresivo que task.UpdateStatus(TaskStatus.InProgress).
    ///
    /// FUTURAS EXPANSIONES:
    /// - Guardar StartedAt timestamp (para cycle time)
    /// - Disparar evento TaskStartedEvent
    /// - Validar que no hay demasiadas tareas InProgress (WIP limit)
    /// </remarks>
    public void Start()
    {
        UpdateStatus(Enums.TaskStatus.InProgress);
    }

    /// <summary>
    /// Reabre una tarea completada (vuelve a Pending).
    /// </summary>
    /// <remarks>
    /// Útil cuando se descubre que la tarea no estaba realmente completa
    /// o necesita trabajo adicional.
    ///
    /// EJEMPLO:
    /// "Bug reportado nuevamente después de marcar tarea como completa"
    ///
    /// FUTURAS EXPANSIONES:
    /// - Agregar razón/comentario obligatorio al reabrir
    /// - Notificar a stakeholders
    /// - Rastrear cuántas veces se reabre una tarea (métrica de calidad)
    /// </remarks>
    public void Reopen()
    {
        UpdateStatus(Enums.TaskStatus.Pending);
    }
}
