using TaskManagement.Domain.Exceptions;

namespace TaskManagement.Domain.Entities;

/// <summary>
/// Entidad de dominio que representa un refresh token para autenticación JWT.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE REFRESH TOKENS:
///
/// En autenticación JWT, tenemos DOS tipos de tokens:
///
/// 1. ACCESS TOKEN:
///    - Token principal usado en cada request
///    - Corta duración (15 minutos típicamente)
///    - Incluido en header: Authorization: Bearer {accessToken}
///    - Contiene claims del usuario (id, email, role)
///    - NO se almacena en BD (stateless)
///
/// 2. REFRESH TOKEN:
///    - Token usado SOLO para renovar access tokens
///    - Larga duración (7 días, 30 días típicamente)
///    - Se almacena en BD (esta entidad)
///    - Permite obtener nuevo access token sin re-login
///
/// FLUJO COMPLETO:
///
/// 1. LOGIN:
///    User → POST /api/auth/login
///         ← { accessToken, refreshToken }
///    - Ambos tokens se retornan
///    - refreshToken se guarda en BD
///
/// 2. REQUEST NORMAL:
///    User → GET /api/tasks
///         Header: Authorization: Bearer {accessToken}
///         ← { tasks }
///
/// 3. ACCESS TOKEN EXPIRA (después de 15 min):
///    User → GET /api/tasks
///         ← 401 Unauthorized
///
/// 4. RENOVAR TOKEN:
///    User → POST /api/auth/refresh
///         Body: { refreshToken }
///         ← { newAccessToken, newRefreshToken }
///    - Se invalida el refresh token anterior
///    - Se genera nuevo par de tokens (ROTATION)
///
/// 5. CONTINUAR USANDO API:
///    User → GET /api/tasks
///         Header: Authorization: Bearer {newAccessToken}
///         ← { tasks }
///
/// POR QUÉ REFRESH TOKENS:
///
/// PROBLEMA SIN REFRESH TOKENS:
/// ❌ Access token de larga duración:
///    - Si es robado, el atacante tiene acceso por mucho tiempo
///    - No hay forma de revocarlo (JWT es stateless)
///
/// ❌ Access token de corta duración sin refresh:
///    - Usuario debe re-loguearse cada 15 minutos
///    - Mala experiencia de usuario
///
/// ✅ SOLUCIÓN CON REFRESH TOKENS:
///    - Access token corto (15 min): Si se roba, daño limitado
///    - Refresh token largo (7 días): Usuario no re-loguea constantemente
///    - Refresh token en BD: Podemos revocarlo si es necesario
///    - Token rotation: Cada refresh invalida el anterior
///
/// REFRESH TOKEN ROTATION:
///
/// Técnica de seguridad que invalida el refresh token después de usarlo.
///
/// ATAQUE SIN ROTATION:
/// 1. Atacante roba refresh token
/// 2. Atacante lo usa para generar access tokens
/// 3. Víctima también lo usa
/// 4. Ambos tienen acceso indefinidamente
///
/// PROTECCIÓN CON ROTATION:
/// 1. Atacante roba refresh token
/// 2. Atacante lo usa → Token A se invalida, recibe Token B
/// 3. Víctima intenta usar Token A → DETECTAMOS REUSO
/// 4. Revocamos TODOS los tokens del usuario (token family)
/// 5. Usuario debe re-loguearse
///
/// DETECCIÓN DE REUSO:
/// if (refreshToken.IsUsed)
/// {
///     // ¡ALERTA! Token ya fue usado antes
///     // Posible robo de token
///     RevokeAllTokensForUser(refreshToken.UserId);
///     throw new SecurityException("Token reuse detected");
/// }
///
/// TOKEN FAMILY:
///
/// Un "family" de tokens es una cadena de tokens relacionados:
///
/// Login → TokenA
/// Refresh(TokenA) → TokenB (ParentTokenId = TokenA.Id)
/// Refresh(TokenB) → TokenC (ParentTokenId = TokenB.Id)
/// Refresh(TokenC) → TokenD (ParentTokenId = TokenC.Id)
///
/// Si detectamos reuso de TokenB:
/// - Revocamos TokenC y TokenD (toda la familia)
/// - Forzamos re-login
///
/// ALMACENAMIENTO:
///
/// Dónde almacenar tokens en el cliente:
///
/// 1. localStorage:
///    ✅ Persiste entre sesiones
///    ❌ Vulnerable a XSS
///    Usar solo si frontend está protegido contra XSS
///
/// 2. sessionStorage:
///    ✅ Se limpia al cerrar browser
///    ❌ Vulnerable a XSS
///    ❌ No persiste entre sesiones
///
/// 3. httpOnly Cookie:
///    ✅ NO accesible desde JavaScript (inmune a XSS)
///    ✅ Se envía automáticamente
///    ❌ Vulnerable a CSRF (mitigar con SameSite=Strict)
///    ❌ Requiere CORS correcto
///
/// RECOMENDACIÓN:
/// - accessToken: localStorage/sessionStorage (corta duración, riesgo limitado)
/// - refreshToken: httpOnly Cookie (larga duración, máxima protección)
///
/// REDIS vs DATABASE:
///
/// Almacenamiento del refresh token:
///
/// OPCIÓN 1: Redis (Cache)
/// ✅ Muy rápido (in-memory)
/// ✅ TTL automático (expira solo)
/// ✅ Ideal para datos temporales
/// ❌ Puede perderse si Redis falla
/// ❌ No apto para auditoría persistente
///
/// OPCIÓN 2: Database (PostgreSQL)
/// ✅ Persistente y confiable
/// ✅ Permite auditoría completa
/// ✅ Transaccional con otros datos
/// ❌ Más lento que Redis
/// ❌ Requiere limpieza de tokens expirados
///
/// OPCIÓN 3: Híbrida (este proyecto)
/// - Almacenar en DB para persistencia
/// - Cachear en Redis para velocidad
/// - Lo mejor de ambos mundos
///
/// LIMPIEZA DE TOKENS EXPIRADOS:
///
/// Los tokens expirados deben eliminarse periódicamente:
///
/// // Background job (Hangfire, Quartz)
/// public async Task CleanupExpiredTokens()
/// {
///     await _context.RefreshTokens
///         .Where(t => t.ExpiresAt < DateTime.UtcNow)
///         .ExecuteDeleteAsync();
/// }
///
/// Ejecutar diariamente o semanalmente.
///
/// MÉTRICAS Y MONITOREO:
///
/// - Tokens activos por usuario
/// - Tasa de reuso detectado (indicador de ataques)
/// - Tokens expirados no limpiados (health check)
/// - Frecuencia de refresh (comportamiento de usuarios)
///
/// SEGURIDAD ADICIONAL:
///
/// 1. Device/Browser Fingerprinting:
///    - Almacenar user agent al crear token
///    - Validar que el refresh viene del mismo device
///
/// 2. IP Whitelisting:
///    - Almacenar IP al crear token
///    - Alertar si refresh viene de IP diferente
///
/// 3. Geolocation:
///    - Detectar cambios imposibles de ubicación
///    - "Imposible estar en USA y China en 5 minutos"
///
/// 4. Max Active Sessions:
///    - Limitar a N tokens activos por usuario
///    - Revocar el más antiguo al crear nuevo
/// </remarks>
public class RefreshToken : BaseEntity
{
    /// <summary>
    /// Valor del refresh token (string aleatorio seguro).
    /// </summary>
    /// <remarks>
    /// GENERACIÓN DEL TOKEN:
    ///
    /// El token debe ser:
    /// - Aleatorio: No predecible
    /// - Largo: Suficiente entropía (64 bytes = 512 bits)
    /// - Único: No debe repetirse
    /// - Seguro: Usar RNG criptográfico
    ///
    /// IMPLEMENTACIÓN:
    ///
    /// public string GenerateRefreshToken()
    /// {
    ///     var randomBytes = new byte[64];
    ///     using var rng = RandomNumberGenerator.Create();
    ///     rng.GetBytes(randomBytes);
    ///     return Convert.ToBase64String(randomBytes);
    /// }
    ///
    /// Ejemplo de token generado:
    /// "Kv7xJZ2kL9mN4pQ6rS8tU0vW2xY4zA6bC8dE0fG2hI4jK6lM8nO0pQ2rS4tU6vW8xY0zA2bC4dE6fG8hI0jK2lM4nO6pQ8=="
    ///
    /// LONGITUD:
    /// 64 bytes = 86 caracteres en Base64
    /// Entropía: 512 bits (extremadamente seguro)
    ///
    /// COMPARACIÓN:
    /// - GUID: 128 bits de entropía
    /// - Refresh Token: 512 bits (4x más seguro)
    ///
    /// NO USAR:
    /// ❌ Guid.NewGuid().ToString() (solo 128 bits, predecible)
    /// ❌ DateTime.Now.Ticks (completamente predecible)
    /// ❌ Random() (no criptográfico, predecible)
    ///
    /// ALMACENAMIENTO:
    /// - En BD: Hash del token (como contraseñas)
    ///   Ventaja: Si hay breach de DB, tokens no son usables
    ///   Desventaja: No podemos "ver" el token para debug
    ///
    /// - En BD: Token en texto plano (este proyecto)
    ///   Ventaja: Más simple, debug fácil
    ///   Desventaja: Si hay breach, tokens son usables
    ///   Mitigación: Encriptar BD, acceso restringido, TTL corto
    ///
    /// Para máxima seguridad, considerar hashear.
    /// </remarks>
    public string Token { get; private set; }

