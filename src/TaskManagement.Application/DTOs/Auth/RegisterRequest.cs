namespace TaskManagement.Application.DTOs.Auth;

/// <summary>
/// DTO para registro de nuevo usuario.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE REGISTER REQUEST:
///
/// Este DTO contiene los datos necesarios para crear una nueva cuenta de usuario.
///
/// CAMPOS REQUERIDOS:
/// - Email: Identificador único del usuario
/// - Password: Contraseña en texto plano (se hasheará con BCrypt)
/// - ConfirmPassword: Confirmación de contraseña (prevenir typos)
///
/// FLUJO DE REGISTRO:
///
/// 1. Cliente envía RegisterRequest:
///    POST /api/auth/register
///    {
///      "email": "newuser@example.com",
///      "password": "SecurePass123!",
///      "confirmPassword": "SecurePass123!"
///    }
///
/// 2. Servidor valida:
///    - Email no está vacío y formato válido
///    - Email no existe en base de datos
///    - Password cumple requisitos (longitud, complejidad)
///    - Password == ConfirmPassword
///
/// 3. Servidor crea usuario:
///    - Hashea password con BCrypt
///    - Crea entidad User
///    - Guarda en base de datos
///    - Asigna rol User (por defecto)
///
/// 4. Servidor puede:
///    Opción A: Retornar éxito sin tokens (usuario debe hacer login)
///    Opción B: Retornar AuthResponse con tokens (auto-login)
///
///    Para este proyecto: Opción B (auto-login después de registro)
///
/// VALIDACIÓN DE EMAIL:
///
/// Validaciones:
/// 1. No vacío
/// 2. Formato válido (regex)
/// 3. Máximo 254 caracteres (RFC 5321)
/// 4. Email único (no existe en BD)
///
/// public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
/// {
///     private readonly IApplicationDbContext _context;
///
///     public RegisterRequestValidator(IApplicationDbContext context)
///     {
///         _context = context;
///
///         RuleFor(x => x.Email)
///             .NotEmpty()
///             .WithMessage("Email is required")
///             .EmailAddress()
///             .WithMessage("Email must be a valid email address")
///             .MaximumLength(254)
///             .WithMessage("Email must not exceed 254 characters")
///             .MustAsync(BeUniqueEmail)
///             .WithMessage("Email is already registered");
///     }
///
///     private async Task<bool> BeUniqueEmail(string email, CancellationToken ct)
///     {
///         var normalizedEmail = email.Trim().ToLowerInvariant();
///         return !await _context.Users.AnyAsync(u => u.Email.Value == normalizedEmail, ct);
///     }
/// }
///
/// VALIDACIÓN DE PASSWORD:
///
/// Requisitos de contraseña fuerte:
/// - Mínimo 8 caracteres (mejor 12+)
/// - Al menos 1 mayúscula
/// - Al menos 1 minúscula
/// - Al menos 1 número
/// - Al menos 1 carácter especial (@$!%*?&, etc.)
///
/// RuleFor(x => x.Password)
///     .NotEmpty()
///     .WithMessage("Password is required")
///     .MinimumLength(8)
///     .WithMessage("Password must be at least 8 characters")
///     .Matches(@"[A-Z]")
///     .WithMessage("Password must contain at least one uppercase letter")
///     .Matches(@"[a-z]")
///     .WithMessage("Password must contain at least one lowercase letter")
///     .Matches(@"\d")
///     .WithMessage("Password must contain at least one number")
///     .Matches(@"[@$!%*?&#]")
///     .WithMessage("Password must contain at least one special character (@$!%*?&#)");
///
/// VALIDACIÓN DE CONFIRM PASSWORD:
///
/// Password y ConfirmPassword deben coincidir:
///
/// RuleFor(x => x.ConfirmPassword)
///     .Equal(x => x.Password)
///     .WithMessage("Passwords do not match");
///
/// ALTERNATIVA - Sin ConfirmPassword:
///
/// Algunos APIs no usan ConfirmPassword, dejan la validación al frontend.
///
/// Pros: Menos campos en request
/// Cons: Usuarios pueden hacer typo y no darse cuenta
///
/// Mejor práctica: Validar en frontend Y backend.
/// - Frontend: UX (feedback inmediato)
/// - Backend: Seguridad (validación final)
///
/// PASSWORDS COMPROMETIDAS:
///
/// Validación avanzada: Verificar contra base de datos de passwords comprometidas.
///
/// API HaveIBeenPwned:
/// - 600+ millones de passwords comprometidas
/// - API gratuita
/// - K-Anonymity: No envías password completo
///
/// public async Task<bool> IsPasswordCompromised(string password)
/// {
///     var sha1 = ComputeSHA1(password);
///     var prefix = sha1.Substring(0, 5);
///     var suffix = sha1.Substring(5);
///
///     var response = await _httpClient.GetStringAsync($"https://api.pwnedpasswords.com/range/{prefix}");
///     var hashes = response.Split('\n');
///
///     return hashes.Any(h => h.StartsWith(suffix, StringComparison.OrdinalIgnoreCase));
/// }
///
/// RuleFor(x => x.Password)
///     .MustAsync((password, ct) => IsPasswordNotCompromised(password))
///     .WithMessage("This password has been compromised in a data breach. Please choose a different password.");
///
/// Para MVP, no implementamos esto (fuera de scope).
/// Para producción, altamente recomendado.
///
/// EMAIL VERIFICATION:
///
/// Después de registro, enviar email de verificación:
///
/// 1. Usuario se registra
/// 2. Servidor crea usuario con EmailVerified = false
/// 3. Servidor genera verification token
/// 4. Servidor envía email con link: /verify?token=abc123
/// 5. Usuario hace click en link
/// 6. Servidor valida token y marca EmailVerified = true
///
/// Beneficios:
/// - Confirma que email es real
/// - Previene spam registrations
/// - Usuario puede recuperar cuenta si olvida password
///
/// Para MVP, no implementamos verificación (fuera de scope).
/// Para producción, recomendado.
///
/// RATE LIMITING:
///
/// Prevenir spam de registros:
/// - Máximo 3 registros por IP por hora
/// - CAPTCHA después de 2 intentos
/// - Bloquear emails temporales (mailinator, guerrillamail)
///
/// Implementación con AspNetCoreRateLimit:
/// "ClientRateLimiting": {
///   "EnableEndpointRateLimiting": true,
///   "ClientIdHeader": "X-ClientId",
///   "EndpointWhitelist": [],
///   "ClientWhitelist": [],
///   "GeneralRules": [
///     {
///       "Endpoint": "POST:/api/auth/register",
///       "Period": "1h",
///       "Limit": 3
///     }
///   ]
/// }
///
/// GDPR COMPLIANCE:
///
/// Si opera en Europa, cumplir con GDPR:
/// - Consentimiento explícito para procesar datos
/// - Checkbox: "I agree to Terms and Privacy Policy"
/// - Permitir eliminar cuenta y datos (right to be forgotten)
/// - Política de privacidad clara
///
/// public class RegisterRequest
/// {
///     public string Email { get; set; }
///     public string Password { get; set; }
///     public bool AcceptedTerms { get; set; }  // ← GDPR
/// }
///
/// RuleFor(x => x.AcceptedTerms)
///     .Equal(true)
///     .WithMessage("You must accept the Terms and Privacy Policy");
///
/// Para este proyecto, no implementamos (fuera de scope).
///
/// EJEMPLO DE IMPLEMENTACIÓN:
///
/// // Handler
/// public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
/// {
///     public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken ct)
///     {
///         // 1. Verificar que email no existe
///         var emailExists = await _context.Users.AnyAsync(
///             u => u.Email.Value == request.Email.ToLowerInvariant(), ct);
///
///         if (emailExists)
///             return Result.Failure<AuthResponse>("Email is already registered");
///
///         // 2. Hashear contraseña
///         var passwordHash = _passwordHasher.Hash(request.Password);
///
///         // 3. Crear usuario
///         var email = Email.Create(request.Email);
///         var user = User.Create(email, passwordHash, UserRole.User);
///
///         _context.Users.Add(user);
///         await _context.SaveChangesAsync(ct);
///
///         // 4. Generar tokens (auto-login)
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
///         await _context.SaveChangesAsync(ct);
///
///         // 5. Retornar tokens
///         return Result.Success(new AuthResponse
///         {
///             AccessToken = accessToken,
///             RefreshToken = refreshTokenValue,
///             ExpiresIn = 900
///         });
///     }
/// }
///
/// FRONTEND EXAMPLE:
///
/// async function register(email: string, password: string, confirmPassword: string) {
///   const response = await fetch('/api/auth/register', {
///     method: 'POST',
///     headers: { 'Content-Type': 'application/json' },
///     body: JSON.stringify({ email, password, confirmPassword })
///   });
///
///   if (!response.ok) {
///     const error = await response.json();
///     throw new Error(error.error);
///   }
///
///   const authResponse: AuthResponse = await response.json();
///
///   // Auto-login: almacenar tokens
///   localStorage.setItem('accessToken', authResponse.accessToken);
///   localStorage.setItem('refreshToken', authResponse.refreshToken);
///
///   // Redirigir a dashboard
///   window.location.href = '/dashboard';
/// }
///
/// TESTING:
///
/// [Fact]
/// public async Task Register_ValidData_CreatesUserAndReturnsTokens()
/// {
///     // Arrange
///     var request = new RegisterRequest
///     {
///         Email = "newuser@example.com",
///         Password = "SecurePass123!",
///         ConfirmPassword = "SecurePass123!"
///     };
///
///     // Act
///     var response = await _client.PostAsJsonAsync("/api/auth/register", request);
///
///     // Assert
///     response.EnsureSuccessStatusCode();
///     var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
///     Assert.NotNull(authResponse.AccessToken);
///
///     // Verificar usuario creado en BD
///     var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Value == "newuser@example.com");
///     Assert.NotNull(user);
///     Assert.Equal(UserRole.User, user.Role);
/// }
///
/// [Fact]
/// public async Task Register_DuplicateEmail_ReturnsBadRequest()
/// {
///     // Arrange
///     var existingUser = User.Create(Email.Create("existing@example.com"), "hash", UserRole.User);
///     _context.Users.Add(existingUser);
///     await _context.SaveChangesAsync();
///
///     var request = new RegisterRequest
///     {
///         Email = "existing@example.com",
///         Password = "SecurePass123!",
///         ConfirmPassword = "SecurePass123!"
///     };
///
///     // Act
///     var response = await _client.PostAsJsonAsync("/api/auth/register", request);
///
///     // Assert
///     Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
/// }
///
/// [Fact]
/// public async Task Register_PasswordMismatch_ReturnsBadRequest()
/// {
///     // Arrange
///     var request = new RegisterRequest
///     {
///         Email = "newuser@example.com",
///         Password = "SecurePass123!",
///         ConfirmPassword = "DifferentPass123!"  // ← No coincide
///     };
///
///     // Act
///     var response = await _client.PostAsJsonAsync("/api/auth/register", request);
///
///     // Assert
///     Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
/// }
/// </remarks>
public class RegisterRequest
{
    /// <summary>
    /// Email del nuevo usuario (será su username).
    /// </summary>
    /// <example>newuser@example.com</example>
    /// <remarks>
    /// - Debe ser único en el sistema
    /// - Formato de email válido
    /// - Case-insensitive (se normaliza a minúsculas)
    /// - Máximo 254 caracteres
    ///
    /// Validaciones:
    /// - NotEmpty
    /// - EmailAddress
    /// - MaximumLength(254)
    /// - MustAsync(BeUniqueEmail) - No existe en BD
    /// </remarks>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña del nuevo usuario.
    /// </summary>
    /// <example>MySecurePassword123!</example>
    /// <remarks>
    /// - Se hasheará con BCrypt antes de almacenar
    /// - NUNCA se almacena en texto plano
    ///
    /// Requisitos:
    /// - Mínimo 8 caracteres
    /// - Al menos 1 mayúscula
    /// - Al menos 1 minúscula
    /// - Al menos 1 número
    /// - Al menos 1 carácter especial
    ///
    /// Validaciones:
    /// - NotEmpty
    /// - MinimumLength(8)
    /// - Matches([A-Z]) - Mayúscula
    /// - Matches([a-z]) - Minúscula
    /// - Matches(\d) - Número
    /// - Matches([@$!%*?&#]) - Especial
    /// </remarks>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Confirmación de contraseña (debe coincidir con Password).
    /// </summary>
    /// <example>MySecurePassword123!</example>
    /// <remarks>
    /// - Previene errores de tipeo
    /// - Debe ser exactamente igual a Password
    ///
    /// Validación:
    /// - Equal(Password) - Debe coincidir
    ///
    /// ALTERNATIVA:
    /// Algunos APIs no usan este campo, confiando en validación de frontend.
    /// Mejor práctica: Validar en ambos lados.
    /// </remarks>
    public string ConfirmPassword { get; set; } = string.Empty;
}
