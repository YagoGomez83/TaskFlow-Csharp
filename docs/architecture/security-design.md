# Security Design - TaskManagement API

## Introducción

Este documento describe la estrategia de seguridad implementada en TaskManagement API, siguiendo las mejores prácticas de **DevSecOps** y **Secure Coding**. Cubrimos las mitigaciones para las vulnerabilidades del **OWASP Top 10 2021** y los controles de seguridad aplicados en cada capa de la aplicación.

---

## Filosofía de Seguridad

### Defense in Depth (Defensa en Profundidad)

Implementamos múltiples capas de seguridad:

```
┌─────────────────────────────────────────┐
│  Network Layer (HTTPS, Firewall)       │
├─────────────────────────────────────────┤
│  Application Layer (Rate Limiting)     │
├─────────────────────────────────────────┤
│  Authentication (JWT)                   │
├─────────────────────────────────────────┤
│  Authorization (RBAC)                   │
├─────────────────────────────────────────┤
│  Input Validation (FluentValidation)   │
├─────────────────────────────────────────┤
│  Data Layer (ORM, Prepared Statements) │
├─────────────────────────────────────────┤
│  Logging & Monitoring (Serilog)        │
└─────────────────────────────────────────┘
```

### Security by Design

- **Fail Secure**: Si algo falla, el sistema se cierra, no se abre
- **Least Privilege**: Los usuarios solo tienen los permisos mínimos necesarios
- **Complete Mediation**: Cada request es autenticado y autorizado
- **Security in Every Sprint**: No dejamos seguridad para el final

---

## OWASP Top 10 2021 - Mitigaciones

### A01:2021 - Broken Access Control

**Riesgo:** Usuarios acceden a recursos que no deberían.

#### Mitigaciones Implementadas:

**1. JWT Authentication**
```csharp
[Authorize] // Requiere token válido
public class TasksController : ControllerBase
{
    // Solo accesible con JWT válido
}
```

**2. Role-Based Access Control (RBAC)**
```csharp
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetAllUsersTasks()
{
    // Solo admins pueden ver tareas de todos
}

[Authorize(Roles = "User,Admin")]
public async Task<IActionResult> GetMyTasks()
{
    // Users solo ven sus propias tareas
}
```

**3. Resource-Level Authorization**
```csharp
// GetTaskByIdQueryHandler.cs
public async Task<Result<TaskDto>> Handle(GetTaskByIdQuery request, CancellationToken ct)
{
    var task = await _context.Tasks.FindAsync(request.Id);

    if (task == null)
        return Result.Failure<TaskDto>("Task not found");

    // Verificar que la tarea pertenece al usuario
    if (task.UserId != _currentUser.UserId && _currentUser.Role != UserRole.Admin)
        return Result.Failure<TaskDto>("Access denied");

    return Result.Success(_mapper.Map<TaskDto>(task));
}
```

**4. Claims-Based Authorization**
```csharp
public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    UserRole Role { get; }
    bool IsAuthenticated { get; }
}

// CurrentUserService.cs
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Guid UserId => Guid.Parse(_httpContextAccessor.HttpContext?.User
        ?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    public UserRole Role => Enum.Parse<UserRole>(_httpContextAccessor.HttpContext?.User
        ?.FindFirstValue(ClaimTypes.Role) ?? "User");
}
```

---

### A02:2021 - Cryptographic Failures

**Riesgo:** Exposición de datos sensibles por criptografía débil o ausente.

#### Mitigaciones Implementadas:

**1. Password Hashing con BCrypt**
```csharp
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // 2^12 = 4096 iteraciones

    public string Hash(string password)
    {
        // BCrypt genera salt automáticamente
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        // Timing-safe comparison
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

**2. Secrets Management**
```csharp
// Development: dotnet user-secrets
dotnet user-secrets set "JwtSettings:Secret" "your-secret-key-minimum-32-chars"

// Production: Variables de entorno
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");

// appsettings.json NO contiene secrets
{
  "JwtSettings": {
    "Secret": "", // Vacío - se obtiene de user-secrets o env vars
    "Issuer": "TaskManagementAPI",
    "Audience": "TaskManagementClient"
  }
}
```

**3. HTTPS Enforcement**
```csharp
// Program.cs
app.UseHttpsRedirection(); // Redirige HTTP → HTTPS

