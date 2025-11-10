namespace TaskManagement.Application.Common.Interfaces;

/// <summary>
/// Define el contrato para obtener información del usuario actual autenticado.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE CURRENT USER SERVICE:
///
/// En una aplicación web, necesitamos saber QUÉ usuario está haciendo un request.
/// Esta información viene del token JWT en el Authorization header.
///
/// FLUJO COMPLETO:
///
/// 1. Cliente envía request con token:
///    GET /api/tasks
///    Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
///
/// 2. Middleware de autenticación valida token:
///    - Verifica firma
///    - Verifica expiración
///    - Extrae claims (userId, email, role)
///    - Crea ClaimsPrincipal con los claims
///    - Asigna a HttpContext.User
///
/// 3. Controller/Handler necesita saber el userId:
///    - Opción 1 ❌: Acceder directamente a HttpContext.User (acoplamiento a infraestructura)
///    - Opción 2 ✅: Usar ICurrentUserService (abstracción)
///
/// PROBLEMA SIN ABSTRACCIÓN:
///
/// ❌ Application Layer accede directamente a HttpContext:
///
/// public class CreateTaskCommandHandler
/// {
///     private readonly IHttpContextAccessor _httpContext;  // ← Acoplamiento a HTTP
///
///     public async Task<Result> Handle(Command request)
///     {
///         var userId = _httpContext.HttpContext.User
///             .FindFirst(ClaimTypes.NameIdentifier)?.Value;
///
///         // Crear tarea para este usuario...
///     }
/// }
///
/// Problemas:
/// - Application Layer depende de HTTP (viola Clean Architecture)
/// - Difícil de testear (necesitas mockear HttpContext completo)
/// - No funciona en contextos no-HTTP (background jobs, tests)
/// - Lógica de extracción de claims repetida en múltiples lugares
///
/// ✅ SOLUCIÓN CON ABSTRACCIÓN:
///
/// public class CreateTaskCommandHandler
/// {
///     private readonly ICurrentUserService _currentUser;  // ← Abstracción
///
///     public async Task<Result> Handle(Command request)
///     {
///         var userId = _currentUser.UserId;  // ← Simple y directo
///
///         // Crear tarea para este usuario...
///     }
/// }
///
/// Ventajas:
/// - Application Layer NO depende de HTTP
/// - Fácil de testear (mock simple: return userId)
/// - Funciona en cualquier contexto
/// - Lógica de extracción centralizada
///
/// IMPLEMENTACIÓN (en Infrastructure Layer):
///
/// public class CurrentUserService : ICurrentUserService
/// {
///     private readonly IHttpContextAccessor _httpContextAccessor;
///
///     public CurrentUserService(IHttpContextAccessor httpContextAccessor)
///     {
///         _httpContextAccessor = httpContextAccessor;
///     }
///
///     public Guid UserId
///     {
///         get
///         {
///             var userIdClaim = _httpContextAccessor.HttpContext?.User
///                 .FindFirstValue(ClaimTypes.NameIdentifier);
///
///             if (string.IsNullOrEmpty(userIdClaim))
///                 throw new UnauthorizedAccessException("User is not authenticated");
///
///             return Guid.Parse(userIdClaim);
///         }
///     }
///
///     public string Email
///     {
///         get
///         {
///             var emailClaim = _httpContextAccessor.HttpContext?.User
///                 .FindFirstValue(ClaimTypes.Email);
///
///             if (string.IsNullOrEmpty(emailClaim))
///                 throw new UnauthorizedAccessException("User is not authenticated");
///
///             return emailClaim;
///         }
///     }
///
///     public string Role
///     {
///         get
///         {
///             var roleClaim = _httpContextAccessor.HttpContext?.User
///                 .FindFirstValue(ClaimTypes.Role);
///
///             return roleClaim ?? "User";  // Default a User si no hay claim
///         }
///     }
///
///     public bool IsAuthenticated =>
///         _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
///
///     public bool IsInRole(string role) =>
///         _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
/// }
///
/// REGISTRO EN DI CONTAINER:
///
/// builder.Services.AddHttpContextAccessor();  // ← Necesario para acceder a HttpContext
/// builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
///
/// IMPORTANTE: Scoped lifetime (una instancia por request HTTP).
/// NO usar Singleton (HttpContext cambia por request).
///
/// EJEMPLO DE USO - CREATE TASK:
///
/// public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Result<TaskDto>>
/// {
///     private readonly IApplicationDbContext _context;
///     private readonly ICurrentUserService _currentUser;
///     private readonly IMapper _mapper;
///
///     public CreateTaskCommandHandler(
///         IApplicationDbContext context,
///         ICurrentUserService currentUser,
///         IMapper mapper)
///     {
///         _context = context;
///         _currentUser = currentUser;
///         _mapper = mapper;
///     }
///
///     public async Task<Result<TaskDto>> Handle(
///         CreateTaskCommand request,
///         CancellationToken cancellationToken)
///     {
///         // 1. Obtener userId del token JWT (automático, sin pasar en request)
///         var userId = _currentUser.UserId;
///
///         // 2. Crear tarea asociada a este usuario
///         var task = TaskItem.Create(
///             request.Title,
///             request.Description,
///             userId,  // ← Usuario autenticado
///             request.DueDate,
///             request.Priority
///         );
///
///         _context.Tasks.Add(task);
///         await _context.SaveChangesAsync(cancellationToken);
///
///         var dto = _mapper.Map<TaskDto>(task);
///         return Result.Success(dto);
///     }
/// }
///
/// IMPORTANTE: UserId viene del token, NO del request body.
///
/// ❌ NO HACER (inseguro):
/// public class CreateTaskCommand
/// {
///     public Guid UserId { get; set; }  // ← Cliente puede falsificar esto!
/// }
///
/// Atacante podría enviar:
/// POST /api/tasks
/// { "userId": "otro-usuario-guid", "title": "Tarea maliciosa" }
///
/// Y crear tareas para otros usuarios.
///
/// ✅ HACER (seguro):
/// public class CreateTaskCommand
/// {
///     // Sin UserId, se obtiene del token
///     public string Title { get; set; }
///     public string Description { get; set; }
/// }
///
/// En el Handler:
/// var userId = _currentUser.UserId;  // Del token (no puede ser falsificado)
///
/// EJEMPLO DE USO - GET TASKS:
///
/// public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, Result<PaginatedList<TaskDto>>>
/// {
///     private readonly IApplicationDbContext _context;
///     private readonly ICurrentUserService _currentUser;
///
///     public async Task<Result<PaginatedList<TaskDto>>> Handle(
///         GetTasksQuery request,
///         CancellationToken cancellationToken)
///     {
///         var userId = _currentUser.UserId;
///
///         // Usuario solo puede ver SUS tareas (a menos que sea Admin)
///         var query = _context.Tasks.Where(t => t.UserId == userId);
///
///         // Si es Admin, puede ver todas las tareas
///         if (_currentUser.IsInRole("Admin"))
///             query = _context.Tasks;  // Sin filtro
///
///         // Aplicar filtros adicionales, ordenar, paginar...
///         var paginatedTasks = await PaginatedList<TaskDto>.CreateAsync(
///             query.Select(t => _mapper.Map<TaskDto>(t)),
///             request.Page,
///             request.PageSize
///         );
///
///         return Result.Success(paginatedTasks);
///     }
/// }
///
/// EJEMPLO DE USO - AUTHORIZATION CHECK:
///
/// public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Result>
/// {
///     private readonly IApplicationDbContext _context;
///     private readonly ICurrentUserService _currentUser;
///
///     public async Task<Result> Handle(
///         DeleteTaskCommand request,
///         CancellationToken cancellationToken)
///     {
///         var task = await _context.Tasks.FindAsync(request.TaskId);
///
///         if (task == null)
///             return Result.Failure("Task not found");
///
///         // IMPORTANTE: Verificar que el usuario sea dueño de la tarea
///         if (task.UserId != _currentUser.UserId && !_currentUser.IsInRole("Admin"))
///             return Result.Failure("You don't have permission to delete this task");
///
///         task.Delete();  // Soft delete
///         await _context.SaveChangesAsync(cancellationToken);
///
///         return Result.Success();
///     }
/// }
///
/// TESTING:
///
/// Fácil de mockear en tests:
///
/// [Fact]
/// public async Task CreateTask_CreatesTaskForCurrentUser()
/// {
///     // Arrange
///     var userId = Guid.NewGuid();
///
///     var mockCurrentUser = new Mock<ICurrentUserService>();
///     mockCurrentUser.Setup(x => x.UserId).Returns(userId);
///     mockCurrentUser.Setup(x => x.Email).Returns("test@example.com");
///     mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
///
///     var handler = new CreateTaskCommandHandler(
///         mockContext.Object,
///         mockCurrentUser.Object,
///         mockMapper.Object
///     );
///
///     var command = new CreateTaskCommand { Title = "Test Task" };
///
///     // Act
///     var result = await handler.Handle(command, CancellationToken.None);
///
///     // Assert
///     Assert.True(result.IsSuccess);
///     mockContext.Verify(x => x.Tasks.Add(
///         It.Is<TaskItem>(t => t.UserId == userId)  // ← Verifica userId correcto
///     ), Times.Once);
/// }
///
/// CONTEXTOS NO-HTTP:
///
/// ¿Qué pasa si necesitas ejecutar código fuera de HTTP context?
///
/// Ejemplo: Background job que limpia tareas antiguas
///
/// public class CleanupOldTasksJob
/// {
///     private readonly IApplicationDbContext _context;
///     // NO inyectar ICurrentUserService aquí (no hay usuario en background job)
///
///     public async Task Execute()
///     {
///         var oldTasks = await _context.Tasks
///             .Where(t => t.CreatedAt < DateTime.UtcNow.AddYears(-1))
///             .ToListAsync();
///
///         foreach (var task in oldTasks)
///             task.Delete();
///
///         await _context.SaveChangesAsync();
///     }
/// }
///
/// En este caso, NO hay usuario autenticado (es un background job).
/// No uses ICurrentUserService aquí.
///
/// ALTERNATIVA - IMPERSONATION:
///
/// Si necesitas ejecutar como un usuario específico:
///
/// public class ImpersonatedCurrentUserService : ICurrentUserService
/// {
///     private readonly Guid _userId;
///     private readonly string _email;
///     private readonly string _role;
///
///     public ImpersonatedCurrentUserService(Guid userId, string email, string role)
///     {
///         _userId = userId;
///         _email = email;
///         _role = role;
///     }
///
///     public Guid UserId => _userId;
///     public string Email => _email;
///     public string Role => _role;
///     public bool IsAuthenticated => true;
///     public bool IsInRole(string role) => _role == role;
/// }
///
/// Uso en background job:
/// var adminUser = await _context.Users.FirstAsync(u => u.Role == UserRole.Admin);
/// var impersonatedService = new ImpersonatedCurrentUserService(
///     adminUser.Id,
///     adminUser.Email.Value,
///     "Admin"
/// );
///
/// var handler = new DeleteTaskCommandHandler(_context, impersonatedService);
/// await handler.Handle(new DeleteTaskCommand { TaskId = taskId });
///
/// SEGURIDAD:
///
/// NUNCA confíes en datos del cliente para identificar al usuario:
///
/// ❌ Inseguro:
/// [HttpPost]
/// public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
/// {
///     // Cliente envía userId en el body
///     var task = TaskItem.Create(request.Title, request.UserId);
///     // ← Atacante puede crear tareas para otros usuarios!
/// }
///
/// ✅ Seguro:
/// [HttpPost]
/// [Authorize]  // ← Requiere autenticación
/// public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
/// {
///     var userId = _currentUser.UserId;  // Del token JWT
///     var task = TaskItem.Create(request.Title, userId);
///     // ← UserId viene del token, no puede ser falsificado
/// }
///
/// PERFORMANCE:
///
/// Acceder a _currentUser.UserId es muy rápido:
/// - Solo lee claims del HttpContext.User (en memoria)
/// - No hay query a base de datos
/// - No hay overhead significativo
///
/// Puedes llamarlo múltiples veces en el mismo request sin problema:
/// var userId1 = _currentUser.UserId;
/// var userId2 = _currentUser.UserId;
/// // Mismo valor, sin overhead
///
/// NULLABLE vs THROW:
///
/// Hay dos estrategias para cuando no hay usuario autenticado:
///
/// 1. Throw exception (este proyecto):
///    public Guid UserId
///    {
///        get
///        {
///            var claim = ...;
///            if (claim == null)
///                throw new UnauthorizedAccessException();
///            return Guid.Parse(claim);
///        }
///    }
///
///    - Pro: Fail-fast, error claro
///    - Con: Exception si olvidas [Authorize]
///
/// 2. Nullable (alternativa):
///    public Guid? UserId { get; }
///
///    - Pro: No lanza exception
///    - Con: Necesitas chequear null en todos lados
///
/// Preferimos opción 1 porque:
/// - [Authorize] siempre debe estar presente
/// - Si falta, es un bug que queremos detectar temprano
/// - Mejor fail-fast que comportamiento inesperado
/// </remarks>
public interface ICurrentUserService
{
    /// <summary>
    /// ID del usuario autenticado actualmente.
    /// </summary>
    /// <remarks>
    /// Extrae el claim "sub" (Subject) del token JWT.
    ///
    /// El claim "sub" contiene el Guid del usuario:
    /// {
    ///   "sub": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    ///   ...
    /// }
    ///
    /// Uso:
    /// var userId = _currentUser.UserId;
    /// var task = TaskItem.Create(title, description, userId);
    ///
    /// IMPORTANTE:
    /// - Lanza UnauthorizedAccessException si no hay usuario autenticado
    /// - Siempre usar [Authorize] en controllers para garantizar autenticación
    /// - NO confiar en userId del request body (usar siempre del token)
    ///
    /// Testing:
    /// var mock = new Mock<ICurrentUserService>();
    /// mock.Setup(x => x.UserId).Returns(Guid.Parse("..."));
    /// </remarks>
    Guid UserId { get; }

