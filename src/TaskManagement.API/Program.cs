using System.Reflection;
using FluentValidation;
using TaskManagement.API.Middleware;
using TaskManagement.Application;
using TaskManagement.Infrastructure;

/// <summary>
/// Entry point de la aplicación ASP.NET Core.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE Program.cs:
///
/// Program.cs es el entry point de la aplicación ASP.NET Core.
/// Configura servicios (Dependency Injection) y pipeline de middleware.
///
/// ESTRUCTURA:
///
/// 1. BUILDER (Configuración de servicios):
///    var builder = WebApplication.CreateBuilder(args);
///    builder.Services.Add...
///
/// 2. APP (Configuración de middleware pipeline):
///    var app = builder.Build();
///    app.Use...
///    app.Run();
///
/// WebApplicationBuilder:
/// - Carga configuración (appsettings.json, environment variables, user secrets)
/// - Configura logging
/// - Registra servicios en DI container
///
/// WebApplication:
/// - Configura pipeline de middleware
/// - Inicia servidor HTTP
///
/// CONFIGURACIÓN:
///
/// ASP.NET Core carga configuración de múltiples fuentes (en orden de prioridad):
///
/// 1. appsettings.json (base)
/// 2. appsettings.{Environment}.json (Development, Production, etc.)
/// 3. User Secrets (solo Development)
/// 4. Environment Variables (más prioritario)
/// 5. Command-line arguments (máxima prioridad)
///
/// Ejemplo:
/// appsettings.json: "ConnectionString": "localhost"
/// Environment Variable: ConnectionString="production-server"
/// → Usa "production-server" (environment variable sobrescribe)
///
/// USER SECRETS (Development):
///
/// Para datos sensibles en desarrollo (API keys, connection strings):
/// dotnet user-secrets init
/// dotnet user-secrets set "JwtSettings:SecretKey" "my-secret-key-12345678901234567890"
///
/// User secrets NO se commitean a Git (están fuera del proyecto).
///
/// En producción, usar Environment Variables o Azure Key Vault.
///
/// ENVIRONMENT:
///
/// ASP.NET Core tiene múltiples entornos:
/// - Development: Desarrollo local
/// - Staging: Pre-producción
/// - Production: Producción
///
/// Configurar con variable de entorno:
/// ASPNETCORE_ENVIRONMENT=Development
///
/// En código:
/// if (app.Environment.IsDevelopment()) { ... }
///
/// DEPENDENCY INJECTION CONTAINER:
///
/// builder.Services es el DI container donde se registran servicios:
///
/// builder.Services.AddScoped<IService, Service>();
///
/// ASP.NET Core resuelve dependencias automáticamente:
///
/// public class MyController : ControllerBase
/// {
///     public MyController(IService service)  // ← Inyectado automáticamente
/// }
///
/// EXTENSION METHODS:
///
/// Para organizar registro de servicios, usar extension methods:
///
/// builder.Services.AddApplication();        // Application Layer
/// builder.Services.AddInfrastructure(...); // Infrastructure Layer
///
/// Esto mantiene Program.cs limpio y organizado.
///
/// MIDDLEWARE PIPELINE:
///
/// Middleware procesa requests en orden:
///
/// Request  →  Middleware 1  →  Middleware 2  →  Controller
///             ↓                ↓                ↓
/// Response ←  Middleware 1  ←  Middleware 2  ←  Controller
///
/// ORDEN ES CRÍTICO:
///
/// 1. UseGlobalExceptionHandler()  - Captura excepciones
/// 2. UseHttpsRedirection()         - Redirect HTTP → HTTPS
/// 3. UseRouting()                  - Routing de requests
/// 4. UseCors()                     - CORS policy
/// 5. UseAuthentication()           - Valida JWT
/// 6. UseAuthorization()            - Valida permisos
/// 7. MapControllers()              - Endpoints
///
/// Si cambias orden, puede no funcionar correctamente.
///
/// SWAGGER/OPENAPI:
///
/// Swagger genera documentación interactiva de API:
///
/// builder.Services.AddSwaggerGen();  → Genera spec OpenAPI
/// app.UseSwagger();                  → Expone JSON spec
/// app.UseSwaggerUI();                → UI interactiva
///
/// Acceso: https://localhost:5001/swagger
///
/// SWAGGER CONFIGURATION:
///
/// Para mejor documentación:
///
/// builder.Services.AddSwaggerGen(c =>
/// {
///     c.SwaggerDoc("v1", new OpenApiInfo
///     {
///         Title = "Task Management API",
///         Version = "v1",
///         Description = "API para gestión de tareas"
///     });
///
///     // Incluir XML comments
///     var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
///     var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
///     c.IncludeXmlComments(xmlPath);
///
///     // JWT authentication en Swagger
///     c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
///     {
///         Description = "JWT Authorization header using Bearer scheme",
///         Name = "Authorization",
///         In = ParameterLocation.Header,
///         Type = SecuritySchemeType.ApiKey,
///         Scheme = "Bearer"
///     });
///
///     c.AddSecurityRequirement(new OpenApiSecurityRequirement
///     {
///         {
///             new OpenApiSecurityScheme
///             {
///                 Reference = new OpenApiReference
///                 {
///                     Type = ReferenceType.SecurityScheme,
///                     Id = "Bearer"
///                 }
///             },
///             Array.Empty<string>()
///         }
///     });
/// });
///
/// CORS (Cross-Origin Resource Sharing):
///
/// Si frontend está en dominio diferente, necesitas CORS:
///
/// builder.Services.AddCors(options =>
/// {
///     options.AddPolicy("AllowFrontend", policy =>
///     {
///         policy.WithOrigins("http://localhost:3000", "https://myapp.com")
///               .AllowAnyHeader()
///               .AllowAnyMethod()
///               .AllowCredentials();
///     });
/// });
///
/// app.UseCors("AllowFrontend");
///
/// Sin CORS, browser bloquea requests de frontend a API.
///
/// HTTPS:
///
/// En producción, SIEMPRE usar HTTPS:
/// - Protege datos en tránsito
/// - Previene man-in-the-middle
/// - Requerido para JWT (sin HTTPS, tokens pueden ser interceptados)
///
/// app.UseHttpsRedirection();  // Redirect HTTP → HTTPS
///
/// DEVELOPMENT CERTIFICATE:
///
/// Para desarrollo local con HTTPS:
/// dotnet dev-certs https --trust
///
/// Esto crea y confía en certificado de desarrollo.
///
/// HEALTH CHECKS:
///
/// Para monitoreo de aplicación:
///
/// builder.Services.AddHealthChecks()
///     .AddNpgSql(connectionString)
///     .AddRedis(redisConnection);
///
/// app.MapHealthChecks("/health");
///
/// GET /health → 200 OK si todo funciona, 503 si hay problemas
///
/// LOGGING:
///
/// ASP.NET Core logging está configurado por defecto:
///
/// builder.Logging.AddConsole();
/// builder.Logging.AddDebug();
///
/// En producción, usar proveedores avanzados:
/// - Serilog (popular, estructurado)
/// - Application Insights (Azure)
/// - ELK Stack (Elasticsearch, Logstash, Kibana)
///
/// STARTUP/SHUTDOWN:
///
/// app.Run() inicia el servidor y bloquea hasta shutdown:
///
/// Console.WriteLine("Starting server...");
/// app.Run();  // Bloquea aquí hasta Ctrl+C
/// Console.WriteLine("Server stopped");  // Solo después de shutdown
///
/// Para tareas de startup/shutdown:
///
/// app.Lifetime.ApplicationStarted.Register(() =>
/// {
///     Console.WriteLine("Application started");
/// });
///
/// app.Lifetime.ApplicationStopping.Register(() =>
/// {
///     Console.WriteLine("Application stopping...");
/// });
///
/// URLS:
///
/// Configurar URLs de escucha:
///
/// builder.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001");
///
/// O en appsettings.json:
/// "Kestrel": {
///   "Endpoints": {
///     "Http": { "Url": "http://localhost:5000" },
///     "Https": { "Url": "https://localhost:5001" }
///   }
/// }
///
/// KESTREL:
///
/// Kestrel es el web server de ASP.NET Core:
/// - Cross-platform (Windows, Linux, macOS)
/// - Alto rendimiento (>1M requests/sec)
/// - Lightweight
///
/// En producción, generalmente:
/// - Nginx/Apache como reverse proxy
/// - Kestrel como application server
///
/// MINIMAL APIS vs CONTROLLERS:
///
/// Este proyecto usa Controllers (MVC pattern):
/// app.MapControllers();
///
/// Alternativa: Minimal APIs (más simple, menos features):
/// app.MapGet("/api/tasks", async (IMediator mediator) =>
/// {
///     var result = await mediator.Send(new GetTasksQuery());
///     return Results.Ok(result);
/// });
///
/// Controllers son mejores para APIs complejas.
///
/// ENTITY FRAMEWORK MIGRATIONS:
///
/// Para aplicar migrations en startup (NO recomendado en producción):
///
/// using (var scope = app.Services.CreateScope())
/// {
///     var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
///     context.Database.Migrate();
/// }
///
/// En producción, mejor aplicar migrations manualmente.
///
/// GRACEFUL SHUTDOWN:
///
/// ASP.NET Core maneja shutdown gracefully:
/// 1. Recibe señal de shutdown (Ctrl+C, SIGTERM)
/// 2. Deja de aceptar nuevos requests
/// 3. Espera que requests en curso terminen (timeout: 30s)
/// 4. Dispone de servicios (DbContext, etc.)
/// 5. Termina proceso
///
/// MEJORES PRÁCTICAS:
///
/// 1. ✅ Usar HTTPS siempre
/// 2. ✅ Configurar CORS apropiadamente
/// 3. ✅ Orden correcto de middleware
/// 4. ✅ Secrets en User Secrets (dev) o Environment Variables (prod)
/// 5. ✅ Logging configurado
/// 6. ✅ Health checks para monitoreo
/// 7. ✅ Swagger solo en Development
/// 8. ❌ NO hardcodear secrets en código
/// 9. ❌ NO exponer stack trace en producción
/// </remarks>

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// CONFIGURACIÓN DE SERVICIOS (DEPENDENCY INJECTION)
// ============================================================