    /// <summary>
    /// ID del usuario al que pertenece este refresh token.
    /// </summary>
    /// <remarks>
    /// FOREIGN KEY:
    ///
    /// Relación con User entity:
    /// - Un User puede tener múltiples RefreshTokens (sesiones en diferentes devices)
    /// - Un RefreshToken pertenece a un único User
    ///
    /// CONFIGURACIÓN EN EF CORE:
    /// builder.HasOne<User>()
    ///     .WithMany()
    ///     .HasForeignKey(rt => rt.UserId)
    ///     .OnDelete(DeleteBehavior.Cascade);
    ///
    /// Cascade Delete:
    /// Si se elimina el User, todos sus RefreshTokens se eliminan automáticamente.
    /// Esto cierra todas las sesiones del usuario eliminado.
    ///
    /// ÍNDICE:
    /// Crear índice en UserId para búsquedas rápidas:
    /// CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
    ///
    /// QUERIES COMUNES:
    /// // Obtener todos los tokens activos de un usuario
    /// var userTokens = await _context.RefreshTokens
    ///     .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
    ///     .ToListAsync();
    ///
    /// // Revocar todos los tokens de un usuario (force logout)
    /// var userTokens = await _context.RefreshTokens
    ///     .Where(rt => rt.UserId == userId)
    ///     .ToListAsync();
    /// userTokens.ForEach(t => t.Revoke());
    ///
    /// MULTI-DEVICE:
    /// Un usuario puede tener múltiples tokens activos si usa:
    /// - Web browser
    /// - Mobile app
    /// - Tablet
    /// - Desktop app
    ///
    /// Cada device/sesión tiene su propio refresh token.
    ///
    /// LÍMITE DE SESIONES:
    /// Para evitar abuso, considerar limitar sesiones concurrentes:
    /// const int maxSessions = 5;
    /// var activeSessions = await _context.RefreshTokens
    ///     .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
    ///     .CountAsync();
    /// if (activeSessions >= maxSessions)
    /// {
    ///     // Revocar el más antiguo
    ///     var oldest = await _context.RefreshTokens
    ///         .Where(rt => rt.UserId == userId)
    ///         .OrderBy(rt => rt.CreatedAt)
    ///         .FirstAsync();
    ///     oldest.Revoke();
    /// }
    /// </remarks>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Fecha y hora de expiración del token (UTC).
    /// </summary>
    /// <remarks>
    /// DURACIÓN DEL REFRESH TOKEN:
    ///
    /// Típicamente: 7 días a 30 días
    /// - 7 días: Más seguro, usuario re-loguea semanalmente
    /// - 30 días: Mejor UX, usuario re-loguea mensualmente
    /// - "Remember me": 90 días o más
    ///
    /// En este proyecto: 7 días (balance seguridad/UX)
    ///
    /// CÁLCULO AL CREAR:
    /// ExpiresAt = DateTime.UtcNow.AddDays(7);
    ///
    /// VALIDACIÓN:
    /// public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    ///
    /// LIMPIEZA:
    /// Los tokens expirados deben limpiarse periódicamente:
    /// - Background job diario/semanal
    /// - O al buscar: query incluye WHERE ExpiresAt > NOW()
    ///
    /// EXTENSIÓN DE EXPIRACIÓN:
    ///
    /// Algunos sistemas extienden la expiración en cada uso (sliding expiration):
    /// public void Refresh()
    /// {
    ///     if (IsUsed)
    ///         throw new DomainException("Token already used");
    ///
    ///     ExpiresAt = DateTime.UtcNow.AddDays(7); // Extender
    ///     IsUsed = true;
    /// }
    ///
    /// Pero en ROTATION, cada refresh genera nuevo token, no extiende el actual.
    ///
    /// REVOCACIÓN POR INACTIVIDAD:
    ///
    /// Además de ExpiresAt, considerar LastUsedAt:
    /// - Si no se usa por 30 días, revocar automáticamente
    /// - Incluso si ExpiresAt es futuro
    /// - Protege cuentas inactivas
    /// </remarks>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Indica si el token ya fue usado (para token rotation).
    /// </summary>
    /// <remarks>
    /// TOKEN ROTATION - REGLA DE ORO:
    ///
    /// Un refresh token solo puede usarse UNA vez.
    /// Después de usarlo, se marca IsUsed = true.
    ///
    /// FLUJO NORMAL:
    /// 1. User hace refresh con TokenA
    /// 2. Verificamos: TokenA.IsUsed? No → OK
    /// 3. Generamos TokenB nuevo
    /// 4. Marcamos TokenA.IsUsed = true
    /// 5. Guardamos TokenB (IsUsed = false)
    /// 6. Retornamos TokenB al cliente
    ///
    /// DETECCIÓN DE ATAQUE:
    /// 1. Atacante robó TokenA
    /// 2. User legítimo hace refresh con TokenA
    ///    → TokenA.IsUsed = true
    /// 3. Atacante intenta usar TokenA
    ///    → TokenA.IsUsed = true → ¡ALERTA!
    /// 4. Revocamos toda la token family
    /// 5. Forzamos re-login
    ///
    /// IMPLEMENTACIÓN:
    /// var token = await _context.RefreshTokens
    ///     .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
    ///
    /// if (token == null || token.IsExpired)
    ///     return Failure("Invalid or expired token");
    ///
    /// if (token.IsUsed)
    /// {
    ///     // ¡REUSO DETECTADO!
    ///     _logger.LogCritical("Token reuse detected for user {UserId}", token.UserId);
    ///     await RevokeTokenFamilyAsync(token);
    ///     return Failure("Token reuse detected. Please login again.");
    /// }
    ///
    /// token.MarkAsUsed();
    /// // ... generar nuevo token
    ///
    /// LIMPIEZA:
    /// Tokens usados (IsUsed = true) eventualmente expiran y pueden limpiarse:
    /// DELETE FROM RefreshTokens
    /// WHERE IsUsed = true AND ExpiresAt < NOW() - INTERVAL '7 days';
    ///
    /// Mantenemos por 7 días extra para auditoría.
    /// </remarks>
    public bool IsUsed { get; private set; }

