using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Services;

namespace TaskManagement.Infrastructure;

/// <summary>
/// Configuración de Dependency Injection para Infrastructure Layer.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE DEPENDENCY INJECTION:
///
/// Dependency Injection (DI) es un patrón que implementa Inversion of Control (IoC).
/// En lugar de que una clase cree sus dependencias, las recibe desde fuera.
///
/// SIN DI (dependencias hardcodeadas):
/// public class CreateTaskHandler
/// {
///     public async Task Handle()
///     {
///         var context = new ApplicationDbContext();  // ❌ Hardcoded
///         var tokenService = new TokenService();      // ❌ Hardcoded
///         // Difícil de testear, acoplado, inflexible
///     }
/// }
///
/// CON DI (dependencias inyectadas):
/// public class CreateTaskHandler
/// {
///     private readonly IApplicationDbContext _context;
///     private readonly ITokenService _tokenService;
///
///     public CreateTaskHandler(IApplicationDbContext context, ITokenService tokenService)
///     {
///         _context = context;           // ✅ Inyectado
///         _tokenService = tokenService; // ✅ Inyectado
///     }
/// }
///
/// VENTAJAS DE DI:
///
/// 1. TESTABILIDAD:
///    - Puedes inyectar mocks en tests
///    - No necesitas base de datos real para testing
///
/// 2. DESACOPLAMIENTO:
///    - Handler depende de IApplicationDbContext, no de implementación concreta
///    - Puedes cambiar implementación sin modificar Handler
///
/// 3. CONFIGURACIÓN CENTRALIZADA:
///    - Todos los servicios se registran en un solo lugar
///    - Fácil de mantener y entender
///
/// 4. LIFETIME MANAGEMENT:
///    - DI Container gestiona ciclo de vida automáticamente
///    - No memory leaks, disposal automático
///
/// SERVICE LIFETIMES:
///
/// ASP.NET Core tiene 3 lifetimes:
///
/// 1. TRANSIENT (AddTransient):
///    - Nueva instancia cada vez que se solicita
///    - Ideal para servicios livianos y stateless
///    - Ejemplo: EmailSender, PasswordHasher
///
///    Request 1: Handler → new PasswordHasher()
///    Request 1: Handler → new PasswordHasher()  (diferente instancia)
///
/// 2. SCOPED (AddScoped):
///    - Una instancia por request HTTP
///    - Compartida dentro del mismo request
///    - Ideal para DbContext, servicios con estado por request
///    - Ejemplo: ApplicationDbContext, CurrentUserService
///
///    Request 1: Handler1 → DbContext A
///    Request 1: Handler2 → DbContext A  (misma instancia)
///    Request 2: Handler1 → DbContext B  (nueva instancia)
///
/// 3. SINGLETON (AddSingleton):
///    - Una única instancia para toda la aplicación
///    - Compartida entre todos los requests
///    - Ideal para servicios costosos de crear o stateless
///    - Ejemplo: IConfiguration, ConnectionMultiplexer (Redis)
///    - ⚠️ DEBE SER THREAD-SAFE
///
///    App Start: new ConnectionMultiplexer()
///    Request 1: Handler → mismo ConnectionMultiplexer
///    Request 2: Handler → mismo ConnectionMultiplexer
///
/// REGLAS DE LIFETIME:
///
/// ❌ NO inyectar lifetime más corto en más largo:
/// - ❌ Scoped en Singleton → puede causar memory leak
/// - ❌ Transient en Singleton → puede causar memory leak
/// - ✅ Singleton en Scoped → OK
/// - ✅ Singleton en Transient → OK
/// - ✅ Scoped en Transient → OK
///
/// Ejemplo INCORRECTO:
/// builder.Services.AddSingleton<MyService>();  // Singleton
/// // MyService inyecta DbContext (Scoped)
/// // ❌ DbContext nunca se dispone, memory leak
///
/// EXTENSION METHODS:
///
/// Es buena práctica crear extension methods para cada layer:
///
/// - Infrastructure: AddInfrastructure()
/// - Application: AddApplication()
///
/// Esto mantiene Program.cs limpio y organizado:
///
/// builder.Services
///     .AddApplication()
///     .AddInfrastructure(builder.Configuration);
///
/// REGISTRO DE SERVICIOS:
///
/// Sintaxis:
/// services.AddScoped<IInterface, Implementation>();
///
/// Cuando alguien solicita IInterface, DI inyecta Implementation.
///
/// Ejemplo:
/// services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
///
/// Handler constructor:
/// public CreateTaskHandler(IApplicationDbContext context)
/// // DI inyecta ApplicationDbContext automáticamente
///
/// ENTITY FRAMEWORK CORE:
///
/// services.AddDbContext<T>() registra DbContext como SCOPED automáticamente.
///
/// Configuración:
/// services.AddDbContext<ApplicationDbContext>(options =>
///     options.UseNpgsql(connectionString));
///
/// UseNpgsql configura EF Core para usar PostgreSQL.
///
/// Opciones comunes:
/// - options.EnableSensitiveDataLogging() → Log SQL con parámetros (SOLO dev)
/// - options.EnableDetailedErrors() → Errores detallados (SOLO dev)
/// - options.UseQueryTrackingBehavior(NoTracking) → Queries read-only por defecto
///
/// JWT AUTHENTICATION:
///
/// services.AddAuthentication() configura middleware de autenticación.
/// services.AddJwtBearer() configura validación de JWT.
///
/// TokenValidationParameters especifica cómo validar tokens:
/// - ValidateIssuer: Verificar que token fue emitido por este servidor
/// - ValidateAudience: Verificar que token es para esta API
/// - ValidateLifetime: Verificar que token no expiró
/// - ValidateIssuerSigningKey: Verificar firma con clave secreta
/// - ClockSkew: Tolerancia de tiempo (default 5 min, recomendado 0)
///
/// REDIS CONNECTION:
///
/// ConnectionMultiplexer debe ser SINGLETON:
/// - Costoso de crear (~100ms)
/// - Diseñado para reutilización
/// - Gestiona connection pooling internamente
/// - Thread-safe
///
/// services.AddSingleton<IConnectionMultiplexer>(sp =>
///     ConnectionMultiplexer.Connect(connectionString));
///
/// CONFIGURACIÓN:
///
/// IConfiguration se inyecta automáticamente desde appsettings.json.
///
/// Acceso a configuración:
/// configuration["JwtSettings:SecretKey"]
/// configuration.GetConnectionString("DefaultConnection")
///
/// appsettings.json:
/// {
///   "ConnectionStrings": {
///     "DefaultConnection": "Host=localhost;Database=taskmanagement;..."
///   },
///   "JwtSettings": {
///     "SecretKey": "...",
///     "Issuer": "TaskManagementAPI"
///   }
/// }
///
/// ORDEN DE REGISTRO:
///
/// El orden generalmente NO importa, excepto:
/// 1. Servicios que dependen de otros deben registrarse después
/// 2. Múltiples implementaciones de misma interfaz (última gana)
///
/// MÚLTIPLES IMPLEMENTACIONES:
///
/// Si registras múltiples implementaciones:
/// services.AddScoped<IEmailSender, SmtpEmailSender>();
/// services.AddScoped<IEmailSender, SendGridEmailSender>();
///
/// Al resolver IEmailSender, obtienes la última (SendGridEmailSender).
///
/// Para obtener todas:
/// public MyService(IEnumerable<IEmailSender> senders)
///
/// NAMED OPTIONS PATTERN:
///
/// Para configuración tipada:
///
/// public class JwtSettings
/// {
///     public string SecretKey { get; set; } = string.Empty;
///     public string Issuer { get; set; } = string.Empty;
/// }
///
/// services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
///
/// Uso:
/// public TokenService(IOptions<JwtSettings> options)
/// {
///     var settings = options.Value;
/// }
///
/// VALIDACIÓN EN STARTUP:
///
/// Validar configuración crítica en startup:
///
/// var jwtSecret = configuration["JwtSettings:SecretKey"];
/// if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32)
///     throw new InvalidOperationException("JWT SecretKey must be at least 32 characters");
///
/// Mejor fallar rápido en startup que en runtime.
///
/// HEALTH CHECKS:
///
/// Registrar health checks para dependencias:
///
/// services.AddHealthChecks()
///     .AddNpgSql(connectionString)
///     .AddRedis(redisConnection);
///
/// Exponer endpoint: app.MapHealthChecks("/health");
///
/// TESTING:
///
/// En tests, puedes crear ServiceCollection manualmente:
///
/// var services = new ServiceCollection();
/// services.AddInfrastructure(configuration);
/// services.AddScoped<IApplicationDbContext, TestDbContext>();  // Override
/// var provider = services.BuildServiceProvider();
///
/// var handler = provider.GetRequiredService<CreateTaskCommandHandler>();
///
/// DECORATORS:
///
/// Para agregar comportamiento adicional:
///
/// services.AddScoped<IEmailSender, EmailSender>();
/// services.Decorate<IEmailSender, LoggingEmailSenderDecorator>();
///
/// LoggingEmailSenderDecorator envuelve EmailSender y agrega logging.
///
/// FACTORY PATTERN:
///
/// Para lógica de creación compleja:
///
/// services.AddScoped<IMyService>(sp =>
/// {
///     var config = sp.GetRequiredService<IConfiguration>();
///     if (config["Mode"] == "Production")
///         return new ProductionService();
///     return new DevelopmentService();
/// });
///
/// MEJORES PRÁCTICAS:
///
/// 1. ✅ Usar interfaces (IService) en lugar de clases concretas
/// 2. ✅ DbContext siempre Scoped
/// 3. ✅ ConnectionMultiplexer (Redis) siempre Singleton
/// 4. ✅ Servicios stateless pueden ser Singleton (mejor performance)
/// 5. ✅ Validar configuración crítica en startup
/// 6. ✅ Usar extension methods para organizar registro
/// 7. ❌ NO usar Singleton para servicios con estado
/// 8. ❌ NO inyectar Scoped en Singleton
/// 9. ❌ NO hardcodear conexiones (usar configuration)
/// </remarks>
public static class DependencyInjection
{
    /// <summary>
    /// Registra servicios de Infrastructure Layer en el contenedor de DI.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <returns>IServiceCollection para encadenar llamadas.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ============================================================
        // POSTGRESQL CON ENTITY FRAMEWORK CORE
        // ============================================================

