using System.Text.Json;
using StackExchange.Redis;
using TaskManagement.Application.Common.Interfaces;

namespace TaskManagement.Infrastructure.Services;

/// <summary>
/// Servicio para operaciones de caché usando Redis.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE REDIS:
///
/// Redis (Remote Dictionary Server) es una base de datos en memoria
/// de tipo clave-valor, ultra rápida, usada principalmente como caché.
///
/// ¿POR QUÉ USAR REDIS?
///
/// Sin caché:
/// - Cada request consulta base de datos
/// - Queries complejas son lentas (100-500ms)
/// - Alto load en base de datos
/// - Mal rendimiento bajo carga
///
/// Con Redis:
/// - Datos frecuentes en memoria (< 1ms)
/// - Reduce load en base de datos
/// - Mejor rendimiento y escalabilidad
/// - Puede manejar millones de requests/segundo
///
/// CASOS DE USO:
///
/// 1. Cache de queries:
///    - Listas de tareas del usuario
///    - Información de perfil
///    - Datos que cambian poco
///
/// 2. Session storage:
///    - Tokens de sesión
///    - Carritos de compra
///    - Datos temporales de usuario
///
/// 3. Rate limiting:
///    - Contar intentos de login
///    - Limitar requests por IP
///    - Throttling de API
///
/// 4. Pub/Sub:
///    - Notificaciones en tiempo real
///    - Chat
///    - Eventos entre microservicios
///
/// 5. Distributed locks:
///    - Sincronización en sistema distribuido
///    - Prevenir ejecución concurrente
///
/// ESTRUCTURA DE DATOS EN REDIS:
///
/// Redis soporta varios tipos de datos:
///
/// 1. STRING (más común):
///    SET user:123:profile '{"name":"John","email":"john@example.com"}'
///    GET user:123:profile
///
/// 2. HASH:
///    HSET user:123 name "John" email "john@example.com"
///    HGET user:123 email
///
/// 3. LIST:
///    LPUSH tasks:user:123 "task-id-1" "task-id-2"
///    LRANGE tasks:user:123 0 10
///
/// 4. SET:
///    SADD tags:task:123 "urgent" "bug"
///    SMEMBERS tags:task:123
///
/// 5. SORTED SET:
///    ZADD leaderboard 100 "player1" 95 "player2"
///    ZRANGE leaderboard 0 10 WITHSCORES
///
/// Para este proyecto usamos STRING (más simple y flexible).
///
/// SERIALIZACIÓN:
///
/// Redis almacena strings, necesitamos serializar objetos:
///
/// C# Object → JSON → Redis
/// Redis → JSON → C# Object
///
/// System.Text.Json es rápido y eficiente:
/// - Serialización: JsonSerializer.Serialize(obj)
/// - Deserialización: JsonSerializer.Deserialize<T>(json)
///
/// EXPIRACIÓN (TTL):
///
/// Redis puede expirar keys automáticamente:
///
/// SET key value EX 3600  → Expira en 1 hora (3600 segundos)
///
/// Importante para:
/// - Evitar memoria infinita
/// - Invalidar caché automáticamente
/// - Limpiar datos temporales
///
/// Sin expiración, Redis eventualmente se queda sin memoria.
///
/// ESTRATEGIAS DE CACHÉ:
///
/// 1. CACHE-ASIDE (Lazy Loading):
///    - Aplicación consulta caché primero
///    - Si miss, consulta BD y cachea resultado
///    - Usado en este proyecto
///
///    var tasks = await _cache.GetAsync<List<TaskDto>>(cacheKey);
///    if (tasks == null)
///    {
///        tasks = await _context.Tasks.ToListAsync();
///        await _cache.SetAsync(cacheKey, tasks, TimeSpan.FromMinutes(15));
///    }
///    return tasks;
///
/// 2. WRITE-THROUGH:
///    - Aplicación escribe en caché y BD simultáneamente
///    - Garantiza consistencia
///    - Overhead de escritura
///
/// 3. WRITE-BEHIND (Write-Back):
///    - Aplicación escribe en caché primero
///    - Caché escribe a BD asíncronamente
///    - Muy rápido pero riesgo de pérdida de datos
///
/// 4. REFRESH-AHEAD:
///    - Refresca caché antes de expirar
///    - Para datos con acceso predecible
///    - Más complejo
///
/// Para mayoría de casos, CACHE-ASIDE es suficiente.
///
/// INVALIDACIÓN DE CACHÉ:
///
/// "There are only two hard things in Computer Science:
///  cache invalidation and naming things." - Phil Karlton
///
/// Problema: ¿Cuándo invalidar caché después de actualización?
///
/// Estrategias:
///
/// 1. TIME-BASED (TTL):
///    - Expira después de X tiempo
///    - Simple pero puede servir datos obsoletos
///    - Usado en este proyecto (15 minutos)
///
/// 2. EVENT-BASED:
///    - Invalida cuando dato cambia
///    - Más consistente pero más complejo
///
///    public async Task UpdateTask(UpdateTaskCommand cmd)
///    {
///        await _context.SaveChangesAsync();
///        await _cache.RemoveAsync($"tasks:user:{cmd.UserId}");
///    }
///
/// 3. VERSIONING:
///    - Incluye versión en key
///    - Incrementa versión cuando dato cambia
///
///    tasks:user:123:v1 → tasks:user:123:v2
///
/// CONVENCIONES DE NAMING:
///
/// Buenas prácticas para keys de Redis:
///
/// - Usar prefijos jerárquicos: entity:id:attribute
/// - Consistente y predecible
/// - Fácil de buscar y eliminar en batch
///
/// Ejemplos:
/// - user:123:profile
/// - tasks:user:123:page:1
/// - session:abc123
/// - rate:login:ip:192.168.1.1
/// - cache:product:456
///
/// NO USAR:
/// - Keys muy largos (desperdicio de memoria)
/// - Espacios o caracteres especiales
/// - Keys inconsistentes
///
/// PERFORMANCE:
///
/// Redis es EXTREMADAMENTE rápido:
/// - Operaciones simples: < 1ms
/// - Throughput: 100,000+ ops/segundo en hardware básico
/// - Latencia: submilisegundo en misma red
///
/// Comparación:
/// - PostgreSQL query: 50-200ms
/// - Redis GET: < 1ms
/// - 50-200x más rápido
///
/// PERSISTENCIA:
///
/// Redis es en memoria pero puede persistir a disco:
///
/// 1. RDB (Snapshots):
///    - Copia completa de datos cada X tiempo
///    - Rápido y compacto
///    - Puede perder datos recientes en crash
///
/// 2. AOF (Append-Only File):
///    - Log de todas las operaciones
///    - Mayor durabilidad
///    - Más lento y archivos más grandes
///
/// 3. Híbrido (RDB + AOF):
///    - Mejor de ambos
///    - Recomendado para producción
///
/// Para caché, persistencia no es crítica (datos pueden regenerarse).
///
/// REDIS EN ENTORNO DISTRIBUIDO:
///
/// Para alta disponibilidad:
///
/// 1. REDIS SENTINEL:
///    - Monitoreo y failover automático
///    - Master-slave replication
///    - 3+ nodos
///
/// 2. REDIS CLUSTER:
///    - Sharding automático
///    - Escalamiento horizontal
///    - Miles de millones de keys
///
/// Para este proyecto, una instancia Redis es suficiente.
///
/// ALTERNATIVAS A REDIS:
///
/// 1. MEMCACHED:
///    - Más simple que Redis
///    - Solo clave-valor
///    - Más rápido para casos específicos
///    ❌ Menos features que Redis
///
/// 2. IN-MEMORY CACHE (MemoryCache):
///    - Incluido en .NET
///    - Caché local por servidor
///    ❌ No compartido entre servidores
///    ✅ OK para single-server
///
/// 3. DISTRIBUTED CACHE (IDistributedCache):
///    - Abstracción de .NET
///    - Puede usar Redis, SQL Server, etc.
///    ✅ Portable
///    ⚠️ Features limitados
///
/// Redis es generalmente la mejor opción para sistemas distribuidos.
///
/// CONEXIÓN A REDIS:
///
/// StackExchange.Redis usa connection multiplexing:
/// - Una conexión para múltiples operaciones
/// - Thread-safe
/// - Reutilizable
///
/// ConnectionMultiplexer debe ser SINGLETON:
/// - Costoso de crear
/// - Diseñado para reutilización
/// - Thread-safe
///
/// builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
///     ConnectionMultiplexer.Connect(configuration["Redis:ConnectionString"]!));
///
/// CONFIGURACIÓN:
///
/// appsettings.json:
/// {
///   "Redis": {
///     "ConnectionString": "localhost:6379,abortConnect=false",
///     "InstanceName": "TaskManagement:",
///     "DefaultExpirationMinutes": 15
///   }
/// }
///
/// Opciones de ConnectionString:
/// - localhost:6379 → Host y puerto
/// - password=... → Autenticación
/// - ssl=true → Conexión segura
/// - abortConnect=false → No fallar en startup si Redis no disponible
/// - connectTimeout=5000 → Timeout de conexión
///
/// MANEJO DE ERRORES:
///
/// ¿Qué hacer si Redis falla?
///
/// 1. Cache-Aside Pattern:
///    - Si GetAsync falla → consultar BD
///    - Si SetAsync falla → log warning, continuar
///    - Aplicación funciona sin caché
///
/// 2. Circuit Breaker:
///    - Después de X fallos, skip caché temporalmente
///    - Previene cascading failures
///
/// 3. Fallback to Local Cache:
///    - MemoryCache como backup
///    - Si Redis falla, usar caché local
///
/// Para este proyecto, usamos manejo simple (try-catch).
///
/// MONITOREO:
///
/// Redis provee comandos para monitoreo:
/// - INFO → Estadísticas generales
/// - MONITOR → Ver comandos en tiempo real
/// - SLOWLOG → Queries lentas
/// - MEMORY USAGE key → Memoria usada por key
///
/// TESTING:
///
/// Mockear ICacheService es fácil:
///
/// var mockCache = new Mock<ICacheService>();
/// mockCache.Setup(x => x.GetAsync<List<TaskDto>>(It.IsAny<string>()))
///     .ReturnsAsync((List<TaskDto>?)null); // Cache miss
///
/// mockCache.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>()))
///     .Returns(Task.CompletedTask);
///
/// Para testing de integración, usar Redis en Docker:
/// docker run -p 6379:6379 redis:alpine
///
/// MEJORES PRÁCTICAS:
///
/// 1. ✅ Siempre establecer expiración (evitar memory leak)
/// 2. ✅ Naming convention consistente
/// 3. ✅ Cachear datos que cambian poco
/// 4. ✅ NO cachear datos sensibles sin encriptar
/// 5. ✅ Monitorear uso de memoria
/// 6. ✅ Tener plan de invalidación
/// 7. ❌ NO cachear datos que cambian frecuentemente
/// 8. ❌ NO confiar en caché para datos críticos
/// 9. ❌ NO usar caché como base de datos primaria
/// </remarks>
public class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    public CacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _database = redis.GetDatabase();
    }

    /// <summary>
    /// Obtiene un valor del caché.
    /// </summary>
    /// <typeparam name="T">Tipo del objeto a deserializar.</typeparam>
    /// <param name="key">Clave del caché.</param>
    /// <returns>Objeto deserializado, o null si no existe o expiró.</returns>
    /// <remarks>
    /// Pasos:
    /// 1. GET key de Redis
    /// 2. Si existe, deserializa JSON a T
    /// 3. Si no existe o expiró, retorna null
    ///
    /// El llamador debe manejar cache miss:
    /// var data = await _cache.GetAsync<DataType>(key);
    /// if (data == null)
    /// {
    ///     data = await LoadFromDatabase();
    ///     await _cache.SetAsync(key, data, TimeSpan.FromMinutes(15));
    /// }
    /// </remarks>
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            // GET key de Redis
            var value = await _database.StringGetAsync(key);

            // Si no existe o está vacío, retorna null
            if (!value.HasValue)
                return null;

            // Deserializar JSON a objeto T
            return JsonSerializer.Deserialize<T>(value!);
        }
        catch
        {
            // Si Redis falla, retorna null (cache miss)
            // Aplicación debe funcionar sin caché
            return null;
        }
    }

    /// <summary>
    /// Establece un valor en el caché con expiración.
    /// </summary>
    /// <typeparam name="T">Tipo del objeto a serializar.</typeparam>
    /// <param name="key">Clave del caché.</param>
    /// <param name="value">Objeto a cachear.</param>
    /// <param name="expiration">Tiempo de expiración.</param>
    /// <remarks>
    /// IMPORTANTE: Siempre establecer expiración para evitar memory leak.
    ///
    /// Pasos:
    /// 1. Serializa objeto a JSON
    /// 2. SET key value EX expiration
    ///
    /// Expiración recomendada:
    /// - Datos que cambian poco: 30-60 minutos
    /// - Datos que cambian moderadamente: 5-15 minutos
    /// - Datos que cambian frecuentemente: 1-5 minutos
    /// - Datos temporales (sessions): horas o días
    ///
    /// Sin expiración, datos permanecen hasta:
    /// - Eliminar manualmente con RemoveAsync()
    /// - Redis se queda sin memoria (eviction)
    /// - Restart de Redis
    /// </remarks>
    public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
    {
        try
        {
            // Serializar objeto a JSON
            var json = JsonSerializer.Serialize(value);

            // SET key value con expiración
            await _database.StringSetAsync(key, json, expiration);
        }
        catch
        {
            // Si Redis falla, log warning pero continuar
            // Aplicación funciona sin caché
        }
    }

    /// <summary>
    /// Elimina una clave del caché.
    /// </summary>
    /// <param name="key">Clave a eliminar.</param>
    /// <remarks>
    /// Usado para invalidación manual de caché.
    ///
    /// Ejemplo:
    /// public async Task UpdateTask(UpdateTaskCommand cmd)
    /// {
    ///     // Actualizar en BD
    ///     await _context.SaveChangesAsync();
    ///
    ///     // Invalidar caché
    ///     await _cache.RemoveAsync($"tasks:user:{cmd.UserId}");
    ///     await _cache.RemoveAsync($"task:{cmd.TaskId}");
    /// }
    ///
    /// Para invalidar múltiples keys con patrón, usar SCAN + DEL:
    /// var keys = _redis.GetServer(endpoint).Keys(pattern: "tasks:user:*");
    /// foreach (var key in keys)
    ///     await _database.KeyDeleteAsync(key);
    /// </remarks>
    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch
        {
            // Si falla, log warning
        }
    }

    /// <summary>
    /// Verifica si una clave existe en el caché.
    /// </summary>
    /// <param name="key">Clave a verificar.</param>
    /// <returns>True si existe, False en caso contrario.</returns>
    /// <remarks>
    /// Útil para verificar existencia sin recuperar valor.
    ///
    /// Ejemplo:
    /// if (await _cache.ExistsAsync($"rate:login:{ip}"))
    /// {
    ///     return Result.Failure("Too many attempts");
    /// }
    /// </remarks>
    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch
        {
            return false;
        }
    }
}