    /// <summary>
    /// Indica si el token fue revocado manualmente (invalidado).
    /// </summary>
    /// <remarks>
    /// REVOCACIÓN MANUAL:
    ///
    /// Un token puede revocarse antes de expirar por varias razones:
    ///
    /// 1. USER LOGOUT:
    ///    - Usuario hace logout explícito
    ///    - Revocamos su refresh token
    ///    - Fuerza re-login en próximo access
    ///
    /// 2. CAMBIO DE CONTRASEÑA:
    ///    - Usuario cambia su password
    ///    - Revocamos TODOS sus refresh tokens
    ///    - Cierra todas las sesiones activas
    ///    - Requiere re-login en todos los devices
    ///
    /// 3. ACTIVIDAD SOSPECHOSA:
    ///    - Detectamos múltiples fallos de login
    ///    - Detectamos reuso de token
    ///    - Detectamos login desde ubicación inusual
    ///    - Revocamos todos los tokens por seguridad
    ///
    /// 4. ADMIN ACTION:
    ///    - Admin cierra sesiones de un usuario
    ///    - Por violación de términos
    ///    - Por solicitud del usuario
    ///    - Por investigación de seguridad
    ///
    /// 5. CAMBIO DE ROL:
    ///    - Usuario es promovido/degradado
    ///    - Revocar tokens para forzar obtener nuevo (con nuevo role claim)
    ///
    /// DIFERENCIA IsUsed vs IsRevoked:
    ///
    /// - IsUsed:
    ///   * Automático por token rotation
    ///   * Después de usar el token para refresh
    ///   * Parte del flujo normal
    ///
    /// - IsRevoked:
    ///   * Manual por acción explícita
    ///   * Invalidar token prematuramente
    ///   * Por razones de seguridad/admin
    ///
    /// VALIDACIÓN:
    /// var token = await _context.RefreshTokens.FindAsync(...);
    ///
    /// if (token.IsRevoked)
    ///     return Failure("Token has been revoked");
    ///
    /// if (token.IsUsed)
    ///     return Failure("Token already used");
    ///
    /// if (token.IsExpired)
    ///     return Failure("Token expired");
    ///
    /// REVOCACIÓN EN CASCADA:
    /// Al revocar, también revocar toda la token family:
    /// public async Task RevokeTokenFamilyAsync(RefreshToken token)
    /// {
    ///     var family = await GetTokenFamilyAsync(token);
    ///     foreach (var t in family)
    ///         t.Revoke();
    /// }
    ///
    /// TIMESTAMP DE REVOCACIÓN:
    /// Considerar agregar RevokedAt y RevokedReason:
    /// public DateTime? RevokedAt { get; private set; }
    /// public string? RevokedReason { get; private set; }
    ///
    /// Útil para auditoría y análisis.
    /// </remarks>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// ID del token padre (para token family tracking).
    /// </summary>
    /// <remarks>
    /// TOKEN FAMILY (FAMILIA DE TOKENS):
    ///
    /// Una familia es una cadena de tokens relacionados por token rotation.
    ///
    /// ESTRUCTURA:
    ///
    /// Login → TokenA (ParentTokenId = null, root de familia)
    ///    |
    ///    Refresh(TokenA) → TokenB (ParentTokenId = TokenA.Id)
    ///         |
    ///         Refresh(TokenB) → TokenC (ParentTokenId = TokenB.Id)
    ///              |
    ///              Refresh(TokenC) → TokenD (ParentTokenId = TokenC.Id)
    ///
    /// DETECCIÓN DE REUSO:
    ///
    /// Si detectamos reuso de TokenB:
    /// 1. Encontrar toda la familia (TokenB, TokenC, TokenD)
    /// 2. Revocar todos
    /// 3. Usuario debe re-loguearse
    ///
    /// IMPLEMENTACIÓN:
    ///
    /// public async Task<List<RefreshToken>> GetTokenFamilyAsync(RefreshToken token)
    /// {
    ///     var family = new List<RefreshToken>();
    ///
    ///     // Encontrar root (token sin padre)
    ///     var current = token;
    ///     while (current.ParentTokenId.HasValue)
    ///     {
    ///         current = await _context.RefreshTokens.FindAsync(current.ParentTokenId.Value);
    ///     }
    ///     var root = current;
    ///
    ///     // Agregar root y todos sus descendientes
    ///     family.Add(root);
    ///     await AddDescendantsAsync(root, family);
    ///
    ///     return family;
    /// }
    ///
    /// private async Task AddDescendantsAsync(RefreshToken parent, List<RefreshToken> family)
    /// {
    ///     var children = await _context.RefreshTokens
    ///         .Where(rt => rt.ParentTokenId == parent.Id)
    ///         .ToListAsync();
    ///
    ///     foreach (var child in children)
    ///     {
    ///         family.Add(child);
    ///         await AddDescendantsAsync(child, family);
    ///     }
    /// }
    ///
    /// REVOCACIÓN DE FAMILIA:
    ///
    /// public async Task RevokeTokenFamilyAsync(RefreshToken token)
    /// {
    ///     var family = await GetTokenFamilyAsync(token);
    ///     foreach (var t in family)
    ///     {
    ///         t.Revoke();
    ///     }
    ///     await _context.SaveChangesAsync();
    ///
    ///     _logger.LogWarning(
    ///         "Revoked token family for user {UserId}. Total tokens: {Count}",
    ///         token.UserId, family.Count);
    /// }
    ///
    /// POR QUÉ REVOCAR LA FAMILIA:
    ///
    /// Escenario:
    /// 1. Atacante roba TokenA
    /// 2. Víctima usa TokenA → genera TokenB
    /// 3. Víctima usa TokenB → genera TokenC (actual)
    /// 4. Atacante intenta usar TokenA → DETECTADO
    ///
    /// Si solo revocamos TokenA:
    /// - Víctima sigue con TokenC (OK)
    /// - Pero atacante puede intentar otros ataques
    ///
    /// Si revocamos TODA la familia:
    /// - TokenC también se revoca
    /// - Víctima debe re-loguearse (molestia, pero seguro)
    /// - Atacante no puede hacer nada
    ///
    /// Es un trade-off: seguridad > comodidad
    ///
    /// VISUALIZACIÓN:
    ///
    /// En panel de admin, mostrar árbol de tokens:
    ///
    /// TokenA (root, expired, used)
    ///   └─ TokenB (expired, used)
    ///       └─ TokenC (active)
    ///           └─ TokenD (active, current)
    ///
    /// Útil para debugging y auditoría.
    /// </remarks>
    public Guid? ParentTokenId { get; private set; }

