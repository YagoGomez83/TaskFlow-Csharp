# Clean Architecture - TaskManagement API

## Introducción

Este documento describe la implementación de **Clean Architecture** (también conocida como Onion Architecture o Hexagonal Architecture) en el proyecto TaskManagement API. Esta arquitectura fue propuesta por Robert C. Martin (Uncle Bob) y se basa en la separación de responsabilidades en capas concéntricas.

## Principios Fundamentales

### 1. Independencia de Frameworks

La arquitectura no depende de la existencia de librerías externas. Los frameworks son herramientas, no restricciones.

### 2. Testeable

La lógica de negocio puede ser testeada sin UI, base de datos, servidor web o cualquier elemento externo.

### 3. Independencia de UI

La interfaz de usuario puede cambiar fácilmente sin cambiar el resto del sistema. Por ejemplo, puedes reemplazar React con Vue sin tocar la lógica de negocio.

### 4. Independencia de Base de Datos

Puedes cambiar PostgreSQL por MongoDB, SQL Server o cualquier otra base de datos sin afectar las reglas de negocio.

### 5. Independencia de Agentes Externos

Las reglas de negocio no saben nada sobre el mundo exterior. No conocen detalles de implementación.

### 6. Regla de Dependencia

**Las dependencias del código fuente solo pueden apuntar hacia adentro.**

Nada en un círculo interno puede saber nada sobre un círculo externo. En particular, el nombre de algo declarado en un círculo externo no debe ser mencionado por el código en un círculo interno.

## Arquitectura en Capas

```
┌─────────────────────────────────────────────────────────┐
│                                                         │
│          FRAMEWORKS & DRIVERS (Capa 4)                 │
│  ┌────────────────────────────────────────────────┐   │
│  │ API Controllers, Middleware, Configuration     │   │
│  │ UI (React), Database (PostgreSQL), Cache       │   │
│  └────────────────────────────────────────────────┘   │
│                                                         │
│         INTERFACE ADAPTERS (Capa 3)                    │
│  ┌────────────────────────────────────────────────┐   │
│  │ Infrastructure: EF Core, Redis, JWT Service    │   │
│  │ Repositories, External Services, Persistence   │   │
│  └────────────────────────────────────────────────┘   │
│                                                         │
│         APPLICATION BUSINESS RULES (Capa 2)            │
│  ┌────────────────────────────────────────────────┐   │
│  │ Use Cases (Commands/Queries), DTOs, Validators│   │
│  │ Application Services, Interfaces, Mappings     │   │
│  └────────────────────────────────────────────────┘   │
│                                                         │
│       ENTERPRISE BUSINESS RULES (Capa 1 - Core)        │
│  ┌────────────────────────────────────────────────┐   │
│  │ Entities, Value Objects, Domain Exceptions     │   │
│  │ Domain Events, Business Logic, Enums           │   │
│  └────────────────────────────────────────────────┘   │
│                                                         │
└─────────────────────────────────────────────────────────┘

Dependencias: 4 → 3 → 2 → 1
```

---

## Capa 1: Domain (Núcleo Empresarial)

**Proyecto:** `TaskManagement.Domain`

**Responsabilidades:**
- Contiene la lógica de negocio pura
- Define entidades con identidad
- Implementa value objects inmutables
- Declara excepciones del dominio
- No tiene dependencias externas

### Componentes

#### Entities (Entidades)

Objetos con identidad única que persisten en el tiempo.

```csharp
// User.cs
public class User : BaseEntity
{
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public bool IsLockedOut { get; private set; }
    public DateTime? LockedOutUntil { get; private set; }

    // Constructor privado - solo se crea via factory method
    private User() { }

    // Factory method con validaciones
    public static User Create(Email email, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required");

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            FailedLoginAttempts = 0,
            IsLockedOut = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Métodos que implementan reglas de negocio
    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= 5)
        {
            IsLockedOut = true;
            LockedOutUntil = DateTime.UtcNow.AddMinutes(15);
        }
    }

    public void ResetLoginAttempts()
    {
        FailedLoginAttempts = 0;
        IsLockedOut = false;
        LockedOutUntil = null;
    }

    public bool CanLogin()
    {
        if (!IsLockedOut) return true;

        if (LockedOutUntil.HasValue && DateTime.UtcNow > LockedOutUntil.Value)
        {
            ResetLoginAttempts();
            return true;
        }

        return false;
    }
}
```

