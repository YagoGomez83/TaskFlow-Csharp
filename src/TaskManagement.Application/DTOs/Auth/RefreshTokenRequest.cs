namespace TaskManagement.Application.DTOs.Auth;

/// <summary>
/// DTO para refrescar el access token usando un refresh token.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE TOKEN REFRESH:
///
/// Cuando el access token expira (después de 15 minutos), el cliente usa el refresh token
/// para obtener un nuevo access token sin requerir que el usuario haga login nuevamente.
///
/// FLUJO DE TOKEN REFRESH:
///
/// 1. Cliente intenta hacer request con access token expirado:
///    GET /api/tasks
///    Headers: { Authorization: "Bearer expired_access_token" }
///    ↓
///    Response: 401 Unauthorized { error: "Token expired" }
///
/// 2. Cliente detecta 401 y refresca tokens:
///    POST /api/auth/refresh
///    {
///      "refreshToken": "xK8mP2nZ5vQ9wR1tY4uI7oP0aS3dF6gH9jK2lM5nB8vC1xZ4"
///    }
///
/// 3. Servidor valida refresh token:
///    - Busca token en base de datos
///    - Verifica que no esté expirado (< 7 días)
///    - Verifica que no esté usado (IsUsed = false)
///    - Verifica que no esté revocado (IsRevoked = false)
///
/// 4. Si válido, servidor genera nuevos tokens:
///    - Marca refresh token actual como usado (IsUsed = true)
///    - Genera nuevo access token (15 min)
///    - Genera nuevo refresh token (7 días) - TOKEN ROTATION
///    - Almacena nuevo refresh token con ParentTokenId = token actual
///
/// 5. Servidor retorna nuevos tokens:
///    Response: AuthResponse {
///      accessToken: "new_access_token",
///      refreshToken: "new_refresh_token",
///      expiresIn: 900
///    }
///
/// 6. Cliente almacena nuevos tokens y reintenta request original
///
/// TOKEN ROTATION (Seguridad):
///
/// Cada vez que se usa un refresh token, se genera uno nuevo.
/// Esto es crítico para detectar robo de tokens.
///
/// ESCENARIO NORMAL:
/// Usuario auténtico:
/// - Usa RefreshToken1 → Obtiene RefreshToken2
/// - RefreshToken1 marcado como usado
/// - Usa RefreshToken2 → Obtiene RefreshToken3
/// - RefreshToken2 marcado como usado
/// - ...
///
/// ESCENARIO DE ATAQUE:
/// Atacante roba RefreshToken1:
///
/// Usuario auténtico usa RefreshToken1 primero:
/// - RefreshToken1 → RefreshToken2
/// - RefreshToken1 marcado como usado
///
/// Atacante intenta usar RefreshToken1 robado:
/// - RefreshToken1 ya está usado (IsUsed = true)
/// - Sistema detecta REUSO → posible robo
/// - Revocar TODA la familia de tokens (ParentTokenId chain)
/// - Forzar re-login del usuario
/// - Alertar al usuario de actividad sospechosa
///
/// Atacante usa RefreshToken1 primero:
/// - RefreshToken1 → RefreshToken2
/// - RefreshToken1 marcado como usado
///
/// Usuario auténtico intenta usar RefreshToken1:
/// - RefreshToken1 ya está usado (IsUsed = true)
/// - Sistema detecta REUSO → posible robo
/// - Revocar toda la familia
/// - Usuario debe hacer login (verá mensaje de seguridad)
///
/// En ambos casos, el robo es detectado y mitigado.
///
/// TOKEN FAMILY:
///
/// RefreshTokens forman una cadena familiar:
///
/// RefreshToken1 (ParentTokenId = null)
///    ↓
/// RefreshToken2 (ParentTokenId = Token1.Id)
///    ↓
/// RefreshToken3 (ParentTokenId = Token2.Id)
///    ↓
/// RefreshToken4 (ParentTokenId = Token3.Id)
///
/// Si RefreshToken2 es reusado:
/// - Revocar RefreshToken2, RefreshToken3, RefreshToken4
/// - Opcionalmente, revocar RefreshToken1 también
///
/// Implementación:
/// public async Task RevokeTokenFamily(Guid tokenId)
/// {
///     var token = await _context.RefreshTokens.FindAsync(tokenId);
///     token.Revoke();
///
///     // Revocar todos los tokens hijo
///     var childTokens = await _context.RefreshTokens
///         .Where(t => t.ParentTokenId == tokenId)
///         .ToListAsync();
///
///     foreach (var childToken in childTokens)
///     {
///         await RevokeTokenFamily(childToken.Id);  // Recursivo
///     }
///
///     await _context.SaveChangesAsync();
/// }
///
/// VALIDACIÓN:
///
/// public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
/// {
///     public RefreshTokenRequestValidator()
///     {
///         RuleFor(x => x.RefreshToken)
///             .NotEmpty()
///             .WithMessage("Refresh token is required");
///     }
/// }
///
/// Validación simple: solo verificar que no esté vacío.
/// Validaciones complejas (expiración, revocación) se hacen en el handler.
///
/// EJEMPLO DE IMPLEMENTACIÓN:
///
/// public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
/// {
///     public async Task<Result<AuthResponse>> Handle(
///         RefreshTokenCommand request,
///         CancellationToken ct)
///     {
///         // 1. Buscar refresh token en BD
///         var refreshToken = await _context.RefreshTokens
///             .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, ct);
///
///         if (refreshToken == null)
///             return Result.Failure<AuthResponse>("Invalid refresh token");
///
///         // 2. Validar token
///         if (!refreshToken.IsValid())
///             return Result.Failure<AuthResponse>("Refresh token is not valid");
///
///         // 3. DETECTAR REUSO (token rotation)
///         if (refreshToken.IsUsed)
///         {
///             // Token ya fue usado - POSIBLE ROBO
///             _logger.LogWarning("Refresh token {TokenId} reused - possible theft", refreshToken.Id);
///
///             // Revocar toda la familia de tokens
///             await RevokeTokenFamily(refreshToken.Id);
///
///             return Result.Failure<AuthResponse>("Refresh token has been revoked due to suspicious activity");
///         }
///
///         // 4. Obtener usuario
///         var user = await _context.Users.FindAsync(refreshToken.UserId);
///         if (user == null)
///             return Result.Failure<AuthResponse>("User not found");
///
///         // 5. Marcar token actual como usado
///         refreshToken.MarkAsUsed();
///
///         // 6. Generar nuevo access token
///         var accessToken = _tokenService.GenerateAccessToken(user);
///
///         // 7. Generar nuevo refresh token (rotation)
///         var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
///         var newRefreshToken = RefreshToken.Create(
///             user.Id,
///             newRefreshTokenValue,
///             DateTime.UtcNow.AddDays(7),
///             refreshToken.Id  // ← ParentTokenId (familia)
///         );
///
///         _context.RefreshTokens.Add(newRefreshToken);
///         await _context.SaveChangesAsync(ct);
///
///         // 8. Retornar nuevos tokens
///         return Result.Success(new AuthResponse
///         {
///             AccessToken = accessToken,
///             RefreshToken = newRefreshTokenValue,
///             ExpiresIn = 900
///         });
///     }
/// }
///
/// FRONTEND IMPLEMENTATION:
///
/// // Axios interceptor para auto-refresh
/// axios.interceptors.response.use(
///   (response) => response,
///   async (error) => {
///     const originalRequest = error.config;
///
///     // Si 401 y no es retry
///     if (error.response?.status === 401 && !originalRequest._retry) {
///       originalRequest._retry = true;
///
///       try {
///         // Obtener refresh token
///         const refreshToken = localStorage.getItem('refreshToken');
///
///         if (!refreshToken) {
///           // No hay refresh token, redirigir a login
///           window.location.href = '/login';
///           return Promise.reject(error);
///         }
///
///         // Refrescar tokens
///         const response = await axios.post('/api/auth/refresh', { refreshToken });
///         const { accessToken, refreshToken: newRefreshToken } = response.data;
///
///         // Almacenar nuevos tokens
///         localStorage.setItem('accessToken', accessToken);
///         localStorage.setItem('refreshToken', newRefreshToken);
///
///         // Reintentar request original con nuevo access token
///         originalRequest.headers.Authorization = `Bearer ${accessToken}`;
///         return axios(originalRequest);
///       } catch (refreshError) {
///         // Refresh falló (token inválido o expirado)
///         localStorage.clear();
///         window.location.href = '/login';
///         return Promise.reject(refreshError);
///       }
///     }
///
///     return Promise.reject(error);
///   }
/// );
///
/// AUTOMATIC REFRESH (Proactivo):
///
/// En lugar de esperar a que access token expire, refrescarlo automáticamente antes:
///
/// // Al hacer login/refresh, calcular cuándo expira
/// const expiryTime = Date.now() + (authResponse.expiresIn * 1000);
/// localStorage.setItem('tokenExpiry', expiryTime.toString());
///
/// // Timer para refrescar 1 minuto antes de expiración
/// setInterval(async () => {
///   const expiry = parseInt(localStorage.getItem('tokenExpiry') || '0');
///   const now = Date.now();
///   const oneMinute = 60000;
///
///   if (expiry - now < oneMinute && expiry - now > 0) {
///     // Expira en menos de 1 minuto, refrescar ahora
///     const refreshToken = localStorage.getItem('refreshToken');
///     if (refreshToken) {
///       try {
///         const response = await axios.post('/api/auth/refresh', { refreshToken });
///         localStorage.setItem('accessToken', response.data.accessToken);
///         localStorage.setItem('refreshToken', response.data.refreshToken);
///         localStorage.setItem('tokenExpiry', (Date.now() + response.data.expiresIn * 1000).toString());
///       } catch (error) {
///         // Refresh falló, redirigir a login
///         localStorage.clear();
///         window.location.href = '/login';
///       }
///     }
///   }
/// }, 30000); // Check cada 30 segundos
///
/// LIMPIEZA DE TOKENS EXPIRADOS:
///
/// Background job para limpiar refresh tokens expirados de BD:
///
/// public class CleanupExpiredTokensJob
/// {
///     public async Task Execute()
///     {
///         var expiredTokens = await _context.RefreshTokens
///             .Where(t => t.ExpiresAt < DateTime.UtcNow)
///             .ToListAsync();
///
///         _context.RefreshTokens.RemoveRange(expiredTokens);
///         await _context.SaveChangesAsync();
///
///         _logger.LogInformation("Cleaned up {Count} expired tokens", expiredTokens.Count);
///     }
/// }
///
/// Ejecutar diariamente con Hangfire/Quartz:
/// RecurringJob.AddOrUpdate<CleanupExpiredTokensJob>(
///     "cleanup-expired-tokens",
///     job => job.Execute(),
///     Cron.Daily
/// );
///
/// TESTING:
///
/// [Fact]
/// public async Task RefreshToken_ValidToken_ReturnsNewTokens()
/// {
///     // Arrange
///     var user = User.Create(Email.Create("test@example.com"), "hash", UserRole.User);
///     _context.Users.Add(user);
///
///     var refreshToken = RefreshToken.Create(user.Id, "valid_token", DateTime.UtcNow.AddDays(7));
///     _context.RefreshTokens.Add(refreshToken);
///     await _context.SaveChangesAsync();
///
///     var request = new RefreshTokenRequest { RefreshToken = "valid_token" };
///
///     // Act
///     var response = await _client.PostAsJsonAsync("/api/auth/refresh", request);
///
///     // Assert
///     response.EnsureSuccessStatusCode();
///     var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
///     Assert.NotNull(authResponse.AccessToken);
///     Assert.NotEqual("valid_token", authResponse.RefreshToken);  // Nuevo token (rotation)
///
///     // Verificar token original marcado como usado
///     var oldToken = await _context.RefreshTokens.FindAsync(refreshToken.Id);
///     Assert.True(oldToken.IsUsed);
/// }
///
/// [Fact]
/// public async Task RefreshToken_ReusedToken_RevokesFamily()
/// {
///     // Arrange
///     var refreshToken = RefreshToken.Create(userId, "used_token", DateTime.UtcNow.AddDays(7));
///     refreshToken.MarkAsUsed();  // Simular token ya usado
///     _context.RefreshTokens.Add(refreshToken);
///     await _context.SaveChangesAsync();
///
///     var request = new RefreshTokenRequest { RefreshToken = "used_token" };
///
///     // Act
///     var response = await _client.PostAsJsonAsync("/api/auth/refresh", request);
///
///     // Assert
///     Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
///
///     // Verificar token revocado
///     var token = await _context.RefreshTokens.FindAsync(refreshToken.Id);
///     Assert.True(token.IsRevoked);
/// }
/// </remarks>
public class RefreshTokenRequest
{
    /// <summary>
    /// Refresh token para obtener nuevo access token.
    /// </summary>
    /// <example>xK8mP2nZ5vQ9wR1tY4uI7oP0aS3dF6gH9jK2lM5nB8vC1xZ4</example>
    /// <remarks>
    /// - String aleatorio de 32 bytes en base64
    /// - Obtenido de AuthResponse durante login o refresh anterior
    /// - Almacenado en localStorage/cookies del cliente
    /// - Debe ser válido, no expirado, no usado, no revocado
    ///
    /// Validación:
    /// - NotEmpty - Requerido
    ///
    /// Validaciones adicionales en handler:
    /// - Existe en base de datos
    /// - No está expirado (< 7 días desde creación)
    /// - No está usado (IsUsed = false) - Token rotation
    /// - No está revocado (IsRevoked = false)
    /// - Usuario asociado existe y está activo
    ///
    /// TOKEN ROTATION:
    /// Cada uso genera nuevo refresh token y marca el actual como usado.
    /// Si token usado es reusado → posible robo → revocar familia.
    /// </remarks>
    public string RefreshToken { get; set; } = string.Empty;
}
