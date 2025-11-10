using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Common.Interfaces;

/// <summary>
/// Define el contrato para generación y validación de tokens JWT.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE JWT (JSON Web Token):
///
/// JWT es un estándar abierto (RFC 7519) para transmitir información de forma segura.
/// Se usa principalmente para autenticación y autorización en APIs.
///
/// ESTRUCTURA DE UN JWT:
///
/// Un JWT tiene 3 partes separadas por puntos (.):
///
/// eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
/// └───────────── Header ─────────────┘ └──────────────── Payload ───────────────┘ └────────────── Signature ──────────────┘
///
/// 1. HEADER (base64url encoded):
/// {
///   "alg": "HS256",      // Algoritmo de firma: HMAC SHA256
///   "typ": "JWT"         // Tipo de token
/// }
///
/// 2. PAYLOAD (base64url encoded):
/// {
///   "sub": "user-guid",         // Subject: ID del usuario
///   "email": "user@example.com", // Claims personalizados
///   "role": "Admin",
///   "nbf": 1640000000,           // Not Before: cuándo es válido
///   "exp": 1640003600,           // Expiration: cuándo expira (15 min)
///   "iat": 1640000000            // Issued At: cuándo se emitió
/// }
///
/// 3. SIGNATURE:
/// HMACSHA256(
///   base64UrlEncode(header) + "." + base64UrlEncode(payload),
///   secret_key
/// )
///
/// La firma garantiza que el token NO ha sido modificado.
/// Si alguien cambia el payload, la firma ya no coincide.
///
/// IMPORTANTE: JWT NO ESTÁ ENCRIPTADO, está FIRMADO.
/// Cualquiera puede decodificar el payload (es base64).
/// NO poner información sensible en el payload (passwords, números de tarjeta, etc.).
///
/// FLUJO DE AUTENTICACIÓN JWT:
///
/// 1. LOGIN:
///    POST /api/auth/login { email, password }
///    ↓
///    Server valida credenciales
///    ↓
///    Server genera Access Token (15 min) + Refresh Token (7 días)
///    ↓
///    Response: { accessToken, refreshToken }
///
/// 2. REQUEST AUTENTICADO:
///    GET /api/tasks
///    Headers: { Authorization: "Bearer eyJhbGci..." }
///    ↓
///    Server valida firma del token
///    ↓
///    Server extrae claims (userId, role)
///    ↓
///    Server ejecuta request con contexto de usuario
///    ↓
///    Response: { tasks: [...] }
///
/// 3. TOKEN EXPIRADO (después de 15 min):
///    GET /api/tasks
///    Headers: { Authorization: "Bearer expired_token" }
///    ↓
///    Server valida token → EXPIRED
///    ↓
///    Response: 401 Unauthorized { error: "Token expired" }
///    ↓
///    Cliente automáticamente llama a:
///    POST /api/auth/refresh { refreshToken }
///    ↓
///    Server valida refresh token
///    ↓
///    Server genera nuevo Access Token (15 min)
///    ↓
///    Response: { accessToken }
///    ↓
///    Cliente reintentar request original con nuevo token
///
/// 4. LOGOUT:
///    POST /api/auth/logout { refreshToken }
///    ↓
///    Server revoca refresh token en BD
///    ↓
///    Cliente elimina tokens de localStorage
///
/// ACCESS TOKEN vs REFRESH TOKEN:
///
/// ACCESS TOKEN:
/// - Corta duración (15 minutos)
/// - Se envía en cada request (Authorization header)
/// - Contiene claims del usuario (id, email, role)
/// - NO se almacena en BD (stateless)
/// - Si se roba, solo es válido 15 minutos
///
/// REFRESH TOKEN:
/// - Larga duración (7 días)
/// - Solo se usa para obtener nuevo access token
/// - Se almacena en BD (stateful)
/// - Puede ser revocado manualmente
/// - Si se roba, podemos revocarlo en BD
///
/// STATELESS vs STATEFUL:
///
/// JWT es stateless: no necesitamos consultar BD en cada request.
/// Validamos la firma y extraemos claims del token.
///
/// Ventajas stateless:
/// - Performance: No query a BD por cada request
/// - Escalabilidad: No necesitamos sesiones en memoria
/// - Microservicios: Token es self-contained
///
/// Desventajas stateless:
/// - No podemos revocar access token antes de expiración
/// - Si un usuario es baneado, puede usar token hasta que expire
///
/// Solución híbrida (este proyecto):
/// - Access token stateless (15 min)
/// - Refresh token stateful (en BD, puede ser revocado)
/// - Blacklist de access tokens revocados (en Redis cache) - opcional
///
/// CLAIMS:
///
/// Claims son propiedades del usuario incluidas en el token:
///
/// Standard claims (RFC 7519):
/// - sub (Subject): User ID
/// - iss (Issuer): Quién emitió el token (ej: "TaskManagementAPI")
/// - aud (Audience): Para quién es el token (ej: "TaskManagementClient")
/// - exp (Expiration): Timestamp de expiración
/// - nbf (Not Before): No válido antes de este timestamp
/// - iat (Issued At): Timestamp de emisión
/// - jti (JWT ID): ID único del token
///
/// Custom claims (nuestro proyecto):
/// - email: Email del usuario
/// - role: Rol del usuario (User/Admin)
///
/// Ejemplo de payload:
/// {
///   "sub": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
///   "email": "john@example.com",
///   "role": "Admin",
///   "iss": "TaskManagementAPI",
///   "aud": "TaskManagementClient",
///   "exp": 1640003600,
///   "nbf": 1640000000,
///   "iat": 1640000000
/// }
///
/// En el backend, extraemos claims:
/// var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
/// var email = User.FindFirst(ClaimTypes.Email)?.Value;
/// var role = User.FindFirst(ClaimTypes.Role)?.Value;
///
/// SEGURIDAD JWT:
///
/// 1. SECRET KEY:
///    - Debe ser fuerte: mínimo 256 bits (32 caracteres)
///    - Generar con: openssl rand -base64 32
///    - Almacenar en variables de entorno, NUNCA en código
///    - Rotar periódicamente (cada 6 meses)
///
///    ❌ Inseguro:
///    var secret = "secret123";  // Demasiado corto, fácil de adivinar
///
///    ✅ Seguro:
///    var secret = Environment.GetEnvironmentVariable("JWT_SECRET");
///    // Ejemplo: "Kv8J2nZ5mP9xQ1wR4tY7uI0oP3aS6dF8gH1jK4lM7nB2vC5xZ8"
///
/// 2. ALGORITMO:
///    - Usar HMAC SHA256 (HS256) para APIs simples
///    - Usar RSA SHA256 (RS256) para microservicios (clave pública/privada)
///
///    HS256 (symmetric):
///    - Misma clave para firmar y verificar
///    - Más simple
///    - Para monolitos o backend único
///
///    RS256 (asymmetric):
///    - Clave privada para firmar (backend)
///    - Clave pública para verificar (microservicios)
///    - Más complejo pero más seguro para arquitecturas distribuidas
///
///    Este proyecto usa HS256 por simplicidad.
///
/// 3. EXPIRACIÓN:
///    - Access token: 15 minutos (balance seguridad/UX)
///    - Refresh token: 7 días
///    - SIEMPRE validar exp claim
///    - Rechazar tokens expirados con 401 Unauthorized
///
/// 4. VALIDACIONES:
///    - Validar firma (evita manipulación)
///    - Validar expiración (exp claim)
///    - Validar issuer (iss claim) - evita tokens de otras apps
///    - Validar audience (aud claim) - evita tokens para otras apps
///    - Validar not before (nbf claim)
///
/// 5. TRANSPORTE:
///    - SIEMPRE usar HTTPS en producción
///    - Enviar token en Authorization header:
///      Authorization: Bearer eyJhbGci...
///    - NUNCA en query string: /api/tasks?token=... (se loguea en servidor)
///    - NUNCA en cookies sin HttpOnly flag (vulnerable a XSS)
///
/// 6. ALMACENAMIENTO EN CLIENTE:
///    - localStorage: Vulnerable a XSS, pero conveniente
///    - sessionStorage: Se pierde al cerrar pestaña
///    - httpOnly cookie: Más seguro, pero requiere manejo de CSRF
///    - Memory (React state): Más seguro, pero se pierde al refrescar
///
///    Para este proyecto: localStorage (aceptable para MVP)
///    Producción: httpOnly cookie + CSRF token
///
/// REFRESH TOKEN ROTATION:
///
/// Técnica de seguridad para detectar token theft:
///
/// 1. Usuario hace login:
///    → Genera RefreshToken1 (ParentTokenId = null)
///
/// 2. Access token expira, cliente pide refresh:
///    POST /api/auth/refresh { refreshToken: RefreshToken1 }
///    → Marca RefreshToken1 como usado (IsUsed = true)
///    → Genera RefreshToken2 (ParentTokenId = RefreshToken1.Id)
///    → Retorna nuevo access token + RefreshToken2
///
/// 3. Cliente legítimo usa RefreshToken2:
///    POST /api/auth/refresh { refreshToken: RefreshToken2 }
///    → Marca RefreshToken2 como usado
///    → Genera RefreshToken3 (ParentTokenId = RefreshToken2.Id)
///
/// 4. ATAQUE - Atacante usa RefreshToken1 robado:
///    POST /api/auth/refresh { refreshToken: RefreshToken1 }
///    → RefreshToken1 ya está usado (IsUsed = true)
///    → ALERTA: Token reutilizado! Posible robo
///    → Revocar toda la familia de tokens (ParentTokenId chain)
///    → Forzar re-login del usuario
///
/// Este mecanismo detecta token theft y protege al usuario.
///
/// EJEMPLO DE IMPLEMENTACIÓN (Infrastructure):
///
/// public class TokenService : ITokenService
/// {
///     private readonly JwtSettings _jwtSettings;
///
///     public string GenerateAccessToken(User user)
///     {
///         var claims = new[]
///         {
///             new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
///             new Claim(ClaimTypes.Email, user.Email.Value),
///             new Claim(ClaimTypes.Role, user.Role.ToString())
///         };
///
///         var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
///         var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
///
///         var token = new JwtSecurityToken(
///             issuer: _jwtSettings.Issuer,
///             audience: _jwtSettings.Audience,
///             claims: claims,
///             expires: DateTime.UtcNow.AddMinutes(15),
///             signingCredentials: credentials
///         );
///
///         return new JwtSecurityTokenHandler().WriteToken(token);
///     }
///
///     public string GenerateRefreshToken()
///     {
///         var randomBytes = new byte[32];
///         using var rng = RandomNumberGenerator.Create();
///         rng.GetBytes(randomBytes);
///         return Convert.ToBase64String(randomBytes);
///     }
/// }
///
/// EJEMPLO DE USO EN HANDLER:
///
/// public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
/// {
///     private readonly IApplicationDbContext _context;
///     private readonly ITokenService _tokenService;
///     private readonly IPasswordHasher _passwordHasher;
///
///     public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken ct)
///     {
///         // 1. Buscar usuario
///         var user = await _context.Users
///             .FirstOrDefaultAsync(u => u.Email == Email.Create(request.Email), ct);
///
///         if (user == null)
///             return Result.Failure<AuthResponse>("Invalid credentials");
///
///         // 2. Verificar contraseña
///         if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
///         {
///             user.RecordFailedLogin();
///             await _context.SaveChangesAsync(ct);
///             return Result.Failure<AuthResponse>("Invalid credentials");
///         }
///
///         // 3. Verificar lockout
///         if (!user.CanLogin())
///             return Result.Failure<AuthResponse>("Account is locked");
///
///         // 4. Generar tokens
///         var accessToken = _tokenService.GenerateAccessToken(user);
///         var refreshTokenValue = _tokenService.GenerateRefreshToken();
///
///         var refreshToken = RefreshToken.Create(
///             user.Id,
///             refreshTokenValue,
///             DateTime.UtcNow.AddDays(7)
///         );
///
///         _context.RefreshTokens.Add(refreshToken);
///
///         // 5. Reset failed login attempts
///         user.ResetLoginAttempts();
///
///         await _context.SaveChangesAsync(ct);
///
///         // 6. Retornar tokens
///         return Result.Success(new AuthResponse
///         {
///             AccessToken = accessToken,
///             RefreshToken = refreshTokenValue,
///             ExpiresIn = 900  // 15 minutos en segundos
///         });
///     }
/// }
///
/// DEBUGGING JWT:
///
/// Para inspeccionar un JWT, usar https://jwt.io
/// Pega el token y verás header, payload, y validación de firma.
///
/// NUNCA logguear tokens completos en producción:
/// ❌ _logger.LogInformation($"Token: {token}");  // Expone token en logs
/// ✅ _logger.LogInformation($"Token generated for user {userId}");
/// </remarks>
public interface ITokenService
{
    /// <summary>
    /// Genera un access token JWT para un usuario.
    /// </summary>
    /// <param name="user">Usuario para el cual generar el token.</param>
    /// <returns>Access token JWT como string.</returns>
    /// <remarks>
    /// El access token:
    /// - Tiene duración corta (15 minutos)
    /// - Contiene claims del usuario (id, email, role)
    /// - Se envía en Authorization header: Bearer {token}
    /// - Es stateless (no se almacena en BD)
    ///
    /// Estructura del token generado:
    /// {
    ///   "sub": "user-guid",
    ///   "email": "user@example.com",
    ///   "role": "User",
    ///   "exp": 1640003600,
    ///   "iss": "TaskManagementAPI",
    ///   "aud": "TaskManagementClient"
    /// }
    ///
    /// Uso en cliente:
    /// fetch('/api/tasks', {
    ///   headers: {
    ///     'Authorization': `Bearer ${accessToken}`
    ///   }
    /// });
    /// </remarks>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Genera un refresh token aleatorio criptográficamente seguro.
    /// </summary>
    /// <returns>Refresh token como string base64.</returns>
    /// <remarks>
    /// El refresh token:
    /// - Es un string aleatorio (no es JWT)
    /// - Tiene duración larga (7 días)
    /// - Se almacena en BD (stateful)
    /// - Solo se usa para obtener nuevo access token
    /// - Puede ser revocado manualmente
    ///
    /// Generación:
    /// 1. Genera 32 bytes aleatorios con RNG criptográfico
    /// 2. Convierte a base64
    /// 3. Resultado: "xK8mP2nZ5vQ9wR1tY4uI7oP0aS3dF6gH9jK2lM5nB8vC1xZ4=="
    ///
    /// IMPORTANTE: Usar RNG criptográfico (RandomNumberGenerator.Create()),
    /// NO usar Random() que es predecible.
    ///
    /// Almacenamiento:
    /// El token se almacena en tabla RefreshTokens con:
    /// - UserId: Dueño del token
    /// - ExpiresAt: Fecha de expiración (7 días)
    /// - IsUsed: Flag para token rotation
    /// - IsRevoked: Flag para revocación manual
    /// - ParentTokenId: Para detectar token theft
    ///
    /// Uso:
    /// 1. Cliente envía refresh token cuando access token expira
    /// 2. Server valida refresh token en BD
    /// 3. Server genera nuevo access token
    /// 4. Server marca refresh token como usado
    /// 5. Server genera nuevo refresh token (rotation)
    /// 6. Retorna nuevo par de tokens
    /// </remarks>
    string GenerateRefreshToken();

