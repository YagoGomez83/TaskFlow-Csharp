using FluentValidation;
using TaskManagement.Application.UseCases.Auth.Commands;

namespace TaskManagement.Application.Validators.Auth;

/// <summary>
/// Validator para RefreshTokenCommand.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE REFRESH TOKEN VALIDATION:
///
/// Para refresh token, las validaciones son simples:
/// - RefreshToken: no vacío
///
/// Las validaciones complejas se hacen en el handler:
/// - Token existe en BD
/// - Token no expirado
/// - Token no usado (token rotation)
/// - Token no revocado
/// - Usuario asociado existe
///
/// ¿POR QUÉ VALIDACIONES SIMPLES AQUÍ?
///
/// Validator: Valida FORMATO de datos
/// Handler: Valida REGLAS DE NEGOCIO
///
/// Formato vs Negocio:
///
/// FORMATO (Validator):
/// - ¿String está vacío?
/// - ¿Email tiene formato correcto?
/// - ¿Número está en rango?
/// → Verificable sin contexto externo
/// → Rápido (sin I/O)
/// → Fail-fast
///
/// NEGOCIO (Handler):
/// - ¿Email existe en BD?
/// - ¿Password es correcta?
/// - ¿Token es válido?
/// → Requiere contexto (BD, servicios)
/// → Puede ser lento (I/O)
/// → Lógica de dominio
///
/// EJEMPLO - REFRESH TOKEN:
///
/// ✅ Validator: Verificar que token no está vacío
/// if (string.IsNullOrEmpty(token)) → Error
///
/// ✅ Handler: Verificar que token es válido
/// var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
/// if (refreshToken == null) → Error "Invalid token"
/// if (refreshToken.IsExpired()) → Error "Token expired"
/// if (refreshToken.IsUsed) → Error "Token already used"
/// if (refreshToken.IsRevoked) → Error "Token revoked"
///
/// VALIDACIONES EN HANDLER:
///
/// public async Task<Result<AuthResponse>> Handle(
///     RefreshTokenCommand request,
///     CancellationToken ct)
/// {
///     // 1. Buscar token en BD
///     var refreshToken = await _context.RefreshTokens
///         .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, ct);
///
///     if (refreshToken == null)
///         return Result.Failure<AuthResponse>("Invalid refresh token");
///
///     // 2. Validar estado del token
///     if (!refreshToken.IsValid())
///         return Result.Failure<AuthResponse>("Refresh token is not valid");
///
///     // 3. Detectar reuso (seguridad)
///     if (refreshToken.IsUsed)
///     {
///         _logger.LogWarning("Refresh token {TokenId} reused - possible theft", refreshToken.Id);
///         await RevokeTokenFamily(refreshToken.Id);
///         return Result.Failure<AuthResponse>("Refresh token has been revoked due to suspicious activity");
///     }
///
///     // 4. Obtener usuario
///     var user = await _context.Users.FindAsync(refreshToken.UserId);
///     if (user == null)
///         return Result.Failure<AuthResponse>("User not found");
///
///     // 5. Generar nuevos tokens...
/// }
///
/// ALTERNATIVA - VALIDAR EN VALIDATOR (no recomendado):
///
/// public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
/// {
///     private readonly IApplicationDbContext _context;
///
///     public RefreshTokenCommandValidator(IApplicationDbContext context)
///     {
///         _context = context;
///
///         RuleFor(x => x.RefreshToken)
///             .NotEmpty()
///             .MustAsync(BeValidToken)
///             .WithMessage("Invalid or expired refresh token");
///     }
///
///     private async Task<bool> BeValidToken(string token, CancellationToken ct)
///     {
///         var refreshToken = await _context.RefreshTokens
///             .FirstOrDefaultAsync(t => t.Token == token, ct);
///
///         return refreshToken != null && refreshToken.IsValid();
///     }
/// }
///
/// Problemas con este enfoque:
///
/// 1. DUPLICACIÓN DE QUERIES:
///    - Validator ejecuta query para verificar
///    - Handler ejecuta query para obtener
///    - 2 queries a BD en lugar de 1
///
/// 2. VIOLACIÓN DE SRP:
///    - Validator tiene lógica de negocio
///    - Acoplamiento con dominio (IsValid())
///    - No es solo validación de formato
///
/// 3. MENSAJES GENÉRICOS:
///    - "Invalid or expired refresh token"
///    - Handler no puede dar mensaje específico
///    - Peor UX
///
/// 4. FALTA DE CONTEXTO:
///    - Validator no tiene acceso a logger
///    - No puede loguear reuso de token (seguridad)
///    - No puede revocar token family
///
/// POR ESO: Solo validar formato en validator, lógica en handler.
///
/// CASOS DE USO:
///
/// ✅ Validar en Validator:
/// - Email no vacío
/// - Email formato válido
/// - Password longitud mínima
/// - Números en rango
/// - Strings no vacíos
///
/// ✅ Validar en Handler:
/// - Email existe en BD
/// - Password es correcta
/// - Token es válido
/// - Usuario tiene permisos
/// - Reglas de negocio complejas
///
/// TESTING:
///
/// [Fact]
/// public void Validate_EmptyToken_ReturnsError()
/// {
///     // Arrange
///     var validator = new RefreshTokenCommandValidator();
///     var command = new RefreshTokenCommand { RefreshToken = "" };
///
///     // Act
///     var result = validator.Validate(command);
///
///     // Assert
///     Assert.False(result.IsValid);
///     Assert.Contains(result.Errors, e => e.PropertyName == nameof(RefreshTokenCommand.RefreshToken));
///     Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("required"));
/// }
///
/// [Fact]
/// public void Validate_NullToken_ReturnsError()
/// {
///     var validator = new RefreshTokenCommandValidator();
///     var command = new RefreshTokenCommand { RefreshToken = null! };
///
///     var result = validator.Validate(command);
///
///     Assert.False(result.IsValid);
/// }
///
/// [Fact]
/// public void Validate_ValidToken_Passes()
/// {
///     var validator = new RefreshTokenCommandValidator();
///     var command = new RefreshTokenCommand { RefreshToken = "valid-token-string" };
///
///     var result = validator.Validate(command);
///
///     Assert.True(result.IsValid);
/// }
///
/// INTEGRACIÓN CON VALIDATIONBEHAVIOR:
///
/// ValidationBehavior automáticamente:
/// 1. Busca IValidator<RefreshTokenCommand>
/// 2. Encuentra RefreshTokenCommandValidator
/// 3. Ejecuta Validate() o ValidateAsync()
/// 4. Si falla: retorna Result.Failure con errores
/// 5. Si pasa: continúa al handler
///
/// NO necesitas código manual de validación.
///
/// MENSAJES DE ERROR:
///
/// Si RefreshToken está vacío:
/// {
///   "error": "Refresh token is required"
/// }
///
/// Cliente puede mostrar mensaje al usuario.
/// O simplemente redirigir a login sin mostrar error.
///
/// FRONTEND:
///
/// async function refreshAccessToken() {
///   const refreshToken = localStorage.getItem('refreshToken');
///
///   if (!refreshToken) {
///     // No hay refresh token, redirigir a login
///     window.location.href = '/login';
///     return;
///   }
///
///   try {
///     const response = await fetch('/api/auth/refresh', {
///       method: 'POST',
///       headers: { 'Content-Type': 'application/json' },
///       body: JSON.stringify({ refreshToken })
///     });
///
///     if (!response.ok) {
///       // Refresh falló, limpiar storage y redirigir
///       localStorage.clear();
///       window.location.href = '/login';
///       return;
///     }
///
///     const authResponse = await response.json();
///     localStorage.setItem('accessToken', authResponse.accessToken);
///     localStorage.setItem('refreshToken', authResponse.refreshToken);
///   } catch (error) {
///     localStorage.clear();
///     window.location.href = '/login';
///   }
/// }
///
/// Generalmente NO mostrar error al usuario.
/// Si refresh falla, simplemente redirigir a login.
///
/// SEGURIDAD:
///
/// Refresh token es crítico para seguridad:
/// - No loguear token completo
/// - Detectar reuso (token rotation)
/// - Revocar familia si reuso detectado
/// - Limpiar tokens expirados periódicamente
///
/// Esto se maneja en handler, no en validator.
/// </remarks>
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>
    /// Constructor que define reglas de validación.
    /// </summary>
    /// <remarks>
    /// Solo valida que RefreshToken no esté vacío.
    /// Validaciones complejas (existencia, expiración, etc.) se hacen en handler.
    ///
    /// Esto es intencional:
    /// - Validator: Formato de datos
    /// - Handler: Reglas de negocio
    /// - Evita duplicación de queries
    /// - Permite mensajes de error específicos en handler
    /// </remarks>
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required");
    }
}
