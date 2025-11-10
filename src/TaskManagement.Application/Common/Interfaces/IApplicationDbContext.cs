using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Common.Interfaces;

/// <summary>
/// Define el contrato para el DbContext de la aplicación.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE DEPENDENCY INVERSION PRINCIPLE (DIP):
///
/// El DIP es uno de los 5 principios SOLID:
/// - Módulos de alto nivel NO deben depender de módulos de bajo nivel
/// - Ambos deben depender de abstracciones (interfaces)
/// - Las abstracciones NO deben depender de detalles
/// - Los detalles deben depender de abstracciones
///
/// PROBLEMA SIN INTERFAZ:
///
/// ❌ Application Layer depende directamente de Infrastructure:
///
/// // En Application Layer (Use Cases)
/// using TaskManagement.Infrastructure.Persistence;  // ← Dependencia directa
///
/// public class CreateTaskHandler
/// {
///     private readonly ApplicationDbContext _context;  // ← Clase concreta
///
///     public CreateTaskHandler(ApplicationDbContext context)
///     {
///         _context = context;
///     }
/// }
///
/// Problemas:
/// - Application depende de Infrastructure (viola Clean Architecture)
/// - No se puede testear fácilmente (necesita DB real)
/// - Acoplamiento fuerte entre capas
/// - Difícil cambiar implementación de persistencia
///
/// ✅ SOLUCIÓN CON INTERFAZ (DIP):
///
/// // En Application Layer
/// using TaskManagement.Application.Common.Interfaces;  // ← Abstracción
///
/// public class CreateTaskHandler
/// {
///     private readonly IApplicationDbContext _context;  // ← Interfaz
///
///     public CreateTaskHandler(IApplicationDbContext context)
///     {
///         _context = context;
///     }
/// }
///
/// // En Infrastructure Layer
/// public class ApplicationDbContext : DbContext, IApplicationDbContext
/// {
///     // Implementación concreta
/// }
///
/// Ventajas:
/// - Application solo depende de abstracción (interfaz)
/// - Infrastructure implementa la interfaz
/// - Fácil de testear con mocks
/// - Respeta flujo de dependencias: Infrastructure → Application
/// - Podemos cambiar implementación sin tocar Application
///
/// DIAGRAMA DE DEPENDENCIAS:
///
/// SIN INTERFAZ (❌ viola Clean Architecture):
/// ┌─────────────┐
/// │ Application │──────┐
/// └─────────────┘      │
///                      ▼ (depende directamente)
///              ┌──────────────────┐
///              │ Infrastructure   │
///              │ ApplicationDbContext │
///              └──────────────────┘
///
/// CON INTERFAZ (✅ respeta Clean Architecture):
/// ┌─────────────┐
/// │ Application │──────┐
/// │ IApplicationDbContext │ ◄─── Define interfaz
/// └─────────────┘      │
///                      │
///              ┌──────────────────┐
///              │ Infrastructure   │
///              │ ApplicationDbContext │ ◄─── Implementa interfaz
///              └──────────────────┘
///
/// ENTITY FRAMEWORK CORE INTEGRATION:
///
/// DbContext es la clase de EF Core que:
/// - Gestiona conexión a base de datos
/// - Trackea cambios en entidades
/// - Ejecuta queries (LINQ to SQL)
/// - Coordina transacciones
/// - Aplica migraciones
///
/// IApplicationDbContext expone:
/// - DbSet<T> para cada entidad (Tasks, Users, etc.)
/// - SaveChangesAsync() para persistir cambios
///
/// EF Core gestiona automáticamente:
/// - Tracking de cambios: _context.Tasks.Add(task) marca como Added
/// - SQL generation: Convierte LINQ a SQL
/// - Identity management: Asigna IDs a nuevas entidades
/// - Relationship fixup: Carga navegaciones automáticamente
///
/// UNIT OF WORK PATTERN:
///
/// DbContext implementa Unit of Work pattern:
/// - Agrupa múltiples operaciones en una transacción
/// - SaveChangesAsync() confirma todos los cambios o ninguno (atomicidad)
///
/// Ejemplo:
/// // Inicia transacción implícita
/// var user = await _context.Users.FindAsync(userId);
/// user.UpdateEmail(newEmail);
///
/// var task = await _context.Tasks.FindAsync(taskId);
/// task.UpdateTitle(newTitle);
///
/// await _context.SaveChangesAsync();  // Commit: ambas actualizaciones se guardan
/// // Si falla, ninguna se guarda (rollback automático)
///
/// EJEMPLO DE USO EN HANDLER:
///
/// public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Result<TaskDto>>
/// {
///     private readonly IApplicationDbContext _context;
///     private readonly ICurrentUserService _currentUser;
///
///     public CreateTaskCommandHandler(
///         IApplicationDbContext context,
///         ICurrentUserService currentUser)
///     {
///         _context = context;
///         _currentUser = currentUser;
///     }
///
///     public async Task<Result<TaskDto>> Handle(
///         CreateTaskCommand request,
///         CancellationToken cancellationToken)
///     {
///         // 1. Crear entidad de dominio
///         var task = TaskItem.Create(
///             request.Title,
///             request.Description,
///             _currentUser.UserId,
///             request.DueDate,
///             request.Priority
///         );
///
///         // 2. Agregar a DbSet (marca como Added en change tracker)
///         _context.Tasks.Add(task);
///
///         // 3. Guardar cambios (ejecuta INSERT SQL)
///         await _context.SaveChangesAsync(cancellationToken);
///
///         // 4. Mapear a DTO y retornar
///         var dto = _mapper.Map<TaskDto>(task);
///         return Result.Success(dto);
///     }
/// }
///
/// SQL GENERADO:
///
/// INSERT INTO Tasks (Id, Title, Description, UserId, Priority, Status, CreatedAt, UpdatedAt, IsDeleted)
/// VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8);
///
/// TESTING CON MOCKS:
///
/// Gracias a la interfaz, podemos mockear el DbContext en tests:
///
/// [Fact]
/// public async Task CreateTask_ValidData_ReturnsSuccess()
/// {
///     // Arrange
///     var mockContext = new Mock<IApplicationDbContext>();
///     var mockTasksDbSet = new Mock<DbSet<TaskItem>>();
///
///     mockContext.Setup(x => x.Tasks).Returns(mockTasksDbSet.Object);
///     mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
///                .ReturnsAsync(1);
///
///     var handler = new CreateTaskCommandHandler(mockContext.Object, ...);
///     var command = new CreateTaskCommand { Title = "Test Task" };
///
///     // Act
///     var result = await handler.Handle(command, CancellationToken.None);
///
///     // Assert
///     Assert.True(result.IsSuccess);
///     mockTasksDbSet.Verify(x => x.Add(It.IsAny<TaskItem>()), Times.Once);
///     mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
/// }
///
/// TESTING CON IN-MEMORY DATABASE (alternativa):
///
/// Otra opción es usar EF Core In-Memory provider para tests:
///
/// var options = new DbContextOptionsBuilder<ApplicationDbContext>()
///     .UseInMemoryDatabase(databaseName: "TestDb")
///     .Options;
///
/// var context = new ApplicationDbContext(options);
/// var handler = new CreateTaskCommandHandler(context, ...);
///
/// // Test con DB en memoria (más realista que mocks)
///
/// Ventajas de In-Memory:
/// - Más cercano a comportamiento real
/// - No necesita mockear DbSet<T>
/// - Tests más simples
///
/// Desventajas:
/// - Más lento que mocks
/// - No valida constraints SQL reales
/// - Comportamiento ligeramente diferente a SQL Server/PostgreSQL
///
/// TRANSACCIONES EXPLÍCITAS:
///
/// Para operaciones complejas, puedes usar transacciones explícitas:
///
/// using var transaction = await _context.Database.BeginTransactionAsync();
/// try
/// {
///     // Operación 1: Crear tarea
///     var task = TaskItem.Create(...);
///     _context.Tasks.Add(task);
///     await _context.SaveChangesAsync();
///
///     // Operación 2: Enviar notificación (ejemplo)
///     await _notificationService.SendAsync(...);
///
///     // Operación 3: Log de auditoría
///     var auditLog = AuditLog.Create(...);
///     _context.AuditLogs.Add(auditLog);
///     await _context.SaveChangesAsync();
///
///     await transaction.CommitAsync();
/// }
/// catch
/// {
///     await transaction.RollbackAsync();
///     throw;
/// }
///
/// Pero en la mayoría de casos, SaveChangesAsync() es suficiente.
///
/// CHANGE TRACKING:
///
/// EF Core trackea automáticamente cambios en entidades:
///
/// var task = await _context.Tasks.FindAsync(id);
/// // Estado: Unchanged
///
/// task.UpdateTitle("New Title");
/// // Estado: Modified (EF detecta el cambio automáticamente)
///
/// await _context.SaveChangesAsync();
/// // Genera: UPDATE Tasks SET Title = @p0, UpdatedAt = @p1 WHERE Id = @p2
///
/// IMPORTANTE: Solo propiedades modificadas se actualizan (eficiente).
///
/// DESACTIVAR TRACKING (para queries read-only):
///
/// var tasks = await _context.Tasks
///     .AsNoTracking()  // ← Más rápido, no trackea cambios
///     .Where(t => t.UserId == userId)
///     .ToListAsync();
///
/// Usar AsNoTracking() cuando:
/// - Solo lees datos (no modificas)
/// - Queries para DTOs
/// - Performance es crítica
///
/// SOFT DELETE GLOBAL FILTER:
///
/// Podemos configurar filtro global para soft deletes:
///
/// modelBuilder.Entity<TaskItem>()
///     .HasQueryFilter(t => !t.IsDeleted);
///
/// Ahora todos los queries automáticamente excluyen items eliminados:
///
/// var tasks = await _context.Tasks.ToListAsync();
/// // SQL: SELECT * FROM Tasks WHERE IsDeleted = 0
///
/// Para incluir eliminados explícitamente:
///
/// var allTasks = await _context.Tasks
///     .IgnoreQueryFilters()
///     .ToListAsync();
///
/// CONCURRENCY HANDLING:
///
/// EF Core detecta conflictos de concurrencia con RowVersion/Timestamp:
///
/// public class TaskItem
/// {
///     [Timestamp]
///     public byte[] RowVersion { get; set; }
/// }
///
/// try
/// {
///     await _context.SaveChangesAsync();
/// }
/// catch (DbUpdateConcurrencyException ex)
/// {
///     // Otro usuario modificó el mismo registro
///     return Result.Failure("Task was modified by another user");
/// }
///
/// Para este proyecto, no implementamos concurrency tokens (fuera de scope).
///
/// PERFORMANCE TIPS:
///
/// 1. Usar AsNoTracking() para queries read-only
/// 2. Proyectar a DTOs directamente en query (no cargar entidad completa)
/// 3. Evitar Select N+1: usar Include() para eager loading
/// 4. Usar AsSplitQuery() para includes múltiples
/// 5. Batch updates con ExecuteUpdateAsync() (EF Core 7+)
///
/// LAZY LOADING:
///
/// NO usar lazy loading en este proyecto (anti-pattern en APIs):
///
/// ❌ Lazy loading:
/// var task = await _context.Tasks.FindAsync(id);
/// var user = task.User;  // ← Query adicional! (N+1 problem)
///
/// ✅ Eager loading:
/// var task = await _context.Tasks
///     .Include(t => t.User)
///     .FirstOrDefaultAsync(t => t.Id == id);
/// var user = task.User;  // ← Sin query adicional
///
/// ✅ Explicit loading (cuando sea necesario):
/// var task = await _context.Tasks.FindAsync(id);
/// await _context.Entry(task).Reference(t => t.User).LoadAsync();
///
/// ✅ Proyección (mejor):
/// var taskDto = await _context.Tasks
///     .Where(t => t.Id == id)
///     .Select(t => new TaskDto
///     {
///         Id = t.Id,
///         Title = t.Title,
///         UserName = t.User.Email.Value  // ← JOIN automático
///     })
///     .FirstOrDefaultAsync();
/// </remarks>
public interface IApplicationDbContext
{
    /// <summary>
    /// DbSet de tareas.
    /// </summary>
    /// <remarks>
    /// DbSet<T> representa una colección de entidades de tipo T en la base de datos.
    /// Permite:
    /// - Queries: _context.Tasks.Where(t => t.UserId == id)
    /// - Inserts: _context.Tasks.Add(task)
    /// - Updates: automático con change tracking
    /// - Deletes: _context.Tasks.Remove(task)
    ///
    /// LINQ to SQL:
    /// EF Core traduce LINQ a SQL:
    ///
    /// var tasks = await _context.Tasks
    ///     .Where(t => t.UserId == userId)
    ///     .OrderByDescending(t => t.CreatedAt)
    ///     .Take(20)
    ///     .ToListAsync();
    ///
    /// SQL generado:
    /// SELECT TOP(20) * FROM Tasks
    /// WHERE UserId = @p0
    /// ORDER BY CreatedAt DESC
    /// </remarks>
    DbSet<TaskItem> Tasks { get; }