#### Value Objects

Objetos inmutables sin identidad, definidos por sus atributos.

```csharp
// Email.cs
public class Email : ValueObject
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value.ToLowerInvariant().Trim();
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email cannot be empty");

        if (!IsValidEmail(value))
            throw new DomainException("Invalid email format");

        return new Email(value);
    }

    private static bool IsValidEmail(string email)
    {
        var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        return regex.IsMatch(email);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

#### Domain Exceptions

```csharp
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, Guid id)
        : base($"{entityName} with ID {id} was not found") { }
}
```

### Principios SOLID Aplicados

- **Single Responsibility**: Cada entidad tiene una sola razón para cambiar
- **Open/Closed**: Entidades abiertas para extensión, cerradas para modificación
- **Liskov Substitution**: Herencia de BaseEntity es sustituible
- **Interface Segregation**: No aplicable (no hay interfaces en Domain)
- **Dependency Inversion**: No depende de detalles de implementación

---

## Capa 2: Application (Casos de Uso)

**Proyecto:** `TaskManagement.Application`

**Responsabilidades:**
- Implementa casos de uso de la aplicación
- Orquesta el flujo de datos entre capas
- Define contratos (interfaces) para servicios externos
- Contiene DTOs para transferencia de datos
- Implementa validaciones de entrada

### Componentes

#### Use Cases (Commands/Queries)

Implementamos CQRS básico separando operaciones de escritura (Commands) y lectura (Queries).

```csharp
// CreateTaskCommand.cs
public record CreateTaskCommand(
    string Title,
    string Description,
    DateTime? DueDate,
    TaskPriority Priority
) : IRequest<Result<TaskDto>>;

// CreateTaskCommandHandler.cs
public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Result<TaskDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateTaskCommandHandler> _logger;

    public async Task<Result<TaskDto>> Handle(CreateTaskCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Creating task for user {UserId}", _currentUser.UserId);

        // Crear entidad de dominio
        var task = TaskItem.Create(
            title: request.Title,
            description: request.Description,
            userId: _currentUser.UserId,
            dueDate: request.DueDate,
            priority: request.Priority
        );

        // Persistir
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Task {TaskId} created successfully", task.Id);

        // Retornar DTO
        return Result.Success(_mapper.Map<TaskDto>(task));
    }
}
```

#### DTOs (Data Transfer Objects)

```csharp
// TaskDto.cs
public record TaskDto(
    Guid Id,
    string Title,
    string Description,
    DateTime? DueDate,
    TaskPriority Priority,
    TaskStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// CreateTaskRequest.cs
public record CreateTaskRequest(
    string Title,
    string Description,
    DateTime? DueDate,
    TaskPriority Priority
);
```

#### Validators (FluentValidation)

```csharp
// CreateTaskRequestValidator.cs
public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters")
            .Must(NotContainHtmlTags).WithMessage("Title cannot contain HTML tags");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters")
            .Must(NotContainScriptTags).WithMessage("Description cannot contain script tags");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future")
            .When(x => x.DueDate.HasValue);

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value");
    }

    private bool NotContainHtmlTags(string text)
        => !Regex.IsMatch(text ?? "", @"<[^>]+>");

    private bool NotContainScriptTags(string text)
        => !text?.Contains("<script>", StringComparison.OrdinalIgnoreCase) ?? true;
}
```

#### Interfaces (Contratos)

```csharp
// IApplicationDbContext.cs
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<TaskItem> Tasks { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

// ITokenService.cs
public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal ValidateToken(string token);
}

