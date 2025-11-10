using FluentValidation;
using TaskManagement.Application.UseCases.Auth.Commands;

namespace TaskManagement.Application.Validators.Auth;

/// <summary>
/// Validator para LoginCommand.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE FLUENTVALIDATION:
///
/// FluentValidation es una biblioteca para validar objetos en .NET usando sintaxis fluida.
/// Es una alternativa más potente y flexible a Data Annotations.
///
/// DATA ANNOTATIONS vs FLUENTVALIDATION:
///
/// Data Annotations (built-in):
/// public class LoginCommand
/// {
///     [Required]
///     [EmailAddress]
///     [MaxLength(254)]
///     public string Email { get; set; }
///
///     [Required]
///     [MinLength(8)]
///     public string Password { get; set; }
/// }
///
/// Problemas:
/// - Atributos mezclados con modelo (viola SRP)
/// - Difícil de testear
/// - Mensajes de error genéricos
/// - Limitado para validaciones complejas
/// - No soporta validaciones async fácilmente
///
/// FluentValidation (este proyecto):
/// public class LoginCommandValidator : AbstractValidator<LoginCommand>
/// {
///     public LoginCommandValidator()
///     {
///         RuleFor(x => x.Email)
///             .NotEmpty()
///             .WithMessage("Email is required")
///             .EmailAddress()
///             .WithMessage("Email must be a valid email address")
///             .MaximumLength(254)
///             .WithMessage("Email must not exceed 254 characters");
///
///         RuleFor(x => x.Password)
///             .NotEmpty()
///             .WithMessage("Password is required")
///             .MinimumLength(8)
///             .WithMessage("Password must be at least 8 characters");
///     }
/// }
///
/// Ventajas:
/// - Separado del modelo (SRP)
/// - Fácil de testear
/// - Mensajes de error customizables
/// - Validaciones complejas (condicionales, dependencias)
/// - Soporta validaciones async (queries a BD)
/// - Composición (reutilizar reglas)
/// - Intellisense y type-safety
///
/// SINTAXIS FLUIDA:
///
/// RuleFor(x => x.Email)           ← Propiedad a validar
///     .NotEmpty()                  ← Regla 1
///     .WithMessage("...")          ← Mensaje custom para regla 1
///     .EmailAddress()              ← Regla 2
///     .WithMessage("...")          ← Mensaje custom para regla 2
///     .MaximumLength(254);         ← Regla 3 (usa mensaje default)
///
/// Cada regla se ejecuta en orden.
/// Si una regla falla, las siguientes NO se ejecutan (fail-fast).
///
/// REGLAS COMUNES:
///
/// 1. NotEmpty():
///    - Para strings: no null, no vacío, no solo whitespace
///    - Para colecciones: no null, no vacío
///    - Para nullable types: no null
///
/// 2. NotNull():
///    - Solo verifica que no sea null
///    - String vacío pasa validación
///
/// 3. EmailAddress():
///    - Valida formato de email
///    - Usa regex básico (no 100% preciso con RFC)
///    - Suficiente para la mayoría de casos
///
/// 4. MinimumLength(n) / MaximumLength(n):
///    - Para strings: longitud de caracteres
///    - Para colecciones: cantidad de elementos
///
/// 5. Matches(regex):
///    - Valida contra expresión regular
///    - Ej: Matches(@"^[A-Z]") → Debe empezar con mayúscula
///
/// 6. Must(predicate):
///    - Custom validation logic
///    - Ej: Must(x => x.StartsWith("test"))
///
/// 7. MustAsync(async predicate):
///    - Custom validation async
///    - Ej: MustAsync(async (email, ct) => !await _context.Users.AnyAsync(...))
///
/// MENSAJES DE ERROR:
///
/// Default:
/// RuleFor(x => x.Email).NotEmpty();
/// → "Email must not be empty."
///
/// Custom:
/// RuleFor(x => x.Email)
///     .NotEmpty()
///     .WithMessage("Email is required");
/// → "Email is required"
///
/// Con placeholders:
/// RuleFor(x => x.Email)
///     .MaximumLength(254)
///     .WithMessage("Email must not exceed {MaxLength} characters");
/// → "Email must not exceed 254 characters"
///
/// Placeholders disponibles:
/// - {PropertyName}: Nombre de la propiedad
/// - {PropertyValue}: Valor actual
/// - {MaxLength}, {MinLength}: Para validaciones de longitud
/// - {ComparisonValue}: Para comparaciones
///
/// EJECUCIÓN:
///
/// FluentValidation se integra con MediatR mediante ValidationBehavior.
/// NO necesitas llamar validator manualmente en handlers.
///
/// Flujo automático:
/// 1. Controller recibe request
/// 2. MediatR procesa command
/// 3. ValidationBehavior intercepta
/// 4. ValidationBehavior busca IValidator<TCommand>
/// 5. ValidationBehavior ejecuta ValidateAsync()
/// 6. Si falla: retorna Result.Failure con errores
/// 7. Si pasa: continúa al handler
///
/// IMPORTANTE: Esto es AUTOMÁTICO. No escribir código de validación en handlers.
///
/// VALIDACIÓN MANUAL (solo para testing):
///
/// var validator = new LoginCommandValidator();
/// var command = new LoginCommand { Email = "", Password = "123" };
///
/// var result = validator.Validate(command);
///
/// if (!result.IsValid)
/// {
///     foreach (var error in result.Errors)
///     {
///         Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
///     }
/// }
///
/// Output:
/// Email: Email is required
/// Password: Password must be at least 8 characters
///
/// ASYNC VALIDATION:
///
/// Para validaciones que requieren consultar BD:
///
/// public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
/// {
///     private readonly IApplicationDbContext _context;
///
///     public RegisterCommandValidator(IApplicationDbContext context)
///     {
///         _context = context;
///
///         RuleFor(x => x.Email)
///             .MustAsync(BeUniqueEmail)
///             .WithMessage("Email is already registered");
///     }
///
///     private async Task<bool> BeUniqueEmail(string email, CancellationToken ct)
///     {
///         var normalized = email.Trim().ToLowerInvariant();
///         return !await _context.Users.AnyAsync(u => u.Email.Value == normalized, ct);
///     }
/// }
///
/// ValidationBehavior automáticamente usa ValidateAsync() que soporta async.
///
/// CONDICIONAL VALIDATION:
///
/// Validar solo cuando se cumple condición:
///
/// RuleFor(x => x.DueDate)
///     .GreaterThan(DateTime.UtcNow)
///     .WithMessage("Due date must be in the future")
///     .When(x => x.DueDate.HasValue);  // ← Solo si no es null
///
/// Sin When(), validación falla si DueDate es null.
/// Con When(), validación se omite si DueDate es null.
///
/// VALIDACIÓN DE COLECCIONES:
///
/// RuleForEach(x => x.Items)
///     .SetValidator(new ItemValidator());
///
/// Valida cada elemento de la colección con ItemValidator.
///
/// CUSTOM VALIDATORS:
///
/// Reutilizar lógica de validación:
///
/// public class EmailValidator : AbstractValidator<string>
/// {
///     public EmailValidator()
///     {
///         RuleFor(x => x)
///             .NotEmpty()
///             .EmailAddress()
///             .MaximumLength(254);
///     }
/// }
///
/// Usar en otros validators:
/// RuleFor(x => x.Email).SetValidator(new EmailValidator());
///
/// O más simple con extensión:
/// public static class CustomValidators
/// {
///     public static IRuleBuilderOptions<T, string> ValidEmail<T>(
///         this IRuleBuilder<T, string> ruleBuilder)
///     {
///         return ruleBuilder
///             .NotEmpty()
///             .EmailAddress()
///             .MaximumLength(254);
///     }
/// }
///
/// Uso:
/// RuleFor(x => x.Email).ValidEmail();
///
/// TESTING:
///
/// [Fact]
/// public void Validate_EmptyEmail_ReturnsError()
/// {
///     // Arrange
///     var validator = new LoginCommandValidator();
///     var command = new LoginCommand { Email = "", Password = "ValidPass123" };
///
///     // Act
///     var result = validator.Validate(command);
///
///     // Assert
///     Assert.False(result.IsValid);
///     Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCommand.Email));
///     Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Email is required"));
/// }
///
/// [Fact]
/// public void Validate_InvalidEmailFormat_ReturnsError()
/// {
///     var validator = new LoginCommandValidator();
///     var command = new LoginCommand { Email = "invalid-email", Password = "ValidPass123" };
///
///     var result = validator.Validate(command);
///
///     Assert.False(result.IsValid);
///     Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("valid email"));
/// }
///
/// [Fact]
/// public void Validate_ValidCommand_Passes()
/// {
///     var validator = new LoginCommandValidator();
///     var command = new LoginCommand
///     {
///         Email = "user@example.com",
///         Password = "ValidPass123"
///     };
///
///     var result = validator.Validate(command);
///
///     Assert.True(result.IsValid);
///     Assert.Empty(result.Errors);
/// }
///
/// REGISTRO EN DI:
///
/// Registrar todos los validators del assembly:
/// services.AddValidatorsFromAssembly(typeof(LoginCommandValidator).Assembly);
///
/// Esto automáticamente registra:
/// - LoginCommandValidator como IValidator<LoginCommand>
/// - RegisterCommandValidator como IValidator<RegisterCommand>
/// - ... todos los validators
///
/// ValidationBehavior los inyecta automáticamente.
///
/// LOGIN VALIDATION:
///
/// Para login, las validaciones son básicas:
/// - Email: no vacío, formato válido, longitud
/// - Password: no vacío, longitud mínima
///
/// NO validamos:
/// - Email existe (eso se hace en handler)
/// - Password es correcta (eso se hace en handler)
///
/// ¿Por qué?
/// - Validaciones de formato ≠ Validaciones de negocio
/// - Validator: "¿Los datos tienen formato correcto?"
/// - Handler: "¿Los datos son correctos para el caso de uso?"
///
/// SEGURIDAD - USER ENUMERATION:
///
/// ❌ NO revelar si email existe:
/// RuleFor(x => x.Email)
///     .MustAsync(EmailExists)
///     .WithMessage("Email does not exist");  // ← Revela si email está registrado
///
/// Atacante puede enumerar emails registrados probando múltiples emails.
///
/// ✅ Validar formato, verificar existencia en handler:
/// RuleFor(x => x.Email).EmailAddress();
///
/// En handler:
/// var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
/// if (user == null || !_passwordHasher.Verify(password, user.PasswordHash))
///     return Result.Failure("Invalid credentials");  // Genérico
///
/// Mensaje genérico no revela si problema es email o password.
///
/// PERFORMANCE:
///
/// Validaciones sync son extremadamente rápidas (< 1ms).
/// Validaciones async (con queries) pueden ser más lentas (10-50ms).
///
/// Para validaciones costosas:
/// - Usar índices en BD
/// - Considerar cache si aplica
/// - Limitar cantidad de validaciones async
///
/// INTERNATIONALIZACIÓN (i18n):
///
/// FluentValidation soporta mensajes localizados:
///
/// RuleFor(x => x.Email)
///     .NotEmpty()
///     .WithMessage(x => Localizer["EmailRequired"]);
///
/// Usar resource files para diferentes idiomas.
/// Para MVP, usamos inglés hardcoded.
/// </remarks>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>
    /// Constructor que define reglas de validación.
    /// </summary>
    /// <remarks>
    /// Define validaciones para LoginCommand:
    /// - Email: no vacío, formato válido, longitud
    /// - Password: no vacío, longitud mínima
    ///
    /// Estas son validaciones de FORMATO, no de negocio.
    /// Handler verifica si credenciales son correctas.
    /// </remarks>
    public LoginCommandValidator()
    {
        // Validaciones para Email
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Email must be a valid email address")
            .MaximumLength(254)
            .WithMessage("Email must not exceed 254 characters");

        // Validaciones para Password
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters");

        // NOTA: No validamos complejidad de password en login
        // Solo longitud mínima
        // Complejidad se valida en registro, no en login
        // (Usuario puede tener password antigua que no cumple reglas nuevas)
    }
}
