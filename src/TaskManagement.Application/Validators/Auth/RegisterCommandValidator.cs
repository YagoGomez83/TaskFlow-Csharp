using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.UseCases.Auth.Commands;

namespace TaskManagement.Application.Validators.Auth;

/// <summary>
/// Validator para RegisterCommand.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE VALIDACIÓN DE REGISTRO:
///
/// Registro requiere validaciones más estrictas que login:
/// - Email: formato, longitud, unicidad (async)
/// - Password: longitud, complejidad (mayúscula, minúscula, número, especial)
/// - ConfirmPassword: debe coincidir con Password
///
/// VALIDACIÓN ASÍNCRONA (Email Único):
///
/// MustAsync() permite ejecutar validaciones que requieren I/O:
/// - Queries a base de datos
/// - Llamadas a APIs externas
/// - Operaciones async
///
/// Ejemplo:
/// RuleFor(x => x.Email)
///     .MustAsync(BeUniqueEmail)
///     .WithMessage("Email is already registered");
///
/// private async Task<bool> BeUniqueEmail(string email, CancellationToken ct)
/// {
///     var normalized = email.Trim().ToLowerInvariant();
///     return !await _context.Users.AnyAsync(u => u.Email.Value == normalized, ct);
/// }
///
/// IMPORTANTE:
/// - Retorna true si validación PASA (email NO existe)
/// - Retorna false si validación FALLA (email ya existe)
///
/// ValidationBehavior automáticamente usa ValidateAsync() que ejecuta MustAsync.
///
/// INYECCIÓN DE DEPENDENCIAS EN VALIDATORS:
///
/// Validators pueden tener dependencias inyectadas:
///
/// public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
/// {
///     private readonly IApplicationDbContext _context;
///
///     public RegisterCommandValidator(IApplicationDbContext context)
///     {
///         _context = context;  // Inyectado por DI
///         // Definir reglas...
///     }
/// }
///
/// Registrado automáticamente:
/// services.AddValidatorsFromAssembly(typeof(RegisterCommandValidator).Assembly);
///
/// FluentValidation.DependencyInjectionExtensions maneja inyección.
///
/// PASSWORD COMPLEXITY:
///
/// Requisitos de contraseña segura:
/// 1. Mínimo 8 caracteres (mejor 12+)
/// 2. Al menos 1 mayúscula (A-Z)
/// 3. Al menos 1 minúscula (a-z)
/// 4. Al menos 1 número (0-9)
/// 5. Al menos 1 carácter especial (@$!%*?&#)
///
/// Implementación con Matches(regex):
///
/// RuleFor(x => x.Password)
///     .MinimumLength(8)
///     .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
///     .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
///     .Matches(@"\d").WithMessage("Password must contain at least one number")
///     .Matches(@"[@$!%*?&#]").WithMessage("Password must contain at least one special character");
///
/// Cada Matches es una regla separada con mensaje específico.
/// Si falla, usuario sabe exactamente qué falta.
///
/// ALTERNATIVA - Regex Complejo (no recomendado):
///
/// RuleFor(x => x.Password)
///     .Matches(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[@$!%*?&#]).{8,}$")
///     .WithMessage("Password must be at least 8 characters and contain uppercase, lowercase, number, and special character");
///
/// Problema: Mensaje genérico, usuario no sabe qué falta.
/// Mejor: Múltiples reglas con mensajes específicos.
///
/// PASSWORD STRENGTH ESTIMATION:
///
/// Para validación más avanzada, usar bibliotecas de password strength:
///
/// Install-Package Zxcvbn  // Password strength estimator
///
/// RuleFor(x => x.Password)
///     .Must(BeStrongPassword)
///     .WithMessage("Password is too weak. Try a longer password with mixed case, numbers, and symbols.");
///
/// private bool BeStrongPassword(string password)
/// {
///     var result = Zxcvbn.Core.EvaluatePassword(password);
///     return result.Score >= 3;  // Score 0-4, 3+ es aceptable
/// }
///
/// Para MVP, validaciones regex son suficientes.
///
/// CONFIRM PASSWORD:
///
/// Validar que Password y ConfirmPassword coincidan:
///
/// RuleFor(x => x.ConfirmPassword)
///     .Equal(x => x.Password)
///     .WithMessage("Passwords do not match");
///
/// Equal() compara valores.
/// Si Password = "abc" y ConfirmPassword = "xyz" → falla.
///
/// IMPORTANTE: Equal recibe lambda (x => x.Password), no valor directo.
/// Esto permite acceso al objeto completo para comparación.
///
/// COMMON PASSWORDS:
///
/// Opcionalmente, validar contra lista de contraseñas comunes:
///
/// private static readonly HashSet<string> CommonPasswords = new()
/// {
///     "password", "123456", "password123", "qwerty", "abc123",
///     "letmein", "admin", "welcome", "monkey", "dragon"
///     // ... top 1000 common passwords
/// };
///
/// RuleFor(x => x.Password)
///     .Must(password => !CommonPasswords.Contains(password.ToLower()))
///     .WithMessage("This password is too common. Please choose a more unique password.");
///
/// Para producción, cargar desde archivo (top 10k passwords).
/// Para MVP, no implementamos (fuera de scope).
///
/// HAVEIBEENPWNED INTEGRATION:
///
/// Validar contra base de datos de passwords comprometidas:
///
/// RuleFor(x => x.Password)
///     .MustAsync(NotBeCompromised)
///     .WithMessage("This password has been compromised in a data breach. Please choose a different password.");
///
/// private async Task<bool> NotBeCompromised(string password, CancellationToken ct)
/// {
///     var sha1 = ComputeSHA1(password);
///     var prefix = sha1.Substring(0, 5);
///     var suffix = sha1.Substring(5);
///
///     var response = await _httpClient.GetStringAsync(
///         $"https://api.pwnedpasswords.com/range/{prefix}", ct);
///
///     var hashes = response.Split('\n');
///     return !hashes.Any(h => h.StartsWith(suffix, StringComparison.OrdinalIgnoreCase));
/// }
///
/// HaveIBeenPwned API:
/// - 600+ millones de passwords comprometidas
/// - K-Anonymity: Solo envías primeros 5 chars del SHA1
/// - Gratuito
///
/// Para MVP, no implementamos (fuera de scope).
/// Para producción, altamente recomendado.
///
/// EMAIL NORMALIZATION:
///
/// Normalizar email antes de verificar unicidad:
///
/// private async Task<bool> BeUniqueEmail(string email, CancellationToken ct)
/// {
///     var normalized = email.Trim().ToLowerInvariant();  // ← Normalización
///     return !await _context.Users.AnyAsync(u => u.Email.Value == normalized, ct);
/// }
///
/// Sin normalización:
/// - "User@Example.com" y "user@example.com" se considerarían diferentes
/// - Usuario podría registrar múltiples cuentas con mismo email
///
/// Con normalización:
/// - Ambos se convierten a "user@example.com"
/// - Solo una cuenta por email
///
/// PERFORMANCE DE VALIDACIÓN ASYNC:
///
/// MustAsync ejecuta query a BD:
/// SELECT COUNT(*) FROM Users WHERE Email = 'user@example.com';
///
/// Performance:
/// - Con índice en Email: ~10ms
/// - Sin índice: ~100ms (tabla grande)
///
/// IMPORTANTE: Crear índice en columna Email:
/// CREATE INDEX IX_Users_Email ON Users(Email);
///
/// Sin índice, validación puede ser lenta en producción.
///
/// ORDEN DE VALIDACIONES:
///
/// Validaciones se ejecutan en orden definido:
///
/// RuleFor(x => x.Email)
///     .NotEmpty()            // 1. Verificar no vacío (rápido)
///     .EmailAddress()        // 2. Verificar formato (rápido)
///     .MaximumLength(254)    // 3. Verificar longitud (rápido)
///     .MustAsync(BeUniqueEmail);  // 4. Verificar unicidad (lento, async)
///
/// Si falla paso 1, 2, o 3 → No ejecuta paso 4 (optimización).
/// Solo hace query a BD si email tiene formato válido.
///
/// TESTING:
///
/// [Fact]
/// public void Validate_WeakPassword_ReturnsErrors()
/// {
///     // Arrange
///     var validator = new RegisterCommandValidator(_mockContext.Object);
///     var command = new RegisterCommand
///     {
///         Email = "test@example.com",
///         Password = "weak",  // Sin mayúscula, número, especial
///         ConfirmPassword = "weak"
///     };
///
///     // Act
///     var result = validator.Validate(command);
///
///     // Assert
///     Assert.False(result.IsValid);
///     Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("uppercase"));
///     Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("number"));
///     Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("special"));
/// }
///
/// [Fact]
/// public async Task Validate_DuplicateEmail_ReturnsError()
/// {
///     // Arrange
///     var existingUser = User.Create(Email.Create("existing@example.com"), "hash", UserRole.User);
///     _context.Users.Add(existingUser);
///     await _context.SaveChangesAsync();
///
///     var validator = new RegisterCommandValidator(_context);
///     var command = new RegisterCommand
///     {
///         Email = "existing@example.com",
///         Password = "ValidPass123!",
///         ConfirmPassword = "ValidPass123!"
///     };
///
///     // Act
///     var result = await validator.ValidateAsync(command);
///
///     // Assert
///     Assert.False(result.IsValid);
///     Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("already registered"));
/// }
///
/// [Fact]
/// public void Validate_PasswordMismatch_ReturnsError()
/// {
///     var validator = new RegisterCommandValidator(_mockContext.Object);
///     var command = new RegisterCommand
///     {
///         Email = "test@example.com",
///         Password = "ValidPass123!",
///         ConfirmPassword = "DifferentPass123!"
///     };
///
///     var result = validator.Validate(command);
///
///     Assert.False(result.IsValid);
///     Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("do not match"));
/// }
/// </remarks>
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Constructor que inyecta dependencias y define reglas.
    /// </summary>
    /// <param name="context">DbContext para validaciones async.</param>
    public RegisterCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        // Validaciones para Email
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Email must be a valid email address")
            .MaximumLength(254)
            .WithMessage("Email must not exceed 254 characters")
            .MustAsync(BeUniqueEmail)
            .WithMessage("Email is already registered");

        // Validaciones para Password (complejidad)
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]")
            .WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"\d")
            .WithMessage("Password must contain at least one number")
            .Matches(@"[@$!%*?&#]")
            .WithMessage("Password must contain at least one special character (@$!%*?&#)");

        // Validación para ConfirmPassword
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match");
    }

    /// <summary>
    /// Valida que el email sea único en el sistema.
    /// </summary>
    /// <param name="email">Email a validar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>true si email NO existe (validación pasa), false si ya existe (validación falla).</returns>
    /// <remarks>
    /// Validación asíncrona que consulta base de datos.
    ///
    /// Proceso:
    /// 1. Normalizar email (trim, lowercase)
    /// 2. Query a Users table
    /// 3. Retornar true si NO existe
    ///
    /// Performance:
    /// - Con índice en Email: ~10ms
    /// - Sin índice: ~100ms (lento)
    ///
    /// IMPORTANTE: Crear índice en Email column en migration.
    ///
    /// Normalización:
    /// - "User@Example.Com" → "user@example.com"
    /// - Previene duplicados con diferentes case
    ///
    /// Email.Value:
    /// - Email es Value Object con propiedad Value
    /// - Value contiene string normalizado
    /// </remarks>
    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        // Normalizar email igual que en Email.Create()
        var normalizedEmail = email.Trim().ToLowerInvariant();

        // Verificar si ya existe usuario con este email
        var exists = await _context.Users
            .AnyAsync(u => u.Email.Value == normalizedEmail, cancellationToken);

        // Retornar true si NO existe (validación pasa)
        return !exists;
    }
}
