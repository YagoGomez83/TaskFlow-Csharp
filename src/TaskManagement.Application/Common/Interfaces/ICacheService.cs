namespace TaskManagement.Application.Common.Interfaces;

/// <summary>
/// Define el contrato para operaciones de caché distribuida.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE CACHING:
///
/// El caching es una técnica para almacenar datos temporalmente en memoria rápida
/// para evitar consultas repetitivas a fuentes lentas (base de datos, APIs externas).
///
/// PROBLEMA SIN CACHÉ:
///
/// Usuario solicita lista de tareas (GET /api/tasks):
/// 1. Request → Server
/// 2. Server → Query a PostgreSQL
/// 3. PostgreSQL procesa query (~50ms)
/// 4. Server recibe datos
/// 5. Server → Response al cliente
/// Total: ~100ms
///
/// Si 1000 usuarios solicitan lo mismo:
/// - 1000 queries a PostgreSQL
/// - 1000 × 50ms = 50 segundos de procesamiento DB
/// - Alta carga en base de datos
/// - Latencia para usuarios
///
/// ✅ SOLUCIÓN CON CACHÉ:
///
/// Primera solicitud (cache miss):
/// 1. Request → Server
/// 2. Server chequea Redis → NO existe
/// 3. Server → Query a PostgreSQL (~50ms)
/// 4. Server almacena resultado en Redis
/// 5. Server → Response
/// Total: ~100ms
///
/// Siguientes solicitudes (cache hit):
/// 1. Request → Server
/// 2. Server chequea Redis → EXISTE (~1ms)
/// 3. Server → Response (sin tocar PostgreSQL)
/// Total: ~10ms (10x más rápido)
///
/// REDIS:
///
/// Redis es un data store in-memory extremadamente rápido.
/// Características:
/// - Almacena datos en RAM (no disco)
/// - Responde en microsegundos (~1ms)
/// - Tipos de datos: strings, lists, sets, hashes, sorted sets
/// - Soporta expiración automática (TTL)
/// - Persistencia opcional (snapshots, AOF)
/// - Puede usarse como caché, message broker, session store
///
/// Para este proyecto, usamos Redis como caché distribuida.
///
/// LOCAL CACHE vs DISTRIBUTED CACHE:
///
/// LOCAL CACHE (in-memory en servidor):
/// - Almacena en memoria del servidor web
/// - Muy rápido (nanosegundos)
/// - Problema: No compartido entre múltiples servidores
/// - Problema: Se pierde al reiniciar servidor
///
/// Ejemplo: 3 servidores web detrás de load balancer
/// Server 1: Cache con datos
/// Server 2: Cache vacío (diferentes instancias)
/// Server 3: Cache vacío
///
/// Usuario 1 → Server 1 → Cache hit ✅
/// Usuario 2 → Server 2 → Cache miss ❌ (debe consultar DB)
/// Usuario 3 → Server 3 → Cache miss ❌
///
/// DISTRIBUTED CACHE (Redis):
/// - Almacena en servidor Redis compartido
/// - Ligeramente más lento (~1ms) pero aún muy rápido
/// - Compartido entre todos los servidores web
/// - Persiste entre reinicios
///
/// Server 1 ┐
/// Server 2 ├── → Redis (cache compartido)
/// Server 3 ┘
///
/// Usuario 1 → Server 1 → Redis → Cache hit ✅
/// Usuario 2 → Server 2 → Redis → Cache hit ✅ (mismo cache)
/// Usuario 3 → Server 3 → Redis → Cache hit ✅
///
/// Para este proyecto usamos Redis (distributed cache) porque:
/// - Escalabilidad: Múltiples instancias de API
/// - Consistencia: Todos los servidores ven mismos datos
/// - Durabilidad: No se pierde al reiniciar
///
/// CACHE STRATEGIES:
///
/// 1. CACHE-ASIDE (Lazy Loading) - Este proyecto usa esto:
///
///    Read:
///    var data = await _cache.GetAsync<List<TaskDto>>(key);
///    if (data == null)  // Cache miss
///    {
///        data = await _database.GetTasksAsync();  // Query DB
///        await _cache.SetAsync(key, data, TimeSpan.FromMinutes(5));  // Store in cache
///    }
///    return data;
///
///    Write:
///    await _database.UpdateTaskAsync(task);
///    await _cache.RemoveAsync(key);  // Invalidate cache
///
///    Ventajas:
///    - Solo cachea datos solicitados (eficiente)
///    - Resiliente: Si Redis falla, app funciona (más lento)
///
///    Desventajas:
///    - Primer request es lento (cache miss)
///    - Lógica de caching en código de aplicación
///
/// 2. WRITE-THROUGH:
///
///    Write:
///    await _database.UpdateTaskAsync(task);
///    await _cache.SetAsync(key, task);  // Update cache también
///
///    Ventajas:
///    - Cache siempre actualizado
///    - No hay invalidación manual
///
///    Desventajas:
///    - Más lento en writes
///    - Cachea datos que quizás nunca se lean
///
/// 3. WRITE-BEHIND (Write-Back):
///
///    Write:
///    await _cache.SetAsync(key, task);  // Solo update cache
///    // Background job escribe a DB después
///
///    Ventajas:
///    - Writes muy rápidos
///    - Reduce carga en DB
///
///    Desventajas:
///    - Riesgo de pérdida de datos si Redis falla
///    - Complejo de implementar
///
/// Para este proyecto: CACHE-ASIDE (más simple y seguro)
///
/// TTL (Time To Live):
///
/// Los datos en caché deben expirar automáticamente para evitar datos obsoletos.
///
/// await _cache.SetAsync(key, data, TimeSpan.FromMinutes(5));
///
/// Después de 5 minutos, Redis automáticamente elimina la clave.
/// Próximo request será cache miss y consultará DB.
///
/// TTLs recomendados:
/// - Datos que cambian frecuentemente: 1-5 minutos
/// - Datos que cambian ocasionalmente: 15-60 minutos
/// - Datos casi estáticos: varias horas
/// - Datos estáticos: 1 día
///
/// Ejemplos para este proyecto:
/// - Lista de tareas del usuario: 5 minutos
/// - Detalle de una tarea: 10 minutos
/// - Configuración de usuario: 30 minutos
/// - Lista de roles/permisos: 1 hora
///
/// CACHE INVALIDATION:
///
/// "There are only two hard things in Computer Science: cache invalidation and naming things."
/// - Phil Karlton
///
/// Cuando datos cambian, debemos invalidar el caché:
///
/// public async Task<Result> UpdateTaskAsync(UpdateTaskCommand command)
/// {
///     var task = await _context.Tasks.FindAsync(command.TaskId);
///     task.UpdateTitle(command.Title);
///     await _context.SaveChangesAsync();
///
///     // Invalidar caché
///     await _cache.RemoveAsync($"task:{command.TaskId}");
///     await _cache.RemoveAsync($"tasks:user:{task.UserId}");  // Lista de tareas
///
///     return Result.Success();
/// }
///
/// Estrategias:
///
/// 1. DELETE (invalidation):
///    await _cache.RemoveAsync(key);
///    Próximo request refresca desde DB.
///
/// 2. UPDATE (refresh):
///    await _cache.SetAsync(key, updatedData);
///    Cache se actualiza inmediatamente.
///
/// 3. TAG-BASED:
///    Asociar múltiples keys a un tag.
///    await _cache.InvalidateByTagAsync("user:123");
///    Invalida todas las keys con ese tag.
///
/// Para este proyecto: Simple DELETE (más fácil).
///
/// CACHE KEYS:
///
/// Diseñar keys descriptivos y jerárquicos:
///
/// ✅ Buenos keys:
/// - "task:a1b2c3d4-e5f6-7890-abcd-ef1234567890"
/// - "tasks:user:a1b2c3d4:page:1:size:20"
/// - "user:a1b2c3d4:profile"
/// - "stats:user:a1b2c3d4:today"
///
/// ❌ Malos keys:
/// - "task1" (no descriptivo)
/// - "tasks" (demasiado genérico)
/// - "user_tasks_page_1" (inconsistente con otros keys)
///
/// Convenciones:
/// - Usar : como separador jerárquico
/// - Incluir tipo de entidad: task:, user:, etc.
/// - Incluir IDs relevantes
/// - Incluir parámetros si aplica (page, size)
///
/// SERIALIZATION:
///
/// Redis almacena strings, así que objetos deben serializarse:
///
/// // JSON serialization (este proyecto)
/// var json = JsonSerializer.Serialize(tasks);
/// await _redis.StringSetAsync(key, json);
///
/// var json = await _redis.StringGetAsync(key);
/// var tasks = JsonSerializer.Deserialize<List<TaskDto>>(json);
///
/// Alternativas:
/// - MessagePack: Más compacto y rápido que JSON
/// - Protobuf: Más compacto, requiere schema
/// - Binary formatter: NO usar (inseguro)
///
/// Para simplicidad, usamos JSON.
///
/// EJEMPLO DE USO - GET TASKS WITH CACHE:
///
/// public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, Result<List<TaskDto>>>
/// {
///     private readonly IApplicationDbContext _context;
///     private readonly ICacheService _cache;
///     private readonly ICurrentUserService _currentUser;
///
///     public async Task<Result<List<TaskDto>>> Handle(
///         GetTasksQuery request,
///         CancellationToken cancellationToken)
///     {
///         var userId = _currentUser.UserId;
///         var cacheKey = $"tasks:user:{userId}:page:{request.Page}:size:{request.PageSize}";
///
///         // 1. Intentar obtener de caché
///         var cachedTasks = await _cache.GetAsync<List<TaskDto>>(cacheKey);
///         if (cachedTasks != null)
///         {
///             _logger.LogInformation($"Cache hit: {cacheKey}");
///             return Result.Success(cachedTasks);
///         }
///
///         _logger.LogInformation($"Cache miss: {cacheKey}");
///
///         // 2. Query a base de datos
///         var tasks = await _context.Tasks
///             .Where(t => t.UserId == userId)
///             .OrderByDescending(t => t.CreatedAt)
///             .Skip((request.Page - 1) * request.PageSize)
///             .Take(request.PageSize)
///             .Select(t => new TaskDto { ... })
///             .ToListAsync(cancellationToken);
///
///         // 3. Almacenar en caché
///         await _cache.SetAsync(cacheKey, tasks, TimeSpan.FromMinutes(5));
///
///         return Result.Success(tasks);
///     }
/// }
///
/// EJEMPLO DE USO - INVALIDATE ON UPDATE:
///
/// public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, Result>
/// {
///     private readonly IApplicationDbContext _context;
///     private readonly ICacheService _cache;
///     private readonly ICurrentUserService _currentUser;
///
///     public async Task<Result> Handle(
///         UpdateTaskCommand request,
///         CancellationToken cancellationToken)
///     {
///         var task = await _context.Tasks.FindAsync(request.TaskId);
///
///         if (task == null)
///             return Result.Failure("Task not found");
///
///         // Update task
///         task.UpdateTitle(request.Title);
///         task.UpdateDescription(request.Description);
///         await _context.SaveChangesAsync(cancellationToken);
///
///         // Invalidate cache
///         await _cache.RemoveAsync($"task:{request.TaskId}");
///         await _cache.RemoveAsync($"tasks:user:{task.UserId}:*");  // Todas las páginas
///
///         return Result.Success();
///     }
/// }
///
/// CACHE STAMPEDE (Thundering Herd):
///
/// Problema: Caché expira, 1000 requests simultáneos consultan DB.
///
/// 1000 requests → Cache miss → 1000 queries a DB
///
/// Solución: SemaphoreSlim (lock) para que solo 1 request consulte DB:
///
/// private static readonly SemaphoreSlim _lock = new(1, 1);
///
/// public async Task<List<TaskDto>> GetTasksAsync()
/// {
///     var cached = await _cache.GetAsync<List<TaskDto>>(key);
///     if (cached != null) return cached;
///
///     await _lock.WaitAsync();
///     try
///     {
///         // Double-check (otra thread pudo haber cacheado mientras esperábamos)
///         cached = await _cache.GetAsync<List<TaskDto>>(key);
///         if (cached != null) return cached;
///
///         // Solo 1 thread llega aquí
///         var data = await _database.GetTasksAsync();
///         await _cache.SetAsync(key, data, TimeSpan.FromMinutes(5));
///         return data;
///     }
///     finally
///     {
///         _lock.Release();
///     }
/// }
///
/// MONITORING:
///
/// Monitorear cache hit rate:
///
/// Total requests: 1000
/// Cache hits: 850
/// Cache misses: 150
/// Hit rate: 85%
///
/// Hit rate saludable: 70-90%
/// - < 70%: TTL muy corto o datos muy dinámicos
/// - > 95%: TTL muy largo, riesgo de datos obsoletos
///
/// CUANDO NO USAR CACHÉ:
///
/// ❌ NO cachear:
/// - Datos que cambian constantemente (cada segundo)
/// - Datos de tiempo real (stock prices, sports scores)
/// - Datos sensibles o personales sin encriptar
/// - Datos muy grandes (> 1MB por key)
/// - Writes (solo cachear reads)
///
/// ✅ Cachear:
/// - Datos que se leen frecuentemente
/// - Datos que cambian ocasionalmente
/// - Queries costosos
/// - Datos compartidos entre usuarios
/// - Resultados de APIs externas
///
/// SEGURIDAD:
///
/// 1. NO cachear tokens o contraseñas
/// 2. Encriptar datos sensibles antes de cachear
/// 3. Usar TTL para limitar exposición
/// 4. Validar datos al leer de caché (pueden ser manipulados)
/// 5. Aislar caché por tenant si es multi-tenant
///
/// REDIS CONFIGURATION:
///
/// appsettings.json:
/// {
///   "Redis": {
///     "ConnectionString": "localhost:6379",
///     "InstanceName": "TaskManagement:",
///     "DefaultExpirationMinutes": 5
///   }
/// }
///
/// TESTING:
///
/// [Fact]
/// public async Task GetTasks_CacheHit_ReturnsCachedData()
/// {
///     // Arrange
///     var cachedTasks = new List<TaskDto> { ... };
///     _mockCache.Setup(x => x.GetAsync<List<TaskDto>>(It.IsAny<string>()))
///         .ReturnsAsync(cachedTasks);
///
///     // Act
///     var result = await _handler.Handle(query, CancellationToken.None);
///
///     // Assert
///     Assert.Equal(cachedTasks, result.Value);
///     _mockContext.Verify(x => x.Tasks, Times.Never);  // No consultó DB
/// }
///
/// [Fact]
/// public async Task GetTasks_CacheMiss_QueriesDbAndCaches()
/// {
///     // Arrange
///     _mockCache.Setup(x => x.GetAsync<List<TaskDto>>(It.IsAny<string>()))
///         .ReturnsAsync((List<TaskDto>)null);  // Cache miss
///
///     var tasks = new List<TaskDto> { ... };
///     // Setup mock DB to return tasks...
///
///     // Act
///     var result = await _handler.Handle(query, CancellationToken.None);
///
///     // Assert
///     Assert.Equal(tasks, result.Value);
///     _mockCache.Verify(x => x.SetAsync(
///         It.IsAny<string>(),
///         It.IsAny<List<TaskDto>>(),
///         It.IsAny<TimeSpan>()
///     ), Times.Once);  // Almacenó en caché
/// }
/// </remarks>
public interface ICacheService
{
    /// <summary>
    /// Obtiene un valor del caché por su clave.
    /// </summary>
    /// <typeparam name="T">Tipo del valor a obtener.</typeparam>
    /// <param name="key">Clave del valor en caché.</param>
    /// <returns>Valor si existe, null si no existe (cache miss).</returns>
    /// <remarks>
    /// Intenta obtener valor de Redis.
    /// Si existe, deserializa de JSON a tipo T.
    /// Si no existe, retorna null.
    ///
    /// Uso:
    /// var tasks = await _cache.GetAsync<List<TaskDto>>($"tasks:user:{userId}");
    /// if (tasks == null)
    /// {
    ///     // Cache miss: consultar DB
    ///     tasks = await _context.Tasks.ToListAsync();
    ///     await _cache.SetAsync($"tasks:user:{userId}", tasks, TimeSpan.FromMinutes(5));
    /// }
    /// return tasks;
    ///
    /// IMPORTANTE:
    /// - null significa cache miss (no existe o expiró)
    /// - NO lanza excepción si Redis está caído (resiliente)
    /// - Deserialización puede fallar si formato cambió
    ///
    /// Performance:
    /// - Cache hit: ~1ms
    /// - Cache miss: ~1ms (solo verificar existencia)
    /// </remarks>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Almacena un valor en el caché con expiración.
    /// </summary>
    /// <typeparam name="T">Tipo del valor a almacenar.</typeparam>
    /// <param name="key">Clave del valor en caché.</param>
    /// <param name="value">Valor a almacenar.</param>
    /// <param name="expiration">Tiempo de expiración (TTL).</param>
    /// <remarks>
    /// Serializa valor a JSON y almacena en Redis con TTL.
    ///
    /// Uso:
    /// var tasks = await _context.Tasks.ToListAsync();
    /// await _cache.SetAsync($"tasks:user:{userId}", tasks, TimeSpan.FromMinutes(5));
    ///
    /// Después de 5 minutos, Redis automáticamente elimina la clave.
    ///
    /// TTLs recomendados:
    /// - Datos muy dinámicos: 1-5 minutos
    /// - Datos medianamente dinámicos: 10-30 minutos
    /// - Datos casi estáticos: 1 hora
    /// - Datos estáticos: varias horas o 1 día
    ///
    /// IMPORTANTE:
    /// - NO lanza excepción si Redis está caído (resiliente)
    /// - Sobrescribe valor existente si key ya existe
    /// - value no debe ser null (usar RemoveAsync para eliminar)
    ///
    /// Tamaño:
    /// - Evitar cachear objetos > 1MB
    /// - Redis tiene límite de memoria (configurable)
    /// - Valores grandes impactan latencia de red
    /// </remarks>
    Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;