// ICacheService.cs
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}
```

#### MediatR Behaviors (Pipeline)

```csharp
// ValidationBehavior.cs
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, ct))
        );

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

### Dependencias

```xml
<PackageReference Include="MediatR" Version="12.0.0" />
<PackageReference Include="FluentValidation" Version="11.5.0" />
<PackageReference Include="AutoMapper" Version="12.0.1" />
```

---

## Capa 3: Infrastructure (Adaptadores de Interfaz)

**Proyecto:** `TaskManagement.Infrastructure`

**Responsabilidades:**
- Implementa interfaces definidas en Application
- Maneja persistencia con Entity Framework Core
- Implementa cache con Redis
- Genera y valida JWT tokens
- Hashea contraseñas con BCrypt

### Componentes

#### Persistence (EF Core)

```csharp
// ApplicationDbContext.cs
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Aplicar configuraciones Fluent API
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Query filters globales
        builder.Entity<TaskItem>().HasQueryFilter(t => !t.IsDeleted);
        builder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Actualizar timestamps automáticamente
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(ct);
    }
}

// TaskItemConfiguration.cs - Fluent API
public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Índices para performance
        builder.HasIndex(t => new { t.UserId, t.Status });
        builder.HasIndex(t => t.DueDate);
        builder.HasIndex(t => t.CreatedAt);

        // Relación con User
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### Cache (Redis)

```csharp
// RedisCacheService.cs
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var json = await _cache.GetStringAsync(key, ct);

            if (json == null)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from cache for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetStringAsync(key, json, options, ct);
            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing to cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
            _logger.LogDebug("Removed cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from cache for key: {Key}", key);
        }
    }
}
```

#### Security (JWT & BCrypt)

```csharp
// TokenService.cs
public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<TokenService> _logger;

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        return tokenHandler.ValidateToken(token, validationParameters, out _);
    }
}

// PasswordHasher.cs
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // BCrypt cost factor

    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

### Dependencias

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
<PackageReference Include="StackExchange.Redis" Version="2.6.122" />
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.0" />
```

---

## Capa 4: API (Presentación)

**Proyecto:** `TaskManagement.API`

**Responsabilidades:**
- Punto de entrada HTTP
- Controllers REST
- Middleware personalizado
- Configuración de servicios
- Security headers
- Rate limiting

### Componentes

#### Controllers

```csharp
// TasksController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TasksController> _logger;

    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var command = new CreateTaskCommand(
            request.Title,
            request.Description,
            request.DueDate,
            request.Priority
        );

        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Detail = result.Error });

        return CreatedAtAction(
            nameof(GetTask),
            new { id = result.Value.Id },
            result.Value
        );
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<TaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTasks([FromQuery] GetTasksRequest request)
    {
        var query = new GetTasksQuery(
            request.PageNumber,
            request.PageSize,
            request.Status,
            request.Priority,
            request.SearchTerm
        );

        var result = await _mediator.Send(query);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTask(Guid id)
    {
        var query = new GetTaskByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result.IsFailure)
            return NotFound();

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request)
    {
        var command = new UpdateTaskCommand(id, request.Title, request.Description,
                                            request.DueDate, request.Priority, request.Status);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return NotFound();

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        var command = new DeleteTaskCommand(id);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return NotFound();

        return NoContent();
    }
}
```

#### Middleware

```csharp
// SecurityHeadersMiddleware.cs
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context)
    {
        // HSTS
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");

        // CSP
        context.Response.Headers.Add("Content-Security-Policy",
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:");

        // Anti-clickjacking
        context.Response.Headers.Add("X-Frame-Options", "DENY");

        // MIME type sniffing
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

        // Referrer policy
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

        // XSS protection
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

        await _next(context);
    }
}
```

### Program.cs Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Application layer
builder.Services.AddApplicationServices();

// Infrastructure layer
builder.Services.AddInfrastructureServices(builder.Configuration);

// Authentication
builder.Services.AddAuthenticationServices(builder.Configuration);

// CORS
builder.Services.AddCorsPolicy(builder.Configuration);

// Rate Limiting
builder.Services.AddRateLimiting(builder.Configuration);

// Serilog
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSecurityHeaders();
app.UseHttpsRedirection();
app.UseCors("AllowedOrigins");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## Flujo de Datos Completo (Ejemplo: Crear Tarea)

```
1. HTTP POST /api/tasks
   │
   ├─> [SecurityHeadersMiddleware] ✓
   ├─> [RateLimitingMiddleware] ✓
   ├─> [AuthenticationMiddleware] ✓ (Valida JWT)
   │
   ▼