// Controllers
builder.Services.AddControllers();

// Application Layer (MediatR, AutoMapper, FluentValidation)
builder.Services.AddApplication();

// Infrastructure Layer (EF Core, JWT, Redis, Servicios)
builder.Services.AddInfrastructure(builder.Configuration);

// Swagger/OpenAPI (solo para desarrollo/documentación)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Task Management API",
        Version = "v1",
        Description = "API RESTful para gestión de tareas con Clean Architecture, CQRS, y JWT authentication",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Task Management Team",
            Email = "support@taskmanagement.com"
        }
    });

    // Incluir XML comments para documentación detallada
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Configurar JWT authentication en Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS (permitir requests desde frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Para producción, especificar orígenes específicos:
    // options.AddPolicy("AllowFrontend", policy =>
    // {
    //     policy.WithOrigins("https://myapp.com", "https://www.myapp.com")
    //           .AllowAnyMethod()
    //           .AllowAnyHeader()
    //           .AllowCredentials();
    // });
});

// ============================================================
// BUILD APPLICATION
// ============================================================

var app = builder.Build();

// ============================================================
// CONFIGURACIÓN DE MIDDLEWARE PIPELINE
// ============================================================

// 1. Exception Handler (PRIMERO - captura todas las excepciones)
app.UseGlobalExceptionHandler();

// 2. Swagger (solo en Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Management API v1");
        c.RoutePrefix = "swagger"; // Acceso: https://localhost:5001/swagger
    });
}

// 3. HTTPS Redirection (Redirect HTTP → HTTPS)
app.UseHttpsRedirection();

// 4. Routing (necesario antes de Authentication/Authorization)
app.UseRouting();

// 5. CORS (debe estar después de UseRouting y antes de UseAuthentication)
app.UseCors("AllowAll");

// 6. Authentication (valida JWT)
app.UseAuthentication();

// 7. Authorization (valida permisos)
app.UseAuthorization();

// 8. Map Controllers (endpoints)
app.MapControllers();

// ============================================================
// LOGGING DE STARTUP
// ============================================================

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application starting...");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

// ============================================================
// RUN APPLICATION
// ============================================================

app.Run();

logger.LogInformation("Application stopped");
