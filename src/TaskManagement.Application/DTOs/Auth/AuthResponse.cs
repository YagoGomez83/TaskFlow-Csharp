namespace TaskManagement.Application.DTOs.Auth;

/// <summary>
/// DTO de respuesta para operaciones de autenticación exitosas.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE AUTH RESPONSE:
///
/// Este DTO contiene los tokens generados después de login o refresh exitoso.
/// Cliente almacena estos tokens para autenticar futuros requests.
///
/// ESTRUCTURA:
///
/// {
///   "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
///   "refreshToken": "xK8mP2nZ5vQ9wR1tY4uI7oP0aS3dF6gH9jK2lM...",
///   "expiresIn": 900
/// }
///
/// ACCESS TOKEN:
/// - JWT token firmado
/// - Contiene claims del usuario (id, email, role)
/// - Duración corta (15 minutos)
/// - Se envía en Authorization header: Bearer {token}
/// - Stateless (no almacenado en servidor)
///
/// REFRESH TOKEN:
/// - String aleatorio criptográficamente seguro
/// - Duración larga (7 días)
/// - Solo se usa para obtener nuevo access token
/// - Almacenado en base de datos (stateful)
/// - Puede ser revocado manualmente
///
/// EXPIRES IN:
/// - Tiempo en segundos hasta que access token expire
/// - 900 segundos = 15 minutos
/// - Cliente usa esto para saber cuándo refrescar
///
/// FLUJO DE AUTENTICACIÓN:
///
/// 1. LOGIN:
///    POST /api/auth/login { email, password }
///    ↓
///    Response: AuthResponse { accessToken, refreshToken, expiresIn }
///    ↓
///    Cliente almacena tokens en localStorage/sessionStorage
///
/// 2. AUTHENTICATED REQUEST:
///    GET /api/tasks
///    Headers: { Authorization: "Bearer {accessToken}" }
///    ↓
///    Server valida access token
///    ↓
///    Response: [...tasks]
///
/// 3. ACCESS TOKEN EXPIRA (después de 15 min):
///    GET /api/tasks
///    Headers: { Authorization: "Bearer {expired_token}" }
///    ↓
///    Response: 401 Unauthorized { error: "Token expired" }
///    ↓
///    Cliente detecta 401 y refresca token:
///    POST /api/auth/refresh { refreshToken }
///    ↓
///    Response: AuthResponse { accessToken, refreshToken, expiresIn }
///    ↓
///    Cliente reintenta request con nuevo access token
///
/// 4. REFRESH TOKEN EXPIRA (después de 7 días):
///    POST /api/auth/refresh { refreshToken }
///    ↓
///    Response: 401 Unauthorized { error: "Refresh token expired" }
///    ↓
///    Cliente redirige a login
///
/// ALMACENAMIENTO EN CLIENTE:
///
/// Opciones para almacenar tokens:
///
/// 1. localStorage (este proyecto):
///    localStorage.setItem('accessToken', authResponse.accessToken);
///    localStorage.setItem('refreshToken', authResponse.refreshToken);
///
///    Pros: Persiste entre tabs, no se pierde al refrescar página
///    Cons: Vulnerable a XSS (JavaScript puede acceder)
///
/// 2. sessionStorage:
///    sessionStorage.setItem('accessToken', authResponse.accessToken);
///
///    Pros: Se limpia al cerrar tab
///    Cons: Vulnerable a XSS, se pierde al cerrar tab
///
/// 3. HttpOnly Cookie:
///    Set-Cookie: accessToken=...; HttpOnly; Secure; SameSite=Strict
///
///    Pros: NO accesible desde JavaScript (protege contra XSS)
///    Cons: Vulnerable a CSRF, requiere CSRF tokens
///
/// 4. Memory (React state):
///    const [accessToken, setAccessToken] = useState(null);
///
///    Pros: Más seguro (no persiste), no vulnerable a XSS storage
///    Cons: Se pierde al refrescar página
///
/// Para MVP: localStorage (aceptable)
/// Para producción: HttpOnly Cookie + CSRF token (más seguro)
///
/// MITIGACIÓN XSS:
///
/// Si usas localStorage, proteger contra XSS:
/// - Sanitizar todo input de usuario
/// - Usar Content Security Policy (CSP)
/// - No usar eval() o innerHTML con datos de usuario
/// - Usar frameworks modernos (React/Vue/Angular) que escapan automáticamente
/// - Auditar dependencias (npm audit)
///
/// REFRESH TOKEN ROTATION:
///
/// Por seguridad, rotar refresh token cada vez que se usa:
///
/// POST /api/auth/refresh { refreshToken: "token1" }
/// ↓
/// 1. Validar token1
/// 2. Marcar token1 como usado (IsUsed = true)
/// 3. Generar nuevo access token
/// 4. Generar nuevo refresh token (token2)
/// 5. Almacenar token2 con ParentTokenId = token1.Id
/// ↓
/// Response: { accessToken: "new_access", refreshToken: "token2", ... }
///
/// Si atacante roba token1 y lo usa:
/// - token1 ya está marcado como usado
/// - Sistema detecta reuso → posible robo
/// - Revocar toda la familia de tokens
/// - Forzar re-login del usuario
///
/// EJEMPLO DE IMPLEMENTACIÓN EN FRONTEND:
///
/// // Almacenar tokens después de login
/// async function login(email: string, password: string) {
///   const response = await fetch('/api/auth/login', {
///     method: 'POST',
///     headers: { 'Content-Type': 'application/json' },
///     body: JSON.stringify({ email, password })
///   });
///
///   if (!response.ok) {
///     throw new Error('Login failed');
///   }
///
///   const authResponse: AuthResponse = await response.json();
///
///   // Almacenar tokens
///   localStorage.setItem('accessToken', authResponse.accessToken);
///   localStorage.setItem('refreshToken', authResponse.refreshToken);
///   localStorage.setItem('tokenExpiry', (Date.now() + authResponse.expiresIn * 1000).toString());
///
///   return authResponse;
/// }
///
/// // Usar access token en requests
/// async function getTasks() {
///   const accessToken = localStorage.getItem('accessToken');
///
///   const response = await fetch('/api/tasks', {
///     headers: {
///       'Authorization': `Bearer ${accessToken}`
///     }
///   });
///
///   if (response.status === 401) {
///     // Token expirado, refrescar
///     await refreshAccessToken();
///     // Reintentar request
///     return getTasks();
///   }
///
///   return response.json();
/// }
///
/// // Refrescar access token
/// async function refreshAccessToken() {
///   const refreshToken = localStorage.getItem('refreshToken');
///
///   const response = await fetch('/api/auth/refresh', {
///     method: 'POST',
///     headers: { 'Content-Type': 'application/json' },
///     body: JSON.stringify({ refreshToken })
///   });
///
///   if (!response.ok) {
///     // Refresh token inválido o expirado, redirigir a login
///     localStorage.clear();
///     window.location.href = '/login';
///     return;
///   }
///
///   const authResponse: AuthResponse = await response.json();
///
///   // Actualizar tokens
///   localStorage.setItem('accessToken', authResponse.accessToken);
///   localStorage.setItem('refreshToken', authResponse.refreshToken);
///   localStorage.setItem('tokenExpiry', (Date.now() + authResponse.expiresIn * 1000).toString());
/// }
///
/// // Auto-refresh antes de expiración
/// setInterval(() => {
///   const expiry = parseInt(localStorage.getItem('tokenExpiry') || '0');
///   const now = Date.now();
///
///   // Si expira en menos de 1 minuto, refrescar
///   if (expiry - now < 60000) {
///     refreshAccessToken();
///   }
/// }, 30000); // Check cada 30 segundos
///
/// AXIOS INTERCEPTOR (alternativa más elegante):
///
/// import axios from 'axios';
///
/// // Request interceptor: agregar token automáticamente
/// axios.interceptors.request.use(
///   (config) => {
///     const accessToken = localStorage.getItem('accessToken');
///     if (accessToken) {
///       config.headers.Authorization = `Bearer ${accessToken}`;
///     }
///     return config;
///   },
///   (error) => Promise.reject(error)
/// );
///
/// // Response interceptor: refrescar token en 401
/// axios.interceptors.response.use(
///   (response) => response,
///   async (error) => {
///     const originalRequest = error.config;
///
///     if (error.response?.status === 401 && !originalRequest._retry) {
///       originalRequest._retry = true;
///
///       try {
///         const refreshToken = localStorage.getItem('refreshToken');
///         const response = await axios.post('/api/auth/refresh', { refreshToken });
///
///         const { accessToken, refreshToken: newRefreshToken } = response.data;
///
///         localStorage.setItem('accessToken', accessToken);
///         localStorage.setItem('refreshToken', newRefreshToken);
///
///         // Reintentar request original con nuevo token
///         originalRequest.headers.Authorization = `Bearer ${accessToken}`;
///         return axios(originalRequest);
///       } catch (refreshError) {
///         // Refresh falló, redirigir a login
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
/// TESTING:
///
/// [Fact]
/// public async Task Login_ValidCredentials_ReturnsAuthResponse()
/// {
///     // Arrange
///     var request = new LoginRequest { Email = "test@example.com", Password = "Pass123!" };
///
///     // Act
///     var response = await _client.PostAsJsonAsync("/api/auth/login", request);
///
///     // Assert
///     response.EnsureSuccessStatusCode();
///     var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
///
///     Assert.NotNull(authResponse);
///     Assert.NotEmpty(authResponse.AccessToken);
///     Assert.NotEmpty(authResponse.RefreshToken);
///     Assert.Equal(900, authResponse.ExpiresIn);
/// }
///
/// SEGURIDAD:
///
/// 1. HTTPS obligatorio:
///    - Tokens viajan en headers
///    - Sin HTTPS, pueden ser interceptados
///
/// 2. No loguear tokens:
///    ❌ _logger.LogInformation($"Generated token: {accessToken}");
///    ✅ _logger.LogInformation("Generated token for user {UserId}", userId);
///
/// 3. Corta duración de access token:
///    - 15 minutos es balance entre seguridad y UX
///    - Si se roba, solo válido 15 minutos
///
/// 4. Refresh token rotation:
///    - Cada uso genera nuevo refresh token
///    - Detecta robo si token usado se reusa
///
/// 5. Revocar refresh tokens:
///    - Logout revoca refresh token
///    - Cambio de password revoca todos los tokens
///    - Usuario puede revocar sesiones activas
///
/// MONITORING:
///
/// Loguear eventos de autenticación:
/// - Login exitoso: Info
/// - Login fallido: Warning (posible ataque)
/// - Refresh exitoso: Debug
/// - Refresh fallido: Warning
/// - Token revocado: Info
/// - Token reusado: Critical (posible robo)
///
/// Alertas:
/// - Múltiples logins fallidos desde misma IP
/// - Token reusado (refresh token rotation)
/// - Refresh token usado después de logout
/// </remarks>
public class AuthResponse
{
    /// <summary>
    /// Access token JWT para autenticar requests.
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c</example>
    /// <remarks>
    /// - JWT token firmado con HS256
    /// - Contiene claims: sub (userId), email, role, exp, iat
    /// - Duración: 15 minutos
    /// - Enviar en Authorization header: Bearer {token}
    /// - Stateless: no almacenado en servidor
    /// - Verificar firma antes de confiar en claims
    /// </remarks>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token para obtener nuevo access token cuando expire.
    /// </summary>
    /// <example>xK8mP2nZ5vQ9wR1tY4uI7oP0aS3dF6gH9jK2lM5nB8vC1xZ4</example>
    /// <remarks>
    /// - String aleatorio criptográficamente seguro (32 bytes base64)
    /// - Duración: 7 días
    /// - Almacenado en base de datos (stateful)
    /// - Solo usar para refresh, NO en Authorization header
    /// - Puede ser revocado manualmente
    /// - Rotar después de cada uso (token rotation)
    /// </remarks>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Tiempo en segundos hasta que el access token expire.
    /// </summary>
    /// <example>900</example>
    /// <remarks>
    /// - 900 segundos = 15 minutos
    /// - Cliente usa esto para saber cuándo refrescar
    /// - Calcular timestamp de expiración: Date.now() + (expiresIn * 1000)
    /// - Refrescar token ANTES de expiración (ej: 1 minuto antes)
    /// - Si expira, hacer refresh con refreshToken
    /// </remarks>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Tipo de token (siempre "Bearer" para JWT).
    /// </summary>
    /// <example>Bearer</example>
    /// <remarks>
    /// - Indica el esquema de autenticación HTTP
    /// - Para JWT, siempre es "Bearer"
    /// - Usado en Authorization header: Bearer {accessToken}
    /// - Parte del estándar OAuth 2.0
    ///
    /// Opcional: Algunos clientes asumen Bearer por defecto.
    /// Incluirlo es buena práctica para compatibilidad.
    /// </remarks>
    public string TokenType { get; set; } = "Bearer";
}