    /// <summary>
    /// Constructor privado para EF Core.
    /// </summary>
    private RefreshToken()
    {
        Token = string.Empty; // Inicializar para evitar warning nullable
    }

    /// <summary>
    /// Factory method para crear un nuevo refresh token.
    /// </summary>
    /// <param name="userId">ID del usuario propietario.</param>
    /// <param name="token">Valor del token (generado externamente).</param>
    /// <param name="expiresAt">Fecha de expiración.</param>
    /// <param name="parentTokenId">ID del token padre (null para root).</param>
    /// <returns>Nueva instancia de RefreshToken válida.</returns>
    /// <exception cref="DomainException">Si los parámetros son inválidos.</exception>
    /// <remarks>
    /// GENERACIÓN DEL TOKEN:
    ///
    /// El token debe generarse ANTES de llamar a Create().
    /// Generación en Infrastructure/TokenService:
    ///
    /// public string GenerateRefreshToken()
    /// {
    ///     var randomBytes = new byte[64];
    ///     using var rng = RandomNumberGenerator.Create();
    ///     rng.GetBytes(randomBytes);
    ///     return Convert.ToBase64String(randomBytes);
    /// }
    ///
    /// EJEMPLO DE USO (LoginCommandHandler):
    ///
    /// // Generar tokens
    /// var accessToken = _tokenService.GenerateAccessToken(user);
    /// var refreshTokenValue = _tokenService.GenerateRefreshToken();
    ///
    /// // Crear entidad
    /// var refreshToken = RefreshToken.Create(
    ///     userId: user.Id,
    ///     token: refreshTokenValue,
    ///     expiresAt: DateTime.UtcNow.AddDays(7),
    ///     parentTokenId: null // Es el primero (root)
    /// );
    ///
    /// _context.RefreshTokens.Add(refreshToken);
    /// await _context.SaveChangesAsync();
    ///
    /// EJEMPLO DE USO (RefreshTokenCommandHandler):
    ///
    /// // Marcar el token anterior como usado
    /// oldToken.MarkAsUsed();
    ///
    /// // Generar nuevo token
    /// var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
    /// var newRefreshToken = RefreshToken.Create(
    ///     userId: oldToken.UserId,
    ///     token: newRefreshTokenValue,
    ///     expiresAt: DateTime.UtcNow.AddDays(7),
    ///     parentTokenId: oldToken.Id // Hijo del anterior
    /// );
    ///
    /// _context.RefreshTokens.Add(newRefreshToken);
    /// await _context.SaveChangesAsync();
    ///
    /// VALIDACIONES:
    /// - userId: No puede ser Guid.Empty
    /// - token: No puede ser null o vacío
    /// - expiresAt: Debe ser fecha futura
    /// </remarks>
    public static RefreshToken Create(
        Guid userId,
        string token,
        DateTime expiresAt,
        Guid? parentTokenId = null)
    {
        // Validación 1: UserId válido
        if (userId == Guid.Empty)
        {
            throw new DomainException("Refresh token must have a valid user ID");
        }

        // Validación 2: Token no vacío
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new DomainException("Refresh token value cannot be empty");
        }