// appsettings.Production.json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:443",
        "Certificate": {
          "Path": "/app/certs/certificate.pfx",
          "Password": "ENV:CERT_PASSWORD"
        }
      }
    }
  }
}
```

**4. Datos Sensibles en Logs**
```csharp
// NUNCA loguear:
_logger.LogInformation("User login: {Email}, Password: {Password}", email, password); // ❌

// CORRECTO:
_logger.LogInformation("User login attempt: {Email}", email); // ✅

// Serilog destructuring seguro
public class UserDto
{
    public string Email { get; set; }

    [NotLogged] // Custom attribute
    public string Password { get; set; }
}
```

**5. JWT Token Storage**
```typescript
// Frontend: NUNCA en localStorage si hay riesgo XSS alto
// Preferir httpOnly cookies o sessionStorage con precaución

// API: Configurar cookies seguras
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // No tolerancia de tiempo
        };
    });
```

---

### A03:2021 - Injection

**Riesgo:** SQL Injection, NoSQL Injection, Command Injection.

#### Mitigaciones Implementadas:

**1. ORM con Parameterización (Entity Framework Core)**
```csharp
// ✅ SEGURO - EF Core usa parámetros automáticamente
var tasks = await _context.Tasks
    .Where(t => t.UserId == userId && t.Title.Contains(searchTerm))
    .ToListAsync();

// ❌ INSEGURO - Concatenación directa
var sql = $"SELECT * FROM Tasks WHERE Title = '{searchTerm}'"; // SQL Injection!
```

**2. Input Validation con FluentValidation**
```csharp
public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .Must(NotContainHtmlTags).WithMessage("HTML tags not allowed")
            .Must(NotContainSqlKeywords).WithMessage("Invalid characters detected");
    }

    private bool NotContainHtmlTags(string text)
        => !Regex.IsMatch(text ?? "", @"<[^>]+>");

    private bool NotContainSqlKeywords(string text)
    {
        var sqlKeywords = new[] { "SELECT", "DROP", "INSERT", "UPDATE", "DELETE", "--", "/*", "*/" };
        return !sqlKeywords.Any(k => text?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
```

**3. Sanitización de Entrada**
```csharp
public static class InputSanitizer
{
    public static string SanitizeHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        // Encode caracteres HTML
        return System.Net.WebUtility.HtmlEncode(input);
    }

    public static string RemoveScriptTags(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        // Remover <script> tags
        return Regex.Replace(input, @"<script[^>]*>.*?</script>", "",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }
}
```

**4. Queries Seguras**
```csharp
// Si necesitas SQL raw (evitar siempre que sea posible):
var userId = new SqlParameter("@userId", currentUserId);
var tasks = await _context.Tasks
    .FromSqlRaw("SELECT * FROM Tasks WHERE UserId = @userId", userId)
    .ToListAsync();
```

---

### A04:2021 - Insecure Design

**Riesgo:** Diseño arquitectónico con fallas de seguridad fundamentales.

#### Mitigaciones Implementadas:

**1. Threat Modeling**
- Identificamos activos críticos: Credenciales, tokens, datos de usuario
- Análisis STRIDE por componente
- Documentación de superficie de ataque

**2. Secure Design Patterns**

**Fail Secure:**
```csharp
public async Task<Result<TaskDto>> Handle(UpdateTaskCommand request, CancellationToken ct)
{
    var task = await _context.Tasks.FindAsync(request.Id);

    // Default deny: Si algo falla, denegar
    if (task == null || task.UserId != _currentUser.UserId)
        return Result.Failure<TaskDto>("Access denied"); // No revelar si existe

    // Continuar solo si todo es válido
}
```

**Separation of Duties:**
```csharp
public enum UserRole
{
    User,   // Puede gestionar solo sus tareas
    Admin   // Puede gestionar todas las tareas + usuarios
}
```

**3. Rate Limiting por Diseño**
```csharp
// appsettings.json
{
  "RateLimiting": {
    "GeneralRules": {
      "PermitLimit": 100,
      "Window": "00:01:00" // 100 requests por minuto
    },
    "AuthenticationRules": {
      "PermitLimit": 10,
      "Window": "00:01:00" // 10 intentos de login por minuto
    }
  }
}

// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });
});
```

**4. Account Lockout**
```csharp
// User.cs - Domain entity
public class User : BaseEntity
{
    public int FailedLoginAttempts { get; private set; }
    public bool IsLockedOut { get; private set; }
    public DateTime? LockedOutUntil { get; private set; }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= 5)
        {
            IsLockedOut = true;
            LockedOutUntil = DateTime.UtcNow.AddMinutes(15);
        }
    }

    public bool CanLogin()
    {
        if (!IsLockedOut) return true;

        // Auto-unlock después de 15 minutos
        if (LockedOutUntil.HasValue && DateTime.UtcNow > LockedOutUntil.Value)
        {
            ResetLoginAttempts();
            return true;
        }

        return false;
    }
}
```

---

### A05:2021 - Security Misconfiguration

**Riesgo:** Configuraciones inseguras por defecto, headers faltantes, errores verbose.

#### Mitigaciones Implementadas:

**1. Security Headers**
```csharp
// SecurityHeadersMiddleware.cs
public class SecurityHeadersMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // HSTS: Forzar HTTPS por 1 año
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";

        // CSP: Content Security Policy
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none';";

        // Prevenir clickjacking
        headers["X-Frame-Options"] = "DENY";

        // Prevenir MIME sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Referrer policy
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // XSS Protection (legacy, pero útil para browsers viejos)
        headers["X-XSS-Protection"] = "1; mode=block";

        // Permissions policy (antes Feature-Policy)
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        await _next(context);
    }
}
```

**2. CORS Restrictivo**
```csharp
// Program.cs
var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins) // Whitelist específica
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials() // Para cookies HttpOnly
              .SetIsOriginAllowedToAllowWildcardSubdomains(); // *.example.com
    });
});

