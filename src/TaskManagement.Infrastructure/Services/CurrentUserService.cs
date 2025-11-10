using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskManagement.Application.Common.Interfaces;

namespace TaskManagement.Infrastructure.Services;

/// <summary>
/// Servicio para acceder a la información del usuario autenticado actual.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE IHttpContextAccessor:
///
/// IHttpContextAccessor es un servicio que proporciona acceso al HttpContext
/// actual de ASP.NET Core desde cualquier parte de la aplicación.
///
/// ¿POR QUÉ ES NECESARIO?
///
/// En Controllers, tienes acceso directo a HttpContext:
/// public class TasksController : ControllerBase
/// {
///     public IActionResult Get()
///     {
///         var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
///     }
/// }
///
/// Pero en Handlers (Application Layer), NO tienes acceso directo:
/// public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Result<TaskDto>>
/// {
///     // ❌ NO tenemos acceso a HttpContext aquí
/// }
///
/// IHttpContextAccessor resuelve esto inyectando HttpContext en servicios.
///
/// ARQUITECTURA:
///
/// API Layer (Controllers) → Application Layer (Handlers) → Domain Layer (Entities)
///
/// Sin IHttpContextAccessor:
/// Controller extrae UserId → Pasa como parámetro a Handler
///
/// Con IHttpContextAccessor:
/// Handler obtiene UserId directamente via ICurrentUserService
///
/// VENTAJAS:
/// - Handlers no necesitan parámetros adicionales de contexto
/// - Código más limpio y DRY
/// - Testeable (puedes mockear ICurrentUserService)
///
/// REGISTRO EN Program.cs:
///
/// builder.Services.AddHttpContextAccessor();
/// builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
///
/// CLAIMS PRINCIPAL:
///
/// Después de autenticación JWT, ASP.NET Core crea un ClaimsPrincipal
/// con los claims extraídos del token.
///
/// HttpContext.User es un ClaimsPrincipal que contiene:
/// - Claims del JWT (sub, email, role, etc.)
/// - Identity (información de autenticación)
/// - IsAuthenticated (si el usuario está autenticado)
///
/// EXTRACCIÓN DE CLAIMS:
///
/// User.FindFirstValue(ClaimTypes.NameIdentifier) → ID del usuario
/// User.FindFirstValue(ClaimTypes.Email) → Email del usuario
/// User.FindFirstValue(ClaimTypes.Role) → Rol del usuario
/// User.IsInRole("Admin") → Si el usuario tiene rol Admin
///
/// CLAIM TYPES:
///
/// ClaimTypes es una clase con constantes para claims estándar:
/// - ClaimTypes.NameIdentifier → "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
/// - ClaimTypes.Email → "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
/// - ClaimTypes.Role → "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
///
/// En el JWT, estos se mapean automáticamente:
/// - "sub" → ClaimTypes.NameIdentifier
/// - "email" → ClaimTypes.Email
/// - "role" → ClaimTypes.Role
///
/// EJEMPLO DE USO EN HANDLER:
///
/// public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Result<TaskDto>>
/// {
///     private readonly IApplicationDbContext _context;
///     private readonly ICurrentUserService _currentUser;
///
///     public CreateTaskCommandHandler(
///         IApplicationDbContext context,
///         ICurrentUserService currentUser)
///     {
///         _context = context;
///         _currentUser = currentUser;
///     }
///
///     public async Task<Result<TaskDto>> Handle(CreateTaskCommand request, CancellationToken ct)
///     {
///         // UserId viene del JWT, no puede ser falsificado por el cliente
///         var userId = _currentUser.UserId;
///
///         var task = TaskItem.Create(request.Title, request.Description, userId, request.DueDate, request.Priority);
///
///         _context.Tasks.Add(task);
///         await _context.SaveChangesAsync(ct);
///
///         return Result.Success(taskDto);
///     }
/// }
///
/// SEGURIDAD:
///
/// El UserId viene del JWT validado por middleware de autenticación.
/// Cliente NO puede falsificar el UserId porque:
/// 1. JWT está firmado con clave secreta del servidor
/// 2. Cualquier modificación invalida la firma
/// 3. Middleware rechaza tokens con firma inválida
///
/// Ejemplo de intento de ataque:
/// 1. Cliente obtiene JWT válido para su usuario (ID: user-123)
/// 2. Cliente modifica el JWT para cambiar "sub" a "admin-456"
/// 3. JWT ahora tiene firma inválida
/// 4. Middleware rechaza el token
/// 5. Request es 401 Unauthorized
///
/// Por eso es SEGURO confiar en _currentUser.UserId.
///
/// AUTORIZACIÓN:
///
/// Además de autenticación (quién eres), validamos autorización (qué puedes hacer):
///
/// // Solo el dueño o Admin puede actualizar la tarea
/// if (task.UserId != _currentUser.UserId && !_currentUser.IsInRole("Admin"))
///     return Result.Failure<TaskDto>("You don't have permission");
///
/// CONTEXTO ASÍNCRONO:
///
/// IHttpContextAccessor usa AsyncLocal<T> internamente para mantener
/// HttpContext por request en contexto asíncrono.
///
/// Funciona correctamente con async/await:
/// - Cada request tiene su propio HttpContext
/// - No hay interferencia entre requests concurrentes
/// - Thread-safe
///
/// NULLABILITY:
///
/// HttpContext puede ser null si:
/// 1. No estamos en contexto de request HTTP (background jobs)
/// 2. Request no está autenticado
///
/// Por eso todas las propiedades retornan valores por defecto:
/// - UserId → Guid.Empty
/// - Email → string.Empty
/// - Role → string.Empty
/// - IsAuthenticated → false
///
/// TESTING:
///
/// Mockear ICurrentUserService es fácil:
///
/// var mockCurrentUser = new Mock<ICurrentUserService>();
/// mockCurrentUser.Setup(x => x.UserId).Returns(testUserId);
/// mockCurrentUser.Setup(x => x.Email).Returns("test@example.com");
/// mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
/// mockCurrentUser.Setup(x => x.IsInRole("Admin")).Returns(false);
///
/// var handler = new CreateTaskCommandHandler(_context, mockCurrentUser.Object);
///
/// ALTERNATIVA: Pasar UserId como parámetro
///
/// Sin ICurrentUserService:
/// public record CreateTaskCommand(string Title, string Description, Guid UserId);
///
/// Controller extrae UserId:
/// var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
/// var command = new CreateTaskCommand(request.Title, request.Description, userId);
///
/// DESVENTAJAS:
/// - Handlers están acoplados a estructura de Command
/// - Código duplicado en cada Controller
/// - Más difícil de testear
/// - Viola DRY
///
/// ICurrentUserService es más limpio y escalable.
///
/// CLAIMS ADICIONALES:
///
/// Puedes agregar claims personalizados en TokenService:
///
/// new Claim("plan", user.SubscriptionPlan.ToString()),
/// new Claim("tenant_id", user.TenantId.ToString())
///
/// Y acceder en CurrentUserService:
///
/// public string SubscriptionPlan => _httpContextAccessor.HttpContext?.User
///     .FindFirstValue("plan") ?? "Free";
///
/// PERFORMANCE:
///
/// IHttpContextAccessor tiene overhead mínimo:
/// - AsyncLocal<T> es muy eficiente
/// - FindFirstValue() es O(n) pero n es pequeño (pocos claims)
/// - Resultados pueden cachearse si necesario
///
/// Para alto rendimiento, cachear en campo:
///
/// private Guid? _cachedUserId;
/// public Guid UserId => _cachedUserId ??= ParseUserId();
///
/// MULTI-TENANCY:
///
/// Para aplicaciones multi-tenant, puedes agregar TenantId:
///
/// public Guid TenantId => Guid.Parse(
///     _httpContextAccessor.HttpContext?.User.FindFirstValue("tenant_id") ?? Guid.Empty.ToString());
///
/// Y filtrar queries por tenant:
///
/// var tasks = _context.Tasks
///     .Where(t => t.TenantId == _currentUser.TenantId)
///     .Where(t => t.UserId == _currentUser.UserId);
///
/// IMPORTANTE: Global Query Filter en DbContext
///
/// Puedes configurar filtro global de tenant:
///
/// modelBuilder.Entity<TaskItem>()
///     .HasQueryFilter(e => e.TenantId == _currentUser.TenantId);
///
/// Así todas las queries automáticamente filtran por tenant.
/// </remarks>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// ID del usuario autenticado actual.
    /// </summary>
    /// <remarks>
    /// Extrae el claim "sub" (ClaimTypes.NameIdentifier) del JWT.
    /// Retorna Guid.Empty si no está autenticado.
    /// </remarks>
    public Guid UserId
    {
        get
        {
            var userIdString = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
        }
    }

    /// <summary>
    /// Email del usuario autenticado actual.
    /// </summary>
    /// <remarks>
    /// Extrae el claim "email" (ClaimTypes.Email) del JWT.
    /// Retorna string vacío si no está autenticado.
    /// </remarks>
    public string Email =>
        _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    /// <summary>
    /// Rol del usuario autenticado actual.
    /// </summary>
    /// <remarks>
    /// Extrae el claim "role" (ClaimTypes.Role) del JWT.
    /// Retorna string vacío si no está autenticado.
    /// </remarks>
    public string Role =>
        _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    /// <summary>
    /// Indica si hay un usuario autenticado.
    /// </summary>
    /// <remarks>
    /// Verifica si HttpContext.User.Identity.IsAuthenticated es true.
    /// Esto es true después de que JWT middleware valida el token.
    /// </remarks>
    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// Verifica si el usuario actual tiene el rol especificado.
    /// </summary>
    /// <param name="role">Nombre del rol (ej: "Admin", "User").</param>
    /// <returns>True si el usuario tiene el rol, False en caso contrario.</returns>
    /// <remarks>
    /// Usa ClaimsPrincipal.IsInRole() que compara con claims de tipo Role.
    ///
    /// Ejemplo:
    /// if (_currentUser.IsInRole("Admin"))
    /// {
    ///     // Usuario es administrador
    /// }
    ///
    /// Comparación case-sensitive.
    /// </remarks>
    public bool IsInRole(string role)
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
    }
}