        // Validación 3: ExpiresAt debe ser futuro
        if (expiresAt <= DateTime.UtcNow)
        {
            throw new DomainException("Refresh token expiration must be in the future");
        }

        // Crear refresh token con valores por defecto seguros
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            UserId = userId,
            ExpiresAt = expiresAt,
            ParentTokenId = parentTokenId,
            IsUsed = false, // Nuevo token, no usado aún
            IsRevoked = false, // Nuevo token, no revocado
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Marca el token como usado (después de refresh exitoso).
    /// </summary>
    /// <remarks>
    /// Llamar este método DESPUÉS de generar el nuevo token y ANTES de SaveChangesAsync.
    ///
    /// FLUJO EN HANDLER:
    /// 1. Validar que el token existe y no está usado/revocado
    /// 2. Generar nuevo access token
    /// 3. Generar nuevo refresh token
    /// 4. Marcar token actual como usado (este método)
    /// 5. Guardar nuevo refresh token
    /// 6. SaveChangesAsync() - ambos cambios en misma transacción
    /// 7. Retornar tokens al cliente
    ///
    /// ATOMICIDAD:
    /// Es crítico que MarkAsUsed() y la creación del nuevo token
    /// ocurran en la MISMA transacción. Si falla alguno, ambos fallan.
    ///
    /// Si no usamos transacción:
    /// - Se marca IsUsed = true
    /// - Falla creación de nuevo token
    /// - Usuario perdió su token sin reemplazo
    /// - Debe re-loguearse
    ///
    /// Con transacción (automática en EF Core):
    /// - Ambos éxito o ambos fallo
    /// - Consistencia garantizada
    /// </remarks>
    public void MarkAsUsed()
    {
        if (IsUsed)
        {
            throw new DomainException("Token is already marked as used");
        }

        if (IsRevoked)
        {
            throw new DomainException("Cannot mark revoked token as used");
        }

        IsUsed = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Revoca el token (invalidación manual).
    /// </summary>
    /// <remarks>
    /// Llamar este método para invalidar un token prematuramente.
    ///
    /// CASOS DE USO:
    ///
    /// 1. LOGOUT:
    /// var token = await _context.RefreshTokens
    ///     .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
    /// token?.Revoke();
    /// await _context.SaveChangesAsync();
    ///
    /// 2. CAMBIO DE CONTRASEÑA:
    /// var userTokens = await _context.RefreshTokens
    ///     .Where(rt => rt.UserId == userId)
    ///     .ToListAsync();
    /// userTokens.ForEach(t => t.Revoke());
    /// await _context.SaveChangesAsync();
    ///
    /// 3. DETECCIÓN DE REUSO:
    /// await RevokeTokenFamilyAsync(compromisedToken);
    ///
    /// IDEMPOTENCIA:
    /// Es seguro llamar múltiples veces. Si ya está revocado, es no-op.
    ///
    /// IRREVERSIBLE:
    /// Una vez revocado, no puede "des-revocarse".
    /// El usuario debe generar un nuevo token (login).
    /// </remarks>
    public void Revoke()
    {
        if (IsRevoked)
        {
            // Ya revocado, no hacer nada (idempotente)
            return;
        }

        IsRevoked = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Verifica si el token es válido (no expirado, usado, o revocado).
    /// </summary>
    /// <returns>true si es válido, false en caso contrario.</returns>
    /// <remarks>
    /// VALIDACIÓN COMPLETA:
    ///
    /// Un token es válido si:
    /// ✅ NO está expirado (ExpiresAt > Now)
    /// ✅ NO fue usado (IsUsed = false)
    /// ✅ NO fue revocado (IsRevoked = false)
    ///
    /// EJEMPLO DE USO:
    /// var token = await _context.RefreshTokens
    ///     .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
    ///
    /// if (token == null || !token.IsValid())
    /// {
    ///     return Result.Failure("Invalid refresh token");
    /// }
    ///
    /// // Token es válido, proceder con refresh...
    ///
    /// ALTERNATIVA - QUERY:
    /// También podemos incluir validaciones en la query:
    /// var token = await _context.RefreshTokens
    ///     .FirstOrDefaultAsync(rt =>
    ///         rt.Token == request.RefreshToken &&
    ///         !rt.IsUsed &&
    ///         !rt.IsRevoked &&
    ///         rt.ExpiresAt > DateTime.UtcNow);
    ///
    /// if (token == null)
    ///     return Failure("Invalid refresh token");
    ///
    /// Ventaja de query: Un solo hit a DB con todos los filtros.
    /// Ventaja de IsValid(): Más expresivo, mejor para testing.
    /// </remarks>
    public bool IsValid()
    {
        return !IsExpired() && !IsUsed && !IsRevoked;
    }

    /// <summary>
    /// Verifica si el token está expirado.
    /// </summary>
    /// <returns>true si está expirado, false en caso contrario.</returns>
    /// <remarks>
    /// Un token está expirado si:
    /// DateTime.UtcNow >= ExpiresAt
    ///
    /// Usar >= (no >) porque en el momento exacto de ExpiresAt ya no es válido.
    ///
    /// EJEMPLO:
    /// ExpiresAt = 2025-01-15 10:00:00 UTC
    /// Now = 2025-01-15 10:00:00 UTC
    /// IsExpired() = true (justo en el momento expira)
    /// </remarks>
    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }
}