// appsettings.Production.json
{
  "CorsSettings": {
    "AllowedOrigins": [
      "https://app.taskmanagement.com",
      "https://admin.taskmanagement.com"
    ]
  }
}
```

**3. Error Handling Seguro**
```csharp
// ExceptionHandlingMiddleware.cs
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context);
    }
    catch (DomainException ex)
    {
        _logger.LogWarning(ex, "Domain exception: {Message}", ex.Message);
        await HandleExceptionAsync(context, ex.Message, StatusCodes.Status400BadRequest);
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "Unauthorized access attempt");
        // No revelar detalles
        await HandleExceptionAsync(context, "Access denied", StatusCodes.Status403Forbidden);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception");

        // En producción, no revelar stack trace
        var message = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
            ? ex.Message
            : "An error occurred processing your request";

        await HandleExceptionAsync(context, message, StatusCodes.Status500InternalServerError);
    }
}
```

**4. Configuración por Ambiente**
```json
// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning", // No Information/Debug en prod
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "DetailedErrors": false,
  "AllowedHosts": "taskmanagement.com,*.taskmanagement.com",
  "Swagger": {
    "Enabled": false // Swagger solo en dev
  }
}
```

---

### A06:2021 - Vulnerable and Outdated Components

**Riesgo:** Uso de librerías con vulnerabilidades conocidas.

#### Mitigaciones Implementadas:

**1. Dependency Scanning (GitHub Actions)**
```yaml
# .github/workflows/security-scan.yml
name: Security Scan

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]
  schedule:
    - cron: '0 0 * * 0' # Weekly

jobs:
  dependency-scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Run Snyk to check for vulnerabilities
        uses: snyk/actions/dotnet@master
        env:
          SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}
        with:
          args: --severity-threshold=high

      - name: Run Trivy vulnerability scanner
        uses: aquasecurity/trivy-action@master
        with:
          scan-type: 'fs'
          scan-ref: '.'
          format: 'sarif'
          output: 'trivy-results.sarif'

      - name: Upload results to GitHub Security
        uses: github/codeql-action/upload-sarif@v2
        with:
          sarif_file: 'trivy-results.sarif'
```

**2. NuGet Package Auditing**
```bash
# Comando para auditar vulnerabilidades
dotnet list package --vulnerable --include-transitive