        // Obtener connection string desde appsettings.json
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

        // Registrar DbContext (automáticamente Scoped)
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            // Configurar PostgreSQL como proveedor
            options.UseNpgsql(connectionString);

            // Habilitar logging sensible SOLO en desarrollo
            // ⚠️ NO usar en producción (expone datos sensibles en logs)
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Registrar interfaz IApplicationDbContext
        // Handlers dependen de IApplicationDbContext, no de ApplicationDbContext concreto
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // ============================================================
        // JWT AUTHENTICATION
        // ============================================================

        // Validar que SecretKey existe y es suficientemente larga
        var jwtSecret = configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey not configured");

        if (jwtSecret.Length < 32)
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters for HS256");

        // Configurar autenticación con JWT Bearer
        services.AddAuthentication(options =>
        {
            // Esquema por defecto: JWT Bearer
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // Configurar cómo validar tokens JWT
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // Validar que el token fue emitido por este servidor
                ValidateIssuer = true,
                ValidIssuer = configuration["JwtSettings:Issuer"],

                // Validar que el token es para esta API
                ValidateAudience = true,
                ValidAudience = configuration["JwtSettings:Audience"],

                // Validar que el token no haya expirado
                ValidateLifetime = true,

                // Validar la firma del token
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecret)),

