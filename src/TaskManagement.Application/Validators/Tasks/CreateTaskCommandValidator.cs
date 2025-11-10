using FluentValidation;
using TaskManagement.Application.UseCases.Tasks.Commands;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Validators.Tasks;

/// <summary>
/// Validator para CreateTaskCommand.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE VALIDACIÓN DE TAREAS:
///
/// CreateTaskCommand valida datos para crear nueva tarea:
/// - Title: requerido, longitud
/// - Description: opcional, longitud
/// - DueDate: opcional, debe ser futura
/// - Priority: valor enum válido
///
/// CAMPOS NO VALIDADOS:
/// - UserId: Viene del token JWT (ICurrentUserService)
/// - Status: Siempre Pending para nuevas tareas
/// - CreatedAt, UpdatedAt: Asignados automáticamente
///
/// VALIDACIÓN CONDICIONAL (When):
///
/// Para campos opcionales, usar When() para validar solo si tiene valor:
///
/// RuleFor(x => x.DueDate)
///     .GreaterThan(DateTime.UtcNow)
///     .WithMessage("Due date must be in the future")
///     .When(x => x.DueDate.HasValue);
///
/// Sin When():
/// - Si DueDate es null → validación falla (null no es > DateTime.UtcNow)
/// - Usuario forzado a especificar DueDate
///
/// Con When():
/// - Si DueDate es null → validación se omite (OK)
/// - Si DueDate tiene valor → se valida que sea futura
///
/// VALIDACIÓN DE ENUMS:
///
/// IsInEnum() valida que valor esté en el enum:
///
/// RuleFor(x => x.Priority)
///     .IsInEnum()
///     .WithMessage("Priority must be a valid value (Low, Medium, High)");
///
/// Casos:
/// - Priority = TaskPriority.Low (0) → Válido
/// - Priority = TaskPriority.Medium (1) → Válido
/// - Priority = TaskPriority.High (2) → Válido
/// - Priority = (TaskPriority)99 → Inválido (no existe)
///
/// En C#, enums son int por defecto.
/// Cliente podría enviar valor inválido: { "priority": 99 }
/// IsInEnum() lo detecta y rechaza.
///
/// IMPORTANTE: Usar JsonStringEnumConverter para serializar enums como strings:
/// { "priority": "High" } en lugar de { "priority": 2 }
///
/// Esto hace validación más intuitiva:
/// - Cliente envía "High", "Medium", "Low"
/// - System.Text.Json lo convierte a enum
/// - Si string inválido ("SuperHigh"), falla en deserialización
/// - IsInEnum() valida que valor enum sea válido
///
/// DATETIME VALIDATION:
///
/// GreaterThan(DateTime.UtcNow):
/// - Valida que fecha sea en el futuro
/// - DateTime.UtcNow = timestamp UTC actual
///
/// Problema potencial - TIMEZONE:
/// Cliente en timezone diferente puede enviar fecha que PARECE futura pero es pasada en UTC.
///
/// Ejemplo:
/// - Cliente en GMT-5 (New York)
/// - Son las 11 PM del 15 de Enero
/// - Cliente selecciona 16 de Enero 12 AM (midnight)
/// - Frontend envía: "2024-01-16T00:00:00-05:00"
/// - Servidor convierte a UTC: "2024-01-16T05:00:00Z"
/// - Validación: 05:00 AM UTC > ahora (04:00 AM UTC) → Válido ✅
///
/// Generalmente System.Text.Json maneja timezones correctamente.
/// DateTime? en C# puede ser UTC, Local, o Unspecified.
///
/// Mejor práctica:
/// - Frontend envía ISO 8601 con timezone: "2024-01-16T00:00:00-05:00"
/// - Backend convierte a UTC automáticamente
/// - Almacenar en DB como UTC
/// - Frontend convierte a timezone local para display
///
/// DESCRIPCIÓN OPCIONAL:
///
/// Description es opcional (puede ser null o vacío).
///
/// Validación:
/// RuleFor(x => x.Description)
///     .MaximumLength(2000)
///     .When(x => !string.IsNullOrEmpty(x.Description));
///
/// Casos:
/// - Description = null → Omite validación ✅
/// - Description = "" → Omite validación ✅
/// - Description = "Valid text" → Valida longitud ✅
/// - Description = "..." (2001 chars) → Error ❌
///
/// ALTERNATIVA sin When():
/// RuleFor(x => x.Description)
///     .MaximumLength(2000)
///     .When(x => x.Description != null);
///
/// Esto valida solo si no es null, pero valida strings vacíos.
/// Depende de si quieres permitir strings vacíos.
///
/// Para este proyecto: Permitir null y empty.
///
/// TÍTULOS DUPLICADOS:
///
/// Opcionalmente, validar que título sea único por usuario:
///
/// public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
/// {
///     private readonly IApplicationDbContext _context;
///     private readonly ICurrentUserService _currentUser;
///
///     public CreateTaskCommandValidator(
///         IApplicationDbContext context,
///         ICurrentUserService currentUser)
///     {
///         _context = context;
///         _currentUser = currentUser;
///
///         RuleFor(x => x.Title)
///             .MustAsync(BeUniqueTitle)
///             .WithMessage("A task with this title already exists");
///     }
///
///     private async Task<bool> BeUniqueTitle(string title, CancellationToken ct)
///     {
///         var userId = _currentUser.UserId;
///         return !await _context.Tasks.AnyAsync(
///             t => t.UserId == userId && t.Title == title && !t.IsDeleted,
///             ct
///         );
///     }
/// }
///
/// Decisión: Para este proyecto, NO validamos títulos únicos.
/// Razón: Usuario puede querer múltiples tareas con mismo título.
/// Ejemplo: "Llamar a cliente" (para diferentes clientes).
///
/// Si requisito de negocio requiere títulos únicos, agregar validación.
///
/// TRIMMING:
///
/// FluentValidation NO hace trim automático.
/// Si usuario envía "  Title  " (con espacios), pasa validación.
///
/// Opciones:
///
/// 1. Trim en handler:
/// var task = TaskItem.Create(command.Title.Trim(), ...);
///
/// 2. Transform en validator:
/// RuleFor(x => x.Title)
///     .Transform(x => x?.Trim())
///     .NotEmpty();
///
/// 3. Custom validator:
/// RuleFor(x => x.Title)
///     .Must(title => !string.IsNullOrWhiteSpace(title))
///     .WithMessage("Title cannot be empty or whitespace");
///
/// Para este proyecto: Trim en entity factory (TaskItem.Create()).
/// TaskItem.Create() ya hace trim del título.
///
/// SANITIZACIÓN:
///
/// Para prevenir XSS, sanitizar HTML en descripción:
///
/// Install-Package HtmlSanitizer
///
/// RuleFor(x => x.Description)
///     .Must(BeValidHtml)
///     .WithMessage("Description contains invalid HTML");
///
/// private bool BeValidHtml(string description)
/// {
///     if (string.IsNullOrEmpty(description)) return true;
///
///     var sanitizer = new HtmlSanitizer();
///     var sanitized = sanitizer.Sanitize(description);
///     return sanitized == description;  // Si cambió, tenía HTML malicioso
/// }
///
/// Alternativa: Sanitizar en handler en lugar de validar.
///
/// Para este proyecto: Almacenar como plain text, frontend escapa HTML.
/// Si necesitas rich text (HTML), agregar sanitización.
///
/// TESTING:
///
/// [Fact]
/// public void Validate_EmptyTitle_ReturnsError()
/// {
///     // Arrange
///     var validator = new CreateTaskCommandValidator();
///     var command = new CreateTaskCommand { Title = "" };
///
///     // Act
///     var result = validator.Validate(command);
///
///     // Assert
///     Assert.False(result.IsValid);
///     Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskCommand.Title));
/// }
///
/// [Fact]
/// public void Validate_TitleTooLong_ReturnsError()
/// {
///     var validator = new CreateTaskCommandValidator();
///     var command = new CreateTaskCommand { Title = new string('a', 201) };  // 201 chars
///
///     var result = validator.Validate(command);
///
///     Assert.False(result.IsValid);
///     Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("200"));
/// }
///
/// [Fact]
/// public void Validate_DueDateInPast_ReturnsError()
/// {
///     var validator = new CreateTaskCommandValidator();
///     var command = new CreateTaskCommand
///     {
///         Title = "Valid Title",
///         DueDate = DateTime.UtcNow.AddDays(-1)  // Ayer
///     };
///
///     var result = validator.Validate(command);
///
///     Assert.False(result.IsValid);
///     Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("future"));
/// }
///
/// [Fact]
/// public void Validate_InvalidPriority_ReturnsError()
/// {
///     var validator = new CreateTaskCommandValidator();
///     var command = new CreateTaskCommand
///     {
///         Title = "Valid Title",
///         Priority = (TaskPriority)99  // Valor inválido
///     };
///
///     var result = validator.Validate(command);
///
///     Assert.False(result.IsValid);
/// }
///
/// [Fact]
/// public void Validate_ValidCommand_Passes()
/// {
///     var validator = new CreateTaskCommandValidator();
///     var command = new CreateTaskCommand
///     {
///         Title = "Valid Task",
///         Description = "Valid description",
///         DueDate = DateTime.UtcNow.AddDays(7),
///         Priority = TaskPriority.High
///     };
///
///     var result = validator.Validate(command);
///
///     Assert.True(result.IsValid);
/// }
/// </remarks>
public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    /// <summary>
    /// Constructor que define reglas de validación.
    /// </summary>
    public CreateTaskCommandValidator()
    {
        // Validaciones para Title (requerido)
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(200)
            .WithMessage("Title must not exceed 200 characters");

        // Validaciones para Description (opcional)
        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description must not exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
        // When: Solo validar si Description tiene valor
        // Si es null o empty, omitir validación

        // Validaciones para DueDate (opcional)
        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Due date must be in the future")
            .When(x => x.DueDate.HasValue);
        // When: Solo validar si DueDate tiene valor
        // Si es null, omitir validación (tareas sin deadline son válidas)

        // Validación para Priority (requerido, enum)
        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Priority must be a valid value (Low, Medium, High)");
        // IsInEnum: Valida que valor esté definido en enum TaskPriority
        // Previene valores inválidos como (TaskPriority)99
    }
}
