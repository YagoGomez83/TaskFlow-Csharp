using MediatR;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Auth;

namespace TaskManagement.Application.UseCases.Auth.Commands;

/// <summary>
/// Command para autenticar usuario y generar tokens JWT.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE CQRS PATTERN:
///
/// CQRS = Command Query Responsibility Segregation
/// Separar operaciones de escritura (Commands) de lectura (Queries).
///
/// COMMANDS:
/// - Modifican estado del sistema (Create, Update, Delete)
/// - No retornan datos (o retornan confirmación/ID)
/// - Pueden retornar Result<T> para indicar éxito/fallo
///
/// QUERIES:
/// - Solo leen datos
/// - No modifican estado
/// - Retornan datos (DTOs)
/// - Idempotentes (múltiples ejecuciones = mismo resultado)
///
/// VENTAJAS DE CQRS:
/// - Separación clara de responsabilidades
/// - Queries optimizadas sin afectar writes
/// - Diferentes modelos para lectura/escritura
/// - Escalabilidad independiente
/// - Cache en queries sin afectar commands
///
/// MEDIATR:
///
/// MediatR es una implementación del patrón Mediator.
/// Desacopla sender (Controller) de handler (lógica).
///
/// Sin MediatR:
/// Controller → Service → Repository → DB
/// Controller tiene dependencia directa de Service
///
/// Con MediatR:
/// Controller → MediatR → Handler → Repository → DB
/// Controller solo depende de IMediator (abstracción)
///
/// IQUEST<TRESPONSE>:
///
/// LoginCommand implementa IRequest<Result<AuthResponse>>
/// - IRequest: Marca la clase como request de MediatR
/// - Result<AuthResponse>: Tipo de retorno del handler
///
/// MediatR busca IRequestHandler<LoginCommand, Result<AuthResponse>>
/// y ejecuta Handle() automáticamente.
///
/// FLUJO COMPLETO:
///
/// 1. Controller recibe LoginRequest:
///    [HttpPost("login")]
///    public async Task<IActionResult> Login([FromBody] LoginRequest request)
///
/// 2. Controller crea LoginCommand:
///    var command = new LoginCommand
///    {
///        Email = request.Email,
///        Password = request.Password
///    };
///
/// 3. Controller envía a MediatR:
///    var result = await _mediator.Send(command);
///
/// 4. MediatR encuentra handler:
///    LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
///
/// 5. MediatR ejecuta pipeline behaviors:
///    - LoggingBehavior (log inicio)
///    - ValidationBehavior (validar con LoginCommandValidator)
///    - Handler (lógica de negocio)
///    - LoggingBehavior (log fin)
///
/// 6. Handler retorna Result<AuthResponse>
///
/// 7. Controller procesa resultado:
///    if (result.IsFailure)
///        return Unauthorized(new { error = result.Error });
///    return Ok(result.Value);
///
/// COMANDO vs DTO:
///
/// LoginRequest (DTO):
/// - Contrato HTTP/API
/// - Define qué datos envía cliente
/// - Puede tener formato específico para API
///
/// LoginCommand (Command):
/// - Intención de negocio
/// - Procesado por MediatR
/// - Puede tener propiedades adicionales no expuestas en API
///
/// ¿Por qué separar?
/// - Versionado: LoginRequestV2 → LoginCommand (mismo)
/// - API puede cambiar sin afectar dominio
/// - Command puede tener metadata (CorrelationId, Timestamp, etc.)
///
/// Para simplicidad, tienen mismas propiedades.
/// </remarks>
public class LoginCommand : IRequest<Result<AuthResponse>>
{
    /// <summary>
    /// Email del usuario.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña en texto plano.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Constructor para crear comando desde controller.
    /// </summary>
    public LoginCommand(string email, string password)
    {
        Email = email;
        Password = password;
    }
}