    /// <summary>
    /// DbSet de usuarios.
    /// </summary>
    /// <remarks>
    /// Permite gestionar usuarios en la base de datos.
    ///
    /// IMPORTANTE: No exponer este DbSet en APIs públicas.
    /// Solo para uso interno (autenticación, gestión de usuarios).
    ///
    /// Ejemplos:
    /// - Buscar usuario por email: _context.Users.FirstOrDefaultAsync(u => u.Email == email)
    /// - Crear usuario: _context.Users.Add(user)
    /// - Verificar existencia: _context.Users.AnyAsync(u => u.Email == email)
    /// </remarks>
    DbSet<User> Users { get; }

    /// <summary>
    /// DbSet de refresh tokens.
    /// </summary>
    /// <remarks>
    /// Gestiona tokens de refresco para JWT authentication.
    ///
    /// Operaciones comunes:
    /// - Crear token: _context.RefreshTokens.Add(token)
    /// - Buscar token: _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == tokenValue)
    /// - Revocar tokens: token.Revoke(); await SaveChangesAsync()
    /// - Limpiar expirados: _context.RefreshTokens.Where(t => t.ExpiresAt < DateTime.UtcNow).ExecuteDeleteAsync()
    ///
    /// SEGURIDAD:
    /// - Nunca retornar tokens en APIs
    /// - Tokens deben ser hasheados o encriptados en DB (para este proyecto, sin hash para simplicidad)
    /// - Limpiar tokens expirados periódicamente (background job)
    /// </remarks>
    DbSet<RefreshToken> RefreshTokens { get; }