# Actualizar paquetes con vulnerabilidades
dotnet add package PackageName --version X.Y.Z

# En CI/CD, fallar build si hay vulnerabilidades críticas
dotnet list package --vulnerable --include-transitive | grep "Critical" && exit 1
```

**3. Renovate Bot o Dependabot**
```yaml
# .github/dependabot.yml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 10
    reviewers:
      - "security-team"
    labels:
      - "dependencies"
      - "security"
```

**4. Pinning de Versiones**
```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <!-- Versiones explícitas, no usar wildcards -->
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
    <PackageVersion Include="FluentValidation" Version="11.5.0" />
  </ItemGroup>
</Project>
```

---

### A07:2021 - Identification and Authentication Failures

**Riesgo:** Fallas en autenticación permitiendo suplantación de identidad.

#### Mitigaciones Implementadas:

**1. JWT con Refresh Token Rotation**
```csharp
// LoginCommandHandler.cs
public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken ct)
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Value == request.Email, ct);

    if (user == null)
    {
        // Timing-safe: mismo tiempo si user existe o no (prevenir enumeración)
        _passwordHasher.Hash("dummy-password");
        return Result.Failure<AuthResponse>("Invalid credentials");
    }

    if (!user.CanLogin())
        return Result.Failure<AuthResponse>("Account is locked");

    if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
    {
        user.RecordFailedLogin();
        await _context.SaveChangesAsync(ct);

        _logger.LogWarning("Failed login attempt for user {Email}", request.Email);
        return Result.Failure<AuthResponse>("Invalid credentials");
    }

    // Login exitoso
    user.ResetLoginAttempts();
    await _context.SaveChangesAsync(ct);

    // Generar tokens
    var accessToken = _tokenService.GenerateAccessToken(user);
    var refreshToken = _tokenService.GenerateRefreshToken();

    // Guardar refresh token en DB
    var refreshTokenEntity = RefreshToken.Create(user.Id, refreshToken, DateTime.UtcNow.AddDays(7));
    _context.RefreshTokens.Add(refreshTokenEntity);
    await _context.SaveChangesAsync(ct);

    return Result.Success(new AuthResponse(accessToken, refreshToken, user.Email.Value));
}
```

**2. Refresh Token Rotation (Mitigar Token Theft)**
```csharp
// RefreshTokenCommandHandler.cs
public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
{
    var storedToken = await _context.RefreshTokens
        .Include(rt => rt.User)
        .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked, ct);

    if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
    {
        _logger.LogWarning("Invalid or expired refresh token used");
        return Result.Failure<AuthResponse>("Invalid refresh token");
    }

    // Detectar reuso de token (posible ataque)
    if (storedToken.IsUsed)
    {
        _logger.LogError("Refresh token reuse detected! Revoking all tokens for user {UserId}", storedToken.UserId);

        // Revocar TODOS los tokens de este usuario (token family)
        var userTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == storedToken.UserId)
            .ToListAsync(ct);

        foreach (var token in userTokens)
        {
            token.Revoke();
        }

        await _context.SaveChangesAsync(ct);

        return Result.Failure<AuthResponse>("Token reuse detected. Please login again.");
    }

    // Marcar token actual como usado
    storedToken.MarkAsUsed();

    // Generar NUEVO par de tokens (rotation)
    var newAccessToken = _tokenService.GenerateAccessToken(storedToken.User);
    var newRefreshToken = _tokenService.GenerateRefreshToken();

    var newRefreshTokenEntity = RefreshToken.Create(
        storedToken.UserId,
        newRefreshToken,
        DateTime.UtcNow.AddDays(7),
        storedToken.Id // Parent token para token family
    );

    _context.RefreshTokens.Add(newRefreshTokenEntity);
    await _context.SaveChangesAsync(ct);

    return Result.Success(new AuthResponse(newAccessToken, newRefreshToken, storedToken.User.Email.Value));
}
```

**3. Password Policy**
```csharp
public class PasswordValidator
{
    public static ValidationResult Validate(string password)
    {
        var errors = new List<string>();

        if (password.Length < 8)
            errors.Add("Password must be at least 8 characters");

        if (!password.Any(char.IsUpper))
            errors.Add("Password must contain at least one uppercase letter");

        if (!password.Any(char.IsLower))
            errors.Add("Password must contain at least one lowercase letter");

        if (!password.Any(char.IsDigit))
            errors.Add("Password must contain at least one number");

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            errors.Add("Password must contain at least one special character");

        // Prevenir passwords comunes
        var commonPasswords = new[] { "Password123!", "Admin123!", "Welcome123!" };
        if (commonPasswords.Contains(password))
            errors.Add("Password is too common");

        return errors.Any()
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }
}
```

**4. Multi-Factor Authentication (MFA) - Preparado para Futuro**
```csharp
public class User : BaseEntity
{
    public bool MfaEnabled { get; private set; }
    public string? MfaSecret { get; private set; } // TOTP secret
    public List<string> BackupCodes { get; private set; } = new();

