using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.UseCases.Auth.Commands;

namespace TaskManagement.API.Controllers;

/// <summary>
/// Controller para autenticación y registro de usuarios.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE API CONTROLLERS:
///
/// Controller es el punto de entrada de la API (API Layer).
/// Recibe HTTP requests, delega a Application Layer (MediatR), retorna responses.
///
/// RESPONSABILIDADES:
///
/// 1. RECIBIR REQUEST:
///    - Parsear JSON a DTOs
///    - Validar formato básico (ModelState)
///
/// 2. DELEGAR A APPLICATION LAYER:
///    - Crear Command/Query
///    - Enviar via MediatR
///    - Esperar resultado
///
/// 3. RETORNAR RESPONSE:
///    - Convertir Result a IActionResult
///    - Status codes apropiados (200, 201, 400, 404, 401, etc.)
///    - Formatear respuesta JSON
///
/// Controller NO debe:
/// - ❌ Contener lógica de negocio
/// - ❌ Acceder directamente a base de datos
/// - ❌ Crear o modificar entidades
/// - ❌ Contener validación de negocio
///
/// Toda lógica está en Handlers (Application Layer).
///
/// ATRIBUTOS IMPORTANTES:
///
/// [ApiController]:
/// - Habilita validación automática de ModelState
/// - Retorna 400 automáticamente si ModelState es inválido
/// - Infiere [FromBody], [FromQuery], etc. automáticamente
/// - Mejora respuestas de error
///
/// [Route("api/[controller]")]:
/// - Define la ruta base: /api/auth
/// - [controller] es reemplazado por nombre del controller sin "Controller"
/// - AuthController → /api/auth
/// - TasksController → /api/tasks
///
/// [HttpPost], [HttpGet], etc.:
/// - Define el verbo HTTP
/// - Opcionalmente puede incluir ruta adicional: [HttpPost("login")]
///
/// [ProducesResponseType]:
/// - Documenta los tipos de respuesta posibles
/// - Usado por Swagger para generar documentación
/// - Especifica status code y tipo de retorno
///
/// MEDIATR PATTERN:
///
/// En lugar de inyectar múltiples handlers:
/// ❌ CreateTaskHandler, UpdateTaskHandler, DeleteTaskHandler...
///
/// Inyectamos solo IMediator:
/// ✅ IMediator
///
/// Y enviamos Commands/Queries:
/// var result = await _mediator.Send(new CreateTaskCommand(...));
///
/// MediatR rutea automáticamente al Handler correcto.
///
/// VENTAJAS:
/// - Controller limpio y simple
/// - Fácil de testear
/// - No necesita conocer todos los handlers
///
/// RESULT PATTERN EN CONTROLLER:
///
/// Handlers retornan Result<T>, Controller convierte a IActionResult:
///
/// var result = await _mediator.Send(command);
///
/// if (!result.IsSuccess)
///     return BadRequest(new { error = result.Error });
///
/// return Ok(result.Value);
///
/// Esto se puede simplificar con extension method:
///
/// return result.ToActionResult();
///
/// STATUS CODES:
///
/// 2xx - Success:
/// - 200 OK: Request exitoso (GET, PUT, DELETE)
/// - 201 Created: Recurso creado (POST)
/// - 204 No Content: Exitoso sin contenido (DELETE)
///
/// 4xx - Client Error:
/// - 400 Bad Request: Datos inválidos
/// - 401 Unauthorized: No autenticado
/// - 403 Forbidden: Autenticado pero sin permisos
/// - 404 Not Found: Recurso no existe
/// - 409 Conflict: Conflicto (ej: email duplicado)
/// - 422 Unprocessable Entity: Validación de negocio falla
///
/// 5xx - Server Error:
/// - 500 Internal Server Error: Error no manejado
///
/// CONTENT NEGOTIATION:
///
/// ASP.NET Core automáticamente:
/// - Parsea JSON de request body a DTOs
/// - Serializa DTOs a JSON en response body
///
/// Configurado en Program.cs:
/// builder.Services.AddControllers();
///
/// REQUEST/RESPONSE FLOW:
///
/// 1. Cliente envía HTTP Request:
///    POST /api/auth/register
///    Content-Type: application/json
///    {
///      "email": "user@example.com",
///      "password": "Password123!",
///      "confirmPassword": "Password123!"
///    }
///
/// 2. ASP.NET Core parsea JSON a RegisterRequest DTO
///
/// 3. [ApiController] valida ModelState (atributos de validación)
///
/// 4. Controller crea RegisterCommand
///
/// 5. MediatR encuentra RegisterCommandHandler
///
/// 6. ValidationBehavior ejecuta RegisterCommandValidator (FluentValidation)
///
/// 7. Handler ejecuta lógica de negocio
///
/// 8. Handler retorna Result<AuthResponse>
///
/// 9. Controller convierte Result a IActionResult
///
/// 10. ASP.NET Core serializa a JSON y envía respuesta:
///     HTTP/1.1 200 OK
///     Content-Type: application/json
///     {
///       "accessToken": "eyJhbGc...",
///       "refreshToken": "abc123...",
///       "expiresIn": 900,
///       "tokenType": "Bearer"
///     }
///
/// SWAGGER/OPENAPI:
///
/// [ProducesResponseType] genera documentación automática:
///
/// - Swagger UI muestra todos los endpoints
/// - Documentación de request/response schemas
/// - Posibilidad de probar endpoints desde browser
///
/// Acceso: https://localhost:5001/swagger
///
/// AUTENTICACIÓN:
///
/// Endpoints públicos (sin [Authorize]):
/// - POST /api/auth/register
/// - POST /api/auth/login
/// - POST /api/auth/refresh-token
///
/// Endpoints protegidos (con [Authorize]):
/// - Requieren JWT en header: Authorization: Bearer <token>
/// - Retornan 401 si no hay token
/// - Retornan 403 si token válido pero sin permisos
///
/// VERSIONING:
///
/// Para versionado de API, usar:
///
/// [Route("api/v1/[controller]")]  → /api/v1/auth
/// [Route("api/v2/[controller]")]  → /api/v2/auth
///
/// O con paquete Microsoft.AspNetCore.Mvc.Versioning:
///
/// [ApiVersion("1.0")]
/// [Route("api/v{version:apiVersion}/[controller]")]
///
/// RATE LIMITING:
///
/// ASP.NET Core 7+ tiene rate limiting integrado:
///
/// builder.Services.AddRateLimiter(options =>
/// {
///     options.AddFixedWindowLimiter("login", opt =>
///     {
///         opt.Window = TimeSpan.FromMinutes(1);
///         opt.PermitLimit = 5;
///     });
/// });
///
/// [EnableRateLimiting("login")]
/// public async Task<IActionResult> Login(...)
///
/// TESTING:
///
/// Testing de controllers es fácil con TestServer:
///
/// var client = _factory.CreateClient();
/// var request = new { email = "test@example.com", password = "Pass123!" };
/// var response = await client.PostAsJsonAsync("/api/auth/login", request);
///
/// Assert.Equal(HttpStatusCode.OK, response.StatusCode);
/// var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
/// Assert.NotNull(result.AccessToken);
///
/// O mockear MediatR:
///
/// var mockMediator = new Mock<IMediator>();
/// mockMediator.Setup(m => m.Send(It.IsAny<LoginCommand>(), default))
///     .ReturnsAsync(Result.Success(authResponse));
///
/// var controller = new AuthController(mockMediator.Object);
/// var result = await controller.Login(request);
///
/// MEJORES PRÁCTICAS:
///
/// 1. ✅ Controller debe ser thin (delgado)
/// 2. ✅ Toda lógica en Handlers
/// 3. ✅ Documentar con [ProducesResponseType]
/// 4. ✅ Usar DTOs para request/response
/// 5. ✅ Retornar status codes apropiados
/// 6. ✅ Validación en Validators, no en Controller
/// 7. ❌ NO poner lógica de negocio en Controller
/// 8. ❌ NO acceder directamente a DbContext
/// 9. ❌ NO manejar exceptions aquí (usar middleware)
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Constructor del AuthController.
    /// </summary>
    /// <param name="mediator">Mediator de MediatR para enviar Commands/Queries.</param>
    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Registra un nuevo usuario.
    /// </summary>
    /// <param name="request">Datos de registro (email, password, confirmPassword).</param>
    /// <returns>AuthResponse con access token y refresh token.</returns>
    /// <response code="200">Usuario registrado exitosamente.</response>
    /// <response code="400">Datos de entrada inválidos o email ya registrado.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Crear command desde DTO
        var command = new RegisterCommand(
            request.Email,
            request.Password,
            request.ConfirmPassword);

        // Enviar command via MediatR
        // ValidationBehavior ejecuta RegisterCommandValidator automáticamente
        var result = await _mediator.Send(command);

        // Convertir Result a IActionResult
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Inicia sesión con email y contraseña.
    /// </summary>
    /// <param name="request">Credenciales (email, password).</param>
    /// <returns>AuthResponse con access token y refresh token.</returns>
    /// <response code="200">Login exitoso.</response>
    /// <response code="400">Credenciales inválidas o cuenta bloqueada.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Email, request.Password);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Renueva el access token usando un refresh token.
    /// </summary>
    /// <param name="request">Refresh token.</param>
    /// <returns>AuthResponse con nuevo access token y refresh token.</returns>
    /// <response code="200">Token renovado exitosamente.</response>
    /// <response code="400">Refresh token inválido, expirado o revocado.</response>
    /// <remarks>
    /// Implementa token rotation: el refresh token anterior se invalida
    /// y se genera uno nuevo. Si un refresh token ya usado se vuelve a usar,
    /// toda la familia de tokens se revoca (detección de robo de token).
    /// </remarks>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}