    /// <summary>
    /// Valida un access token JWT.
    /// </summary>
    /// <param name="token">Token JWT a validar.</param>
    /// <returns>ClaimsPrincipal con los claims del token si es válido, null si es inválido.</returns>
    /// <remarks>
    /// Validaciones realizadas:
    /// 1. Formato válido (3 partes separadas por puntos)
    /// 2. Firma válida (no ha sido modificado)
    /// 3. No expirado (exp claim < now)
    /// 4. Issuer correcto (iss claim)
    /// 5. Audience correcto (aud claim)
    /// 6. Not before válido (nbf claim <= now)
    ///
    /// Retorna null si:
    /// - Token mal formado
    /// - Firma inválida (token manipulado o secret incorrecto)
    /// - Token expirado
    /// - Issuer/Audience incorrecto
    ///
    /// Retorna ClaimsPrincipal si token es válido:
    /// var principal = _tokenService.ValidateToken(token);
    /// if (principal == null)
    ///     return Unauthorized();
    ///
    /// var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    /// var email = principal.FindFirst(ClaimTypes.Email)?.Value;
    /// var role = principal.FindFirst(ClaimTypes.Role)?.Value;
    ///
    /// USO EN MIDDLEWARE:
    ///
    /// Este método NO se usa directamente en la aplicación.
    /// ASP.NET Core middleware de autenticación lo usa automáticamente:
    ///
    /// app.UseAuthentication();  // ← Usa TokenService internamente
    /// app.UseAuthorization();
    ///
    /// Configuración en Program.cs:
    /// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    ///     .AddJwtBearer(options =>
    ///     {
    ///         options.TokenValidationParameters = new TokenValidationParameters
    ///         {
    ///             ValidateIssuer = true,
    ///             ValidateAudience = true,
    ///             ValidateLifetime = true,
    ///             ValidateIssuerSigningKey = true,
    ///             ValidIssuer = jwtSettings.Issuer,
    ///             ValidAudience = jwtSettings.Audience,
    ///             IssuerSigningKey = new SymmetricSecurityKey(
    ///                 Encoding.UTF8.GetBytes(jwtSettings.Secret)
    ///             )
    ///         };
    ///     });
    ///
    /// SOLO útil si necesitas validar tokens manualmente (raro).
    /// </remarks>
    System.Security.Claims.ClaimsPrincipal? ValidateToken(string token);
}