2. [TasksController.CreateTask]
   │
   ├─> Mapea CreateTaskRequest → CreateTaskCommand
   ├─> Envía comando a MediatR
   │
   ▼
3. [MediatR Pipeline]
   │
   ├─> [ValidationBehavior] ✓ (FluentValidation)
   ├─> [LoggingBehavior] ✓ (Serilog)
   │
   ▼
4. [CreateTaskCommandHandler] (Application Layer)
   │
   ├─> Obtiene UserId desde ICurrentUserService
   ├─> Llama a TaskItem.Create() (Domain Logic)
   ├─> Persiste vía IApplicationDbContext
   │
   ▼
5. [ApplicationDbContext] (Infrastructure Layer)
   │
   ├─> EF Core traduce a SQL
   ├─> Ejecuta INSERT en PostgreSQL
   ├─> Actualiza timestamps automáticamente
   │
   ▼
6. [CreateTaskCommandHandler]
   │
   ├─> Mapea TaskItem → TaskDto (AutoMapper)
   ├─> Retorna Result<TaskDto>
   │
   ▼
7. [TasksController]
   │
   ├─> Retorna 201 Created
   ├─> Location header: /api/tasks/{id}
   ├─> Body: TaskDto JSON
   │
   ▼
8. Cliente recibe respuesta
```

---

## Ventajas de esta Arquitectura

### 1. Testabilidad
- Domain layer: 100% testeable sin mocks
- Application layer: testeable con mocks de interfaces
- Infrastructure: testeable con base de datos en memoria

### 2. Mantenibilidad
- Separación clara de responsabilidades
- Cada capa tiene un propósito único
- Cambios en una capa no afectan otras

### 3. Escalabilidad
- Fácil agregar nuevos casos de uso
- Repositorios facilitan cambio de base de datos
- Cache layer desacopla preocupaciones de performance

### 4. Seguridad
- Validaciones en múltiples capas
- Lógica de negocio protegida en el core
- Middleware maneja security headers

### 5. Independencia
- UI puede cambiar sin afectar backend
- Base de datos puede ser reemplazada
- Frameworks son intercambiables

---

## Desventajas y Trade-offs

### 1. Complejidad Inicial
- Más archivos y proyectos que una arquitectura monolítica
- Curva de aprendizaje para desarrolladores nuevos

### 2. Over-engineering para Proyectos Pequeños
- Para APIs simples puede ser excesivo
- Más código boilerplate (DTOs, mappers)

### 3. Performance
- Múltiples capas de abstracción pueden impactar performance
- Mapeo Entity ↔ DTO tiene costo computacional
- **Mitigación**: Cache estratégico, perfilado continuo

---

## Conclusión

Clean Architecture nos proporciona una base sólida para construir aplicaciones mantenibles, testables y escalables. Aunque tiene un costo inicial en complejidad, los beneficios a largo plazo superan ampliamente este trade-off, especialmente en proyectos que esperan crecer y evolucionar.

La independencia de frameworks y bases de datos nos da la flexibilidad de adaptarnos a nuevos requisitos sin reestructuraciones masivas del código.

---

**Última actualización:** 2025-01-09
**Autor:** Senior Full-Stack Developer & DevSecOps Engineer
