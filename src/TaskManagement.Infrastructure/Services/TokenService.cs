using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Services;

/// <summary>
/// Servicio para generación y validación de tokens JWT.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE JWT (JSON Web Tokens):
///
/// JWT es un estándar (RFC 7519) para crear tokens de acceso que permiten
/// autenticación y autorización sin estado (stateless).
///
/// ESTRUCTURA DE UN JWT:
///
/// Un JWT tiene 3 partes separadas por puntos (.):
///
/// HEADER.PAYLOAD.SIGNATURE
///
/// Ejemplo:
/// eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
///
/// 1. HEADER (Base64Url encoded):
/// {
///   "alg": "HS256",      // Algoritmo de firma
///   "typ": "JWT"         // Tipo de token
/// }
///
/// 2. PAYLOAD (Base64Url encoded):
/// {
///   "sub": "user-id-123",          // Subject (ID del usuario)
///   "email": "user@example.com",   // Email del usuario
///   "role": "User",                // Rol del usuario
///   "exp": 1735689600,             // Expiration time (Unix timestamp)
///   "iat": 1735688700,             // Issued at (Unix timestamp)
///   "jti": "unique-token-id"       // JWT ID (identificador único)
/// }
///
/// 3. SIGNATURE:
/// HMACSHA256(
///   base64UrlEncode(header) + "." + base64UrlEncode(payload),
///   secret_key
/// )
///
/// CLAIMS:
///
/// Claims son pares clave-valor que contienen información sobre el usuario.
///
/// Standard Claims (RFC 7519):
/// - sub (Subject): ID del usuario
/// - exp (Expiration): Tiempo de expiración
/// - iat (Issued At): Tiempo de emisión
/// - iss (Issuer): Quién emitió el token
/// - aud (Audience): Para quién es el token
/// - jti (JWT ID): Identificador único del token
///
/// Custom Claims (propios de la aplicación):
/// - email: Email del usuario
/// - role: Rol del usuario (User, Admin)
///
/// EJEMPLO DE CLAIMS EN C#:
///
/// var claims = new[]
/// {
///     new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),  // sub
///     new Claim(ClaimTypes.Email, user.Email.Value),             // email
///     new Claim(ClaimTypes.Role, user.Role.ToString()),          // role
///     new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())  // jti
/// };
///
/// VALIDACIÓN DE JWT:
///
/// Cuando el cliente envía el token en el header Authorization:
/// Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
///
/// El servidor valida:
/// 1. Firma (signature): Verifica que el token no fue modificado
/// 2. Expiración (exp): Verifica que el token no haya expirado
/// 3. Issuer (iss): Verifica que el token fue emitido por este servidor
/// 4. Audience (aud): Verifica que el token es para esta API
///
/// Si la validación pasa, extrae los claims y crea ClaimsPrincipal.
///
/// ACCESO A CLAIMS EN CONTROLLER:
///
/// [Authorize]
/// public class TasksController : ControllerBase
/// {
///     [HttpGet]
///     public IActionResult Get()
///     {
///         // HttpContext.User es un ClaimsPrincipal con los claims del JWT
///         var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
///         var email = User.FindFirstValue(ClaimTypes.Email);
///         var role = User.FindFirstValue(ClaimTypes.Role);
///         var isAdmin = User.IsInRole("Admin");
///
///         return Ok($"User {userId} with email {email}");
///     }
/// }
///
/// SEGURIDAD:
///
/// 1. SECRET KEY:
/// - Debe ser >= 256 bits (32 bytes) para HS256
/// - Debe ser criptográficamente seguro
/// - Debe estar en variables de entorno, NO en código
/// - Si se compromete, TODOS los tokens son vulnerables
///
/// 2. EXPIRACIÓN:
/// - Access tokens: corta duración (15 minutos)
/// - Refresh tokens: larga duración (7 días)
/// - Tokens expirados no pueden ser renovados
///
/// 3. HTTPS:
/// - SIEMPRE usar HTTPS en producción
/// - Sin HTTPS, tokens pueden ser interceptados
///
/// 4. ALMACENAMIENTO EN CLIENTE:
/// - NO guardar en localStorage (vulnerable a XSS)
/// - Usar httpOnly cookies (mejor para XSS)
/// - O guardar en memoria (mejor seguridad, pero se pierde al refrescar)
///
/// REFRESH TOKENS:
///
/// Access token expira rápido (15 min) por seguridad.
/// Refresh token permite obtener nuevo access token sin re-login.
///
/// Flujo:
/// 1. Login → Access token (15 min) + Refresh token (7 días)
/// 2. Access token expira → Cliente envía refresh token
/// 3. API valida refresh token → Nuevo access token + Nuevo refresh token
/// 4. Refresh token también expira → Cliente debe hacer login
///
/// TOKEN ROTATION:
///
/// Cada vez que se usa un refresh token, se invalida y se genera uno nuevo.
/// Si un token ya usado se vuelve a usar → posible robo → revocar todos.
///
/// COMPARACIÓN: JWT vs Session Cookies:
///
/// JWT (Stateless):
/// ✅ Escalable (no requiere almacenamiento en servidor)
/// ✅ Funciona bien con microservicios
/// ✅ Mobile-friendly
/// ❌ No se puede revocar inmediatamente (hasta que expire)
/// ❌ Tamaño más grande (enviado en cada request)
///
/// Session Cookies (Stateful):
/// ✅ Se puede revocar inmediatamente
/// ✅ Tamaño pequeño (solo session ID)
/// ❌ Requiere almacenamiento en servidor (Redis, DB)
/// ❌ Difícil de escalar horizontalmente
/// ❌ CORS más complicado
///
/// ALGORITMOS DE FIRMA:
///
/// - HS256 (HMAC + SHA256): Simétrico, una clave para firmar y validar
/// - RS256 (RSA + SHA256): Asimétrico, clave privada para firmar, pública para validar
///
/// HS256 es suficiente para APIs monolíticas.
/// RS256 es mejor para microservicios (Auth service firma, otros validan con clave pública).
///
/// CONFIGURACIÓN EN appsettings.json:
///
/// {
///   "JwtSettings": {
///     "SecretKey": "your-256-bit-secret-key-min-32-characters",
///     "Issuer": "TaskManagementAPI",
///     "Audience": "TaskManagementAPI",
///     "AccessTokenExpirationMinutes": 15,
///     "RefreshTokenExpirationDays": 7
///   }
/// }
///
/// REGISTRO EN Program.cs:
///
/// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
///     .AddJwtBearer(options =>
///     {
///         options.TokenValidationParameters = new TokenValidationParameters
///         {
///             ValidateIssuer = true,
///             ValidateAudience = true,
///             ValidateLifetime = true,
///             ValidateIssuerSigningKey = true,
///             ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
///             ValidAudience = builder.Configuration["JwtSettings:Audience"],
///             IssuerSigningKey = new SymmetricSecurityKey(
///                 Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!))
///         };
///     });
///
/// app.UseAuthentication();  // ANTES de UseAuthorization
/// app.UseAuthorization();
///
/// TESTING:
///
/// [Fact]
/// public void GenerateAccessToken_ShouldContainValidClaims()
/// {
///     var user = User.Create(Email.Create("test@example.com"), "hash", UserRole.User);
///     var token = _tokenService.GenerateAccessToken(user);
///
///     var handler = new JwtSecurityTokenHandler();
///     var jwtToken = handler.ReadJwtToken(token);
///
///     Assert.Equal(user.Id.ToString(), jwtToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
///     Assert.Equal(user.Email.Value, jwtToken.Claims.First(c => c.Type == ClaimTypes.Email).Value);
/// }
///
/// [Fact]
/// public void ValidateToken_ExpiredToken_ShouldReturnNull()
/// {
///     var expiredToken = "..."; // Token expirado
///     var result = _tokenService.ValidateToken(expiredToken);
///     Assert.Null(result);
/// }
/// </remarks>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly SymmetricSecurityKey _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;

        var secretKey = _configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey not configured");

        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        _issuer = _configuration["JwtSettings:Issuer"] ?? "TaskManagementAPI";
        _audience = _configuration["JwtSettings:Audience"] ?? "TaskManagementAPI";
        _accessTokenExpirationMinutes = int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "15");
    }

    /// <summary>
    /// Genera un access token JWT para el usuario.
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        // Claims: información sobre el usuario embebida en el token
        var claims = new[]
        {
            // ClaimTypes.NameIdentifier → "sub" en JWT
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),

            // ClaimTypes.Email → "email" en JWT
            new Claim(ClaimTypes.Email, user.Email.Value),

            // ClaimTypes.Role → "role" en JWT
            // Usado por [Authorize(Roles = "Admin")]
            new Claim(ClaimTypes.Role, user.Role.ToString()),

            // JwtRegisteredClaimNames.Jti → "jti" en JWT (JWT ID)
            // Identificador único del token, útil para tracking/revocación
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Credenciales de firma: algoritmo HS256 (HMAC + SHA256)
        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        // Crear el token JWT
        var token = new JwtSecurityToken(
            issuer: _issuer,                                          // Quién emitió el token
            audience: _audience,                                      // Para quién es el token
            claims: claims,                                           // Claims del usuario
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),  // Expira en 15 minutos
            signingCredentials: credentials                           // Firma del token
        );

        // Serializar token a string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Genera un refresh token aleatorio.
    /// </summary>
    /// <remarks>
    /// Refresh token es opaco (no contiene información).
    /// Se almacena en base de datos con su expiración.
    /// Cliente lo envía cuando access token expira.
    /// </remarks>
    public string GenerateRefreshToken()
    {
        // Generar 32 bytes aleatorios criptográficamente seguros
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        // Convertir a Base64 para transmisión
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Valida un token JWT y devuelve el ClaimsPrincipal si es válido.
    /// </summary>
    /// <returns>ClaimsPrincipal con claims del token, o null si es inválido.</returns>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            // Parámetros de validación (mismos que en Program.cs)
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,                    // Valida que no haya expirado
                ValidateIssuerSigningKey = true,            // Valida la firma
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = _key,
                ClockSkew = TimeSpan.Zero                   // No tolerancia de tiempo (default es 5 min)
            };

            // Valida y devuelve el ClaimsPrincipal
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // Verificar que el algoritmo es el esperado (prevenir ataques de cambio de algoritmo)
            if (validatedToken is JwtSecurityToken jwtToken &&
                jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return principal;
            }

            return null;
        }
        catch
        {
            // Token inválido, expirado o con firma incorrecta
            return null;
        }
    }
}