    public void EnableMfa(string secret, List<string> backupCodes)
    {
        MfaEnabled = true;
        MfaSecret = secret;
        BackupCodes = backupCodes;
    }

    public bool ValidateMfaCode(string code)
    {
        if (!MfaEnabled) return true;

        // Validar TOTP code (Google Authenticator)
        // Implementación con library OtpNet
        var totp = new Totp(Base32Encoding.ToBytes(MfaSecret));
        return totp.VerifyTotp(code, out _, new VerificationWindow(2, 2));
    }
}
```

---

### A08:2021 - Software and Data Integrity Failures

**Riesgo:** Código o infraestructura sin verificación de integridad.

#### Mitigaciones Implementadas:

**1. Code Signing y Verification**
```yaml
# .github/workflows/ci-cd.yml
- name: Sign assemblies
  run: |
    dotnet build --configuration Release
    # Sign with code signing certificate
    signtool sign /f certificate.pfx /p ${{ secrets.CERT_PASSWORD }} /t http://timestamp.digicert.com bin/Release/TaskManagement.API.dll
```

**2. Docker Image Scanning**
```yaml
# .github/workflows/ci-cd.yml
- name: Build Docker image
  run: docker build -t taskmanagement-api:${{ github.sha }} .

- name: Scan Docker image for vulnerabilities
  uses: aquasecurity/trivy-action@master
  with:
    image-ref: taskmanagement-api:${{ github.sha }}
    format: 'table'
    exit-code: '1' # Fail build en vulnerabilidades críticas
    severity: 'CRITICAL,HIGH'

- name: Sign Docker image
  run: |
    cosign sign --key cosign.key taskmanagement-api:${{ github.sha }}
```

**3. Subresource Integrity (SRI) en Frontend**
```html
<!-- index.html -->
<link rel="stylesheet" href="https://cdn.example.com/styles.css"
      integrity="sha384-oqVuAfXRKap7fdgcCY5uykM6+R9GqQ8K/uxy9rx7HNQlGYl1kPzQho1wx4JwY8wC"
      crossorigin="anonymous">
```

**4. Dependency Lock Files**
```bash
# .NET: usa restore con locked mode
dotnet restore --locked-mode

# packages.lock.json asegura versiones exactas
```

---

### A09:2021 - Security Logging and Monitoring Failures

**Riesgo:** Falta de logging dificulta detección de ataques.

#### Mitigaciones Implementadas:

**1. Structured Logging con Serilog**
```csharp
// Program.cs
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "TaskManagement.API")
        .WriteTo.Console(new JsonFormatter())
        .WriteTo.File(
            path: "logs/taskmanagement-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        )
        .WriteTo.Seq("http://seq:5341"); // Centralizado
});
```

**2. Eventos de Seguridad Logueados**
```csharp
public class SecurityAuditLogger : ISecurityAuditLogger
{
    private readonly ILogger<SecurityAuditLogger> _logger;

    public void LogLoginAttempt(string email, bool success, string ipAddress)
    {
        _logger.LogWarning(
            "Login attempt for {Email} from {IpAddress}: {Result}",
            email, ipAddress, success ? "Success" : "Failed"
        );
    }

    public void LogAccountLockout(Guid userId, string email)
    {
        _logger.LogError(
            "Account locked out: UserId={UserId}, Email={Email}",
            userId, email
        );
    }

