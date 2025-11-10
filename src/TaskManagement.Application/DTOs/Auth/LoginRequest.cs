namespace TaskManagement.Application.DTOs.Auth;

/// <summary>
/// DTO para login de usuario.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE DTOs (Data Transfer Objects):
///
/// DTOs son objetos simples usados para transferir datos entre capas.
/// NO contienen lógica de negocio, solo propiedades.
///
/// PROBLEMA SIN DTOs:
///
/// ❌ Exponer entidades de dominio directamente en API:
///
/// [HttpPost("login")]
/// public async Task<IActionResult> Login([FromBody] User user)
/// {
///     // ¿Qué hacemos con todos los campos de User?
///     // User tiene: Id, Email, PasswordHash, Role, FailedLoginAttempts, etc.
///     // Cliente solo debería enviar Email y Password
/// }
///
/// Problemas:
/// - Cliente puede enviar campos que no debería (Id, Role, etc.)
/// - Expone estructura interna del dominio
/// - Acoplamiento entre API y dominio
/// - Riesgo de seguridad (mass assignment)
///
/// ✅ SOLUCIÓN CON DTOs:
///
/// [HttpPost("login")]
/// public async Task<IActionResult> Login([FromBody] LoginRequest request)
/// {
///     // request solo tiene Email y Password
///     // Estructura controlada y específica para este endpoint
/// }
///
/// Ventajas:
/// - Control total sobre qué datos se envían/reciben
/// - Desacoplamiento entre API y dominio
/// - Versionado (LoginRequestV1, LoginRequestV2)
/// - Validación específica por endpoint
/// - Documentación clara (Swagger)
///
/// NOMENCLATURA:
///
/// Convenciones para DTOs:
/// - *Request: Datos enviados por cliente a servidor (input)
/// - *Response: Datos enviados por servidor a cliente (output)
/// - *Dto: General purpose DTO (read/write)
/// - *Command: CQRS command (write operation)
/// - *Query: CQRS query (read operation)
///
/// Ejemplos:
/// - LoginRequest (input para login)
/// - AuthResponse (output de login con tokens)
/// - TaskDto (datos de tarea para lectura)
/// - CreateTaskCommand (comando para crear tarea)
/// - GetTasksQuery (query para obtener tareas)
///
/// LOGIN REQUEST:
///
/// Este DTO representa la solicitud de login:
/// - Email: Identificador del usuario
/// - Password: Contraseña en texto plano (será enviada por HTTPS)
///
/// IMPORTANTE: Contraseña viaja en texto plano por HTTPS.
/// - HTTPS encripta todo el tráfico (incluida contraseña)
/// - Servidor recibe contraseña en texto plano
/// - Servidor la hashea con BCrypt para comparar con hash almacenado
///
/// ❌ NO hashear en cliente:
/// var hashedPassword = BCrypt.Hash(password);  // En JavaScript
/// fetch('/api/auth/login', { body: { email, password: hashedPassword } });
///
/// Problema: El hash se convierte en la "contraseña".
/// Atacante puede capturar hash y enviarlo (pass-the-hash attack).
///
/// ✅ Enviar texto plano por HTTPS:
/// fetch('/api/auth/login', { body: { email, password } });
/// // HTTPS encripta automáticamente
///
/// SEGURIDAD:
///
/// 1. HTTPS obligatorio en producción:
///    - Previene man-in-the-middle attacks
///    - Encripta contraseña en tránsito
///    - Verifica identidad del servidor (certificado SSL)
///
/// 2. Rate limiting:
///    - Máximo 5 intentos por IP por minuto
///    - Bloqueo temporal después de intentos fallidos
///    - CAPTCHA después de 3 intentos
///
/// 3. Account lockout:
///    - 5 intentos fallidos → lockout 15 minutos
///    - Previene brute force attacks
///    - Implementado en User entity (RecordFailedLogin)
///
/// 4. Mensajes genéricos:
///    - "Invalid credentials" (no revelar si email existe)
///    - Previene user enumeration
///
/// 5. Timing attacks:
///    - BCrypt.Verify() usa timing-safe comparison
///    - No revelar información por tiempo de respuesta
///
/// VALIDACIÓN:
///
/// FluentValidation validará este DTO:
/// - Email: no vacío, formato válido, max 254 caracteres
/// - Password: no vacío, min 8 caracteres
///
/// public class LoginRequestValidator : AbstractValidator<LoginRequest>
/// {
///     public LoginRequestValidator()
///     {
///         RuleFor(x => x.Email)
///             .NotEmpty()
///             .EmailAddress()
///             .MaximumLength(254);
///
///         RuleFor(x => x.Password)
///             .NotEmpty()
///             .MinimumLength(8);
///     }
/// }
///
/// EJEMPLO DE USO:
///
/// // Cliente (JavaScript/TypeScript)
/// const loginRequest = {
///   email: "user@example.com",
///   password: "MySecurePass123!"
/// };
///
/// const response = await fetch('/api/auth/login', {
///   method: 'POST',
///   headers: { 'Content-Type': 'application/json' },
///   body: JSON.stringify(loginRequest)
/// });
///
/// const authResponse = await response.json();
/// // { accessToken: "eyJhbGci...", refreshToken: "abc123...", expiresIn: 900 }
///
/// // Servidor (C#)
/// [HttpPost("login")]
/// public async Task<IActionResult> Login([FromBody] LoginRequest request)
/// {
///     var command = new LoginCommand
///     {
///         Email = request.Email,
///         Password = request.Password
///     };
///
///     var result = await _mediator.Send(command);
///
///     if (result.IsFailure)
///         return Unauthorized(new { error = result.Error });
///
///     return Ok(result.Value);  // AuthResponse
/// }
///
/// MAPEO:
///
/// LoginRequest (API) → LoginCommand (Application)
///
/// ¿Por qué dos tipos diferentes?
/// - LoginRequest: Contrato HTTP (API Layer)
/// - LoginCommand: Intención de negocio (Application Layer)
/// - Permite evolucionar API sin afectar dominio
/// - Versionado: LoginRequestV1 → LoginCommand (mismo)
///
/// ALTERNATIVAS:
///
/// 1. Usar LoginCommand directamente en controller:
///    [HttpPost("login")]
///    public async Task<IActionResult> Login([FromBody] LoginCommand command)
///    {
///        var result = await _mediator.Send(command);
///        return Ok(result);
///    }
///
///    Pros: Menos código (sin mapeo)
///    Cons: Acopla API a Application Layer
///
/// 2. Usar clases anónimas:
///    [HttpPost("login")]
///    public async Task<IActionResult> Login([FromBody] dynamic request)
///    {
///        var command = new LoginCommand { ... };
///    }
///
///    Pros: Flexible
///    Cons: No type-safe, sin validación automática
///
/// Para este proyecto: Usar DTOs separados (mejor práctica).
///
/// TESTING:
///
/// [Fact]
/// public async Task Login_ValidCredentials_ReturnsToken()
/// {
///     // Arrange
///     var request = new LoginRequest
///     {
///         Email = "test@example.com",
///         Password = "ValidPassword123!"
///     };
///
///     // Act
///     var response = await _client.PostAsJsonAsync("/api/auth/login", request);
///
///     // Assert
///     response.EnsureSuccessStatusCode();
///     var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
///     Assert.NotNull(authResponse.AccessToken);
/// }
///
/// SWAGGER DOCUMENTATION:
///
/// Con Swashbuckle, este DTO genera documentación automática:
///
/// POST /api/auth/login
/// Request Body:
/// {
///   "email": "string",
///   "password": "string"
/// }
///
/// Agregar XML comments para mejor documentación:
/// /// <summary>Email del usuario</summary>
/// public string Email { get; set; }
///
/// Swagger muestra estos comments en UI.
///
/// DATA ANNOTATIONS vs FLUENTVALIDATION:
///
/// Data Annotations (alternativa):
/// [Required]
/// [EmailAddress]
/// public string Email { get; set; }
///
/// [Required]
/// [MinLength(8)]
/// public string Password { get; set; }
///
/// Pros: Built-in, simple
/// Cons: Mezcla validación con modelo, menos flexible
///
/// FluentValidation (este proyecto):
/// RuleFor(x => x.Email).NotEmpty().EmailAddress();
///
/// Pros: Separado del modelo, más flexible, testeable
/// Cons: Dependencia extra
///
/// Para aplicaciones complejas, FluentValidation es mejor.
/// </remarks>
public class LoginRequest
{
    /// <summary>
    /// Email del usuario.
    /// </summary>
    /// <example>user@example.com</example>
    /// <remarks>
    /// - Debe ser un email válido
    /// - Case-insensitive (se normaliza a minúsculas)
    /// - Máximo 254 caracteres (RFC 5321)
    ///
    /// Validaciones:
    /// - NotEmpty: No puede estar vacío
    /// - EmailAddress: Formato de email válido
    /// - MaximumLength(254): Límite de RFC
    /// </remarks>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario en texto plano.
    /// </summary>
    /// <example>MySecurePassword123!</example>
    /// <remarks>
    /// - Se envía en texto plano por HTTPS (seguro)
    /// - Servidor la hashea con BCrypt
    /// - NUNCA se almacena en texto plano
    ///
    /// Validaciones:
    /// - NotEmpty: No puede estar vacía
    /// - MinimumLength(8): Al menos 8 caracteres
    ///
    /// IMPORTANTE:
    /// - NO hashear en cliente
    /// - Solo enviar por HTTPS en producción
    /// - Usar contraseñas fuertes (mayúscula, minúscula, número, símbolo)
    /// </remarks>
    public string Password { get; set; } = string.Empty;
}