    /// <summary>
    /// Email del usuario autenticado actualmente.
    /// </summary>
    /// <remarks>
    /// Extrae el claim "email" del token JWT.
    ///
    /// Útil para:
    /// - Logs de auditoría
    /// - Envío de notificaciones
    /// - Mostrar información del usuario
    ///
    /// Uso:
    /// _logger.LogInformation($"User {_currentUser.Email} created a task");
    ///
    /// IMPORTANTE:
    /// - Lanza UnauthorizedAccessException si no hay usuario autenticado
    /// - Email viene del token, no de la base de datos
    /// - Si usuario cambió email, token viejo tiene email antiguo
    ///   (hasta que token expire y se genere uno nuevo)
    /// </remarks>
    string Email { get; }

    /// <summary>
    /// Rol del usuario autenticado actualmente.
    /// </summary>
    /// <remarks>
    /// Extrae el claim "role" del token JWT.
    ///
    /// Valores posibles:
    /// - "User": Usuario normal
    /// - "Admin": Administrador
    ///
    /// Uso:
    /// if (_currentUser.Role == "Admin")
    /// {
    ///     // Permitir acción solo a admins
    /// }
    ///
    /// Mejor usar IsInRole() para chequeos:
    /// if (_currentUser.IsInRole("Admin"))
    /// {
    ///     // Más expresivo
    /// }
    ///
    /// IMPORTANTE:
    /// - Si usuario no tiene claim de rol, retorna "User" por defecto
    /// - Rol viene del token, no de la base de datos
    /// - Si rol cambió en BD, token viejo tiene rol antiguo
    ///   (hasta que token expire)
    /// </remarks>
    string Role { get; }