    public void LogUnauthorizedAccess(Guid userId, string resource)
    {
        _logger.LogWarning(
            "Unauthorized access attempt: UserId={UserId}, Resource={Resource}",
            userId, resource
        );
    }

    public void LogRefreshTokenReuse(Guid userId)
    {
        _logger.LogCritical(
            "SECURITY ALERT: Refresh token reuse detected for UserId={UserId}",
            userId
        );
    }
}
```

**3. Request Logging Middleware**
```csharp
public class RequestLoggingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        _logger.LogInformation(
            "HTTP {Method} {Path} from {IpAddress}",
            request.Method,
            request.Path,
            context.Connection.RemoteIpAddress
        );

        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        _logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
            request.Method,
            request.Path,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds
        );
    }
}
```

**4. Alerting en Eventos Críticos**
```csharp
// Integration con Slack/Email/PagerDuty
public class CriticalEventAlertService
{
    public async Task AlertSecurityTeam(string message, Dictionary<string, string> context)
    {
        _logger.LogCritical("SECURITY ALERT: {Message}, Context: {@Context}", message, context);

        // Enviar a Slack
        await _slackClient.PostMessageAsync(
            channel: "#security-alerts",
            text: $"🚨 SECURITY ALERT: {message}",
            attachments: context
        );

        // Enviar email
        await _emailService.SendAsync(
            to: "security@company.com",
            subject: "Security Alert - TaskManagement API",
            body: $"{message}\n\nContext: {JsonSerializer.Serialize(context)}"
        );
    }
}
```

---

### A10:2021 - Server-Side Request Forgery (SSRF)

**Riesgo:** Servidor hace requests a recursos internos no autorizados.

#### Mitigaciones Implementadas:

**1. Whitelist de URLs Permitidas**
```csharp
public class SafeHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SafeHttpClient> _logger;
    private readonly HashSet<string> _allowedHosts;

    public SafeHttpClient(IConfiguration configuration)
    {
        _allowedHosts = configuration.GetSection("AllowedExternalHosts").Get<HashSet<string>>();
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        var uri = new Uri(url);

        // Prevenir acceso a localhost, redes privadas
        if (IsPrivateOrLocalhost(uri.Host))
        {
            _logger.LogWarning("Blocked SSRF attempt to private network: {Url}", url);
            throw new SecurityException("Access to private networks is not allowed");
        }

        // Whitelist de hosts permitidos
        if (!_allowedHosts.Contains(uri.Host))
        {
            _logger.LogWarning("Blocked request to non-whitelisted host: {Host}", uri.Host);
            throw new SecurityException($"Host {uri.Host} is not allowed");
        }

        return await _httpClient.GetAsync(url);
    }

    private bool IsPrivateOrLocalhost(string host)
    {
        if (host == "localhost" || host == "127.0.0.1") return true;

        var ipAddress = Dns.GetHostAddresses(host).FirstOrDefault();
        if (ipAddress == null) return false;

        // Rangos privados: 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16
        var bytes = ipAddress.GetAddressBytes();
        return bytes[0] == 10 ||
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168);
    }
}
```

---

## Checklist de Seguridad

### Pre-Deployment
- [ ] Secrets no están hardcoded en código
- [ ] Todas las dependencias actualizadas y sin vulnerabilidades
- [ ] SAST (Static Analysis) ejecutado sin issues críticos
- [ ] Penetration testing realizado
- [ ] Security headers configurados
- [ ] CORS configurado restrictivamente
- [ ] Rate limiting habilitado
- [ ] Logging de eventos de seguridad activo
- [ ] HTTPS enforced
- [ ] Passwords hasheadas con BCrypt

### Post-Deployment
- [ ] Monitoreo activo en Seq/Grafana
- [ ] Alertas configuradas para eventos críticos
- [ ] Backups automáticos habilitados
- [ ] Plan de respuesta a incidentes documentado
- [ ] Security headers validados con securityheaders.com
- [ ] Vulnerability scanning semanal activo

---

## Recursos Adicionales

- [OWASP Top 10 2021](https://owasp.org/Top10/)
- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [CWE Top 25](https://cwe.mitre.org/top25/)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)

---

**Última actualización:** 2025-01-09
**Autor:** Senior DevSecOps Engineer