                // Sin tolerancia de tiempo (default es 5 minutos)
                // Si token expira, es rechazado inmediatamente
                ClockSkew = TimeSpan.Zero
            };

            // Configurar eventos de autenticación (opcional, para logging/debugging)
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    // Log cuando autenticación falla
                    // Útil para debugging
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                },

                OnTokenValidated = context =>
                {
                    // Log cuando token es validado exitosamente
                    // Útil para auditoría
                    return Task.CompletedTask;
                }
            };
        });

        // ============================================================
        // REDIS
        // ============================================================

        // Obtener connection string de Redis
        var redisConnection = configuration["Redis:ConnectionString"]
            ?? "localhost:6379,abortConnect=false";

        // Registrar ConnectionMultiplexer como SINGLETON
        // ⚠️ ConnectionMultiplexer es costoso de crear y thread-safe
        // SIEMPRE usar Singleton
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnection));

        // ============================================================
        // SERVICIOS DE APLICACIÓN
        // ============================================================

        // ITokenService - Generación y validación de JWT
        // Transient porque es stateless y liviano
        services.AddTransient<ITokenService, TokenService>();

        // IPasswordHasher - Hash y verificación de contraseñas con BCrypt
        // Transient porque es stateless y liviano
        services.AddTransient<IPasswordHasher, PasswordHasher>();

        // ICacheService - Operaciones de caché con Redis
        // Scoped para coincidir con lifetime de request
        services.AddScoped<ICacheService, CacheService>();

        // ICurrentUserService - Acceso a usuario autenticado actual
        // Scoped porque depende de HttpContext (por request)
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // IHttpContextAccessor - Requerido por CurrentUserService
        // Scoped automáticamente
        services.AddHttpContextAccessor();

        return services;
    }
}