    /// <summary>
    /// Indica si hay un usuario autenticado.
    /// </summary>
    /// <remarks>
    /// Verifica si HttpContext.User.Identity.IsAuthenticated == true
    ///
    /// Uso:
    /// if (!_currentUser.IsAuthenticated)
    ///     return Unauthorized();
    ///
    /// IMPORTANTE:
    /// En la mayoría de casos, NO necesitas esto porque:
    /// - [Authorize] en controller ya garantiza autenticación
    /// - Si llegas al handler, el usuario YA está autenticado
    ///
    /// Solo útil en casos especiales:
    /// - Endpoints que permiten anónimos y autenticados
    /// - Lógica diferente según autenticación
    ///
    /// Ejemplo:
    /// [AllowAnonymous]
    /// public class GetTasksQueryHandler
    /// {
    ///     public async Task<Result> Handle(...)
    ///     {
    ///         if (_currentUser.IsAuthenticated)
    ///         {
    ///             // Mostrar tareas del usuario
    ///             return await GetUserTasks(_currentUser.UserId);
    ///         }
    ///         else
    ///         {
    ///             // Mostrar tareas públicas
    ///             return await GetPublicTasks();
    ///         }
    ///     }
    /// }
    /// </remarks>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Verifica si el usuario tiene un rol específico.
    /// </summary>
    /// <param name="role">Nombre del rol a verificar.</param>
    /// <returns>true si el usuario tiene el rol, false si no.</returns>
    /// <remarks>
    /// Chequea si el claim "role" coincide con el rol especificado.
    ///
    /// Uso:
    /// if (_currentUser.IsInRole("Admin"))
    /// {
    ///     // Lógica solo para admins
    ///     query = _context.Tasks;  // Ver todas las tareas
    /// }
    /// else
    /// {
    ///     // Lógica para usuarios normales
    ///     query = _context.Tasks.Where(t => t.UserId == _currentUser.UserId);
    /// }
    ///
    /// ALTERNATIVA - [Authorize(Roles = "Admin")]:
    ///
    /// En controllers, puedes usar atributo:
    /// [Authorize(Roles = "Admin")]
    /// public async Task<IActionResult> DeleteUser(Guid userId)
    /// {
    ///     // Solo admins pueden ejecutar esto
    /// }
    ///
    /// Pero en Handlers (Use Cases), usa IsInRole():
    /// public class DeleteUserCommandHandler
    /// {
    ///     public async Task<Result> Handle(...)
    ///     {
    ///         if (!_currentUser.IsInRole("Admin"))
    ///             return Result.Failure("Only admins can delete users");
    ///
    ///         // Proceder con eliminación...
    ///     }
    /// }
    ///
    /// MÚLTIPLES ROLES:
    ///
    /// Si tienes múltiples roles (User, Admin, SuperAdmin):
    /// if (_currentUser.IsInRole("Admin") || _currentUser.IsInRole("SuperAdmin"))
    /// {
    ///     // Lógica para usuarios con permisos elevados
    /// }
    ///
    /// O mejor, usar claims-based authorization con policies.
    /// </remarks>
    bool IsInRole(string role);
}