    /// <summary>
    /// Elimina un valor del caché.
    /// </summary>
    /// <param name="key">Clave del valor a eliminar.</param>
    /// <remarks>
    /// Elimina key de Redis (cache invalidation).
    ///
    /// Uso:
    /// await _context.SaveChangesAsync();  // Update DB
    /// await _cache.RemoveAsync($"task:{taskId}");  // Invalidate cache
    ///
    /// Llamar cuando:
    /// - Datos cambian (update, delete)
    /// - Necesitas forzar refresh
    /// - Limpieza de caché obsoleto
    ///
    /// IMPORTANTE:
    /// - Idempotente: No lanza error si key no existe
    /// - NO lanza excepción si Redis está caído
    /// - Operación atómica
    ///
    /// Pattern matching:
    /// Para eliminar múltiples keys con patrón:
    /// await _cache.RemoveByPatternAsync($"tasks:user:{userId}:*");
    ///
    /// Pero cuidado con patterns genéricos (pueden ser lentos).
    /// </remarks>
    Task RemoveAsync(string key);

    /// <summary>
    /// Verifica si una clave existe en el caché.
    /// </summary>
    /// <param name="key">Clave a verificar.</param>
    /// <returns>true si existe, false si no.</returns>
    /// <remarks>
    /// Verifica existencia de key en Redis sin obtener el valor.
    ///
    /// Uso:
    /// if (await _cache.ExistsAsync($"task:{taskId}"))
    /// {
    ///     // Key existe, podemos obtenerlo
    ///     var task = await _cache.GetAsync<TaskDto>($"task:{taskId}");
    /// }
    ///
    /// Generalmente NO necesitas esto porque:
    /// - GetAsync() retorna null si no existe
    /// - ExistsAsync() + GetAsync() = 2 round trips a Redis
    ///
    /// Solo útil en casos específicos:
    /// - Verificar antes de operación costosa
    /// - Lógica condicional basada en existencia
    ///
    /// Preferir:
    /// var task = await _cache.GetAsync<TaskDto>(key);
    /// if (task != null) { ... }
    ///
    /// En lugar de:
    /// if (await _cache.ExistsAsync(key))
    /// {
    ///     var task = await _cache.GetAsync<TaskDto>(key);
    /// }
    /// </remarks>
    Task<bool> ExistsAsync(string key);
}