    /// <summary>
    /// Guarda todos los cambios trackeados en la base de datos.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Número de entidades afectadas.</returns>
    /// <remarks>
    /// SaveChangesAsync() es el método principal para persistir cambios:
    ///
    /// 1. Detecta cambios en entidades trackeadas
    /// 2. Genera comandos SQL (INSERT/UPDATE/DELETE)
    /// 3. Ejecuta comandos en transacción
    /// 4. Actualiza propiedades (IDs generados, timestamps, etc.)
    /// 5. Marca entidades como Unchanged
    /// 6. Retorna cantidad de filas afectadas
    ///
    /// TRANSACCIONALIDAD:
    ///
    /// SaveChangesAsync() es atómico:
    /// - Si todas las operaciones tienen éxito → COMMIT
    /// - Si alguna falla → ROLLBACK (ningún cambio se aplica)
    ///
    /// _context.Tasks.Add(task1);
    /// _context.Tasks.Add(task2);
    /// _context.Users.Update(user);
    ///
    /// await _context.SaveChangesAsync();
    /// // O las 3 operaciones se guardan, o ninguna
    ///
    /// MANEJO DE ERRORES:
    ///
    /// try
    /// {
    ///     await _context.SaveChangesAsync(cancellationToken);
    /// }
    /// catch (DbUpdateException ex)
    /// {
    ///     // Errores de BD: constraint violations, duplicados, etc.
    ///     // Ejemplo: UNIQUE constraint on Email
    ///     return Result.Failure("Email already exists");
    /// }
    /// catch (DbUpdateConcurrencyException ex)
    /// {
    ///     // Conflicto de concurrencia (otro usuario modificó el registro)
    ///     return Result.Failure("Record was modified by another user");
    /// }
    ///
    /// VALIDACIÓN:
    ///
    /// EF Core valida:
    /// - Required fields
    /// - Max length
    /// - Data types
    /// - Constraints (UNIQUE, FK, CHECK)
    ///
    /// Pero la validación de negocio debe hacerse ANTES:
    ///
    /// ✅ Correcto:
    /// // 1. Validar con FluentValidation (en pipeline)
    /// var validationResult = await validator.ValidateAsync(command);
    ///
    /// // 2. Validar reglas de dominio
    /// if (await _context.Tasks.AnyAsync(t => t.Title == command.Title))
    ///     return Result.Failure("Task with this title already exists");
    ///
    /// // 3. Crear entidad (validación en constructor)
    /// var task = TaskItem.Create(...);
    ///
    /// // 4. Guardar (validación de BD)
    /// await _context.SaveChangesAsync();
    ///
    /// PERFORMANCE:
    ///
    /// SaveChangesAsync() puede ser costoso si hay muchas entidades trackeadas:
    /// - Detectar cambios en 1000 entidades puede tomar tiempo
    /// - Considerar batch updates con ExecuteUpdateAsync() para updates masivos
    ///
    /// // ❌ Lento para 1000 tareas
    /// var tasks = await _context.Tasks.Where(...).ToListAsync();
    /// foreach (var task in tasks)
    ///     task.UpdateStatus(TaskStatus.Completed);
    /// await _context.SaveChangesAsync();  // Genera 1000 UPDATE statements
    ///
    /// // ✅ Rápido (EF Core 7+)
    /// await _context.Tasks
    ///     .Where(...)
    ///     .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, TaskStatus.Completed));
    /// // Genera 1 UPDATE statement
    ///
    /// RETORNO:
    ///
    /// Retorna número de filas afectadas:
    /// - 0 = No hubo cambios
    /// - 1 = Una entidad modificada
    /// - N = N entidades modificadas
    ///
    /// var affectedRows = await _context.SaveChangesAsync();
    /// if (affectedRows == 0)
    ///     return Result.Failure("No changes were made");
    ///
    /// CANCELLATION TOKEN:
    ///
    /// Importante pasar cancellationToken para poder cancelar operaciones largas:
    ///
    /// public async Task<Result> Handle(Command request, CancellationToken ct)
    /// {
    ///     // Si el cliente cancela el request (cierra navegador),
    ///     // la operación de DB se cancela también
    ///     await _context.SaveChangesAsync(ct);
    /// }
    ///
    /// Esto previene:
    /// - Operaciones huérfanas en DB
    /// - Waste de recursos
    /// - Locks innecesarios
    /// </remarks>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
