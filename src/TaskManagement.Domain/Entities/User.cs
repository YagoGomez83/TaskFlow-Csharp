using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;
using TaskManagement.Domain.ValueObjects;

namespace TaskManagement.Domain.Entities;

/// <summary>
/// Entidad de dominio que representa un usuario del sistema.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE LA ENTIDAD USER:
///
/// Esta entidad encapsula TODA la lógica relacionada con usuarios:
/// - Autenticación (login, lockout)
/// - Autorización (roles)
/// - Gestión de cuenta
/// - Reglas de negocio
///
/// PRINCIPIOS APLICADOS:
///
/// 1. ENCAPSULACIÓN:
///    - Setters privados (private set)
///    - Modificación solo vía métodos públicos
///    - Los métodos validan reglas de negocio
///
/// 2. AGGREGATE ROOT:
///    - User es un aggregate root
///    - Coordina operaciones sobre sus datos
///    - Garantiza invariantes (reglas que siempre deben cumplirse)
///
/// 3. RICH DOMAIN MODEL:
///    - La entidad tiene comportamiento, no solo datos
///    - Métodos como RecordFailedLogin(), ResetLoginAttempts()
///    - Lógica de negocio en el dominio, no en servicios
///
/// DIFERENCIA CON ANEMIC DOMAIN MODEL (anti-pattern):
///
/// ❌ Anemic Model (malo):
/// public class User
/// {
///     public Guid Id { get; set; }
///     public string Email { get; set; }
///     public int FailedLoginAttempts { get; set; }
///     // Solo getters/setters, sin comportamiento
/// }
///
/// ✅ Rich Model (bueno - este):
/// public class User
/// {
///     public int FailedLoginAttempts { get; private set; }
///
///     public void RecordFailedLogin()
///     {
///         FailedLoginAttempts++;
///         if (FailedLoginAttempts >= 5)
///             IsLockedOut = true;
///     }
///     // Comportamiento encapsulado con validaciones
/// }
///
/// ACCOUNT LOCKOUT (Bloqueo de Cuenta):
///
/// Implementamos lockout automático para prevenir brute-force attacks:
/// - Después de 5 intentos fallidos → cuenta bloqueada por 15 minutos
/// - LockedOutUntil guarda hasta cuándo está bloqueada
/// - CanLogin() verifica si puede loguearse (auto-desbloquea)
/// - ResetLoginAttempts() limpia contador al login exitoso
///
/// SEGURIDAD:
/// - Password NO se almacena (solo hash con BCrypt)
/// - Email como Value Object (validado)
/// - Lockout automático
/// - Auditoría de intentos de login
///
/// MEJORAS FUTURAS:
/// - MFA (Multi-Factor Authentication)
/// - Password history (no reutilizar últimas N passwords)
/// - Email verification
/// - Account activation
/// - Password reset via email
/// </remarks>
public class User : BaseEntity
{
    /// <summary>
    /// Email del usuario (identificador único).
    /// </summary>
    /// <remarks>
    /// EXPLICACIÓN:
    ///
    /// Usamos Value Object Email en lugar de string por:
    /// ✅ Validación automática del formato
    /// ✅ Normalización (lowercase, trim)
    /// ✅ Type safety
    /// ✅ Encapsulación de lógica de email
    ///
    /// El email es único en el sistema (enforced en DB con índice único).
    ///
    /// private set: Solo puede cambiarse dentro de la clase.
    /// Para cambiar el email, usar método UpdateEmail() que valida reglas de negocio.
    /// </remarks>
    public Email Email { get; private set; }

    /// <summary>
    /// Hash de la contraseña del usuario (BCrypt).
    /// </summary>
    /// <remarks>
    /// SEGURIDAD CRÍTICA:
    ///
    /// NUNCA almacenamos la contraseña en texto plano.
    /// Almacenamos solo el hash generado por BCrypt.
    ///
    /// BCrypt:
    /// - Algoritmo de hashing diseñado para passwords
    /// - Incluye salt automático (previene rainbow tables)
    /// - Cost factor ajustable (work factor)
    /// - Resistente a brute force por diseño
    ///
    /// Formato del hash BCrypt:
    /// $2a$12$KIXxLVkaOxZNzYFXQ7BJ2.iJV8z6YJTbXW9LxVZv1JQMxYv5e6hGi
    ///  ^  ^  ^                                           ^
    ///  |  |  |                                           |
    ///  |  |  salt (22 chars)                            hash (31 chars)
    ///  |  cost factor (12 = 2^12 = 4096 iteraciones)
    ///  algoritmo (2a = BCrypt)
    ///
    /// Cost Factor 12:
    /// - 2^12 = 4,096 iteraciones
    /// - Toma ~0.3 segundos hashear
    /// - Balance entre seguridad y UX
    /// - Aumentar si el hardware mejora
    ///
    /// NUNCA:
    /// ❌ Almacenar contraseña en texto plano
    /// ❌ Usar MD5 o SHA1 (inseguros para passwords)
    /// ❌ Usar hashing sin salt
    /// ❌ Loguear el hash (información sensible)
    ///
    /// VERIFICACIÓN:
    /// bool isValid = BCrypt.Net.BCrypt.Verify(plainPassword, PasswordHash);
    /// // Timing-safe comparison, previene timing attacks
    /// </remarks>
    public string PasswordHash { get; private set; }

    /// <summary>
    /// Rol del usuario (User o Admin).
    /// </summary>
    /// <remarks>
    /// RBAC (Role-Based Access Control):
    ///
    /// El rol determina qué puede hacer el usuario en el sistema.
    /// Ver UserRole enum para detalles de cada rol.
    ///
    /// Rol por defecto: User (asignado en factory method Create)
    ///
    /// Cambio de rol:
    /// - Solo un Admin puede cambiar roles
    /// - Se debe validar en Application layer
    /// - Loguear en auditoría todos los cambios de rol
    ///
    /// Implementación en Authorization:
    /// - JWT claim: "role" = "Admin" o "User"
    /// - Atributo en controllers: [Authorize(Roles = "Admin")]
    /// - Verificación en handlers: if (_currentUser.Role != UserRole.Admin)
    /// </remarks>
    public UserRole Role { get; private set; }

    /// <summary>
    /// Contador de intentos fallidos de login consecutivos.
    /// </summary>
    /// <remarks>
    /// PREVENCIÓN DE BRUTE FORCE:
    ///
    /// Cada vez que falla un login, incrementamos este contador.
    /// Al llegar a 5, bloqueamos la cuenta automáticamente.
    ///
    /// Flujo:
    /// 1. Login fallido → FailedLoginAttempts++
    /// 2. Si FailedLoginAttempts >= 5 → IsLockedOut = true
    /// 3. Login exitoso → FailedLoginAttempts = 0
    ///
    /// Por qué 5 intentos:
    /// - Suficiente para errores honestos (typos)
    /// - Insuficiente para brute force efectivo
    /// - Estándar de la industria
    ///
    /// Alternativas consideradas:
    /// - CAPTCHA después de 3 intentos (mejor UX)
    /// - Delay incremental (1s, 2s, 4s, 8s, 16s)
    /// - Rate limiting por IP
    ///
    /// Reseteo:
    /// - Al login exitoso: ResetLoginAttempts()
    /// - Al desbloqueo automático: CanLogin() + login exitoso
    /// - Admin puede resetear manualmente
    /// </remarks>
    public int FailedLoginAttempts { get; private set; }

    /// <summary>
    /// Indica si la cuenta está bloqueada por intentos fallidos.
    /// </summary>
    /// <remarks>
    /// Flag de bloqueo temporal por seguridad.
    ///
    /// Estados:
    /// - false: Cuenta activa, puede intentar login
    /// - true: Cuenta bloqueada, no puede intentar login
    ///
    /// Bloqueo:
    /// - Automático tras 5 intentos fallidos
    /// - Dura 15 minutos (ver LockedOutUntil)
    ///
    /// Desbloqueo:
    /// - Automático tras tiempo expirado (CanLogin())
    /// - Manual por Admin (future feature)
    ///
    /// Diferencia con Soft Delete (IsDeleted):
    /// - IsLockedOut: Temporal, reversible, por seguridad
    /// - IsDeleted: "Permanente", usuario eliminó su cuenta
    /// </remarks>
    public bool IsLockedOut { get; private set; }

    /// <summary>
    /// Timestamp hasta cuándo está bloqueada la cuenta (UTC).
    /// </summary>
    /// <remarks>
    /// Nullable porque:
    /// - null = no bloqueada O bloqueo ya expiró
    /// - DateTime = bloqueada hasta esa fecha/hora
    ///
    /// Duración del bloqueo: 15 minutos
    ///
    /// Ejemplo:
    /// - 5to intento fallido a las 10:00 AM
    /// - LockedOutUntil = 10:15 AM
    /// - A las 10:15 AM, CanLogin() retorna true y auto-desbloquea
    ///
    /// Por qué 15 minutos:
    /// - Suficiente para frustrar ataques automatizados
    /// - No demasiado largo para usuarios legítimos
    /// - Balance entre seguridad y UX
    ///
    /// Alternativas:
    /// - Bloqueo permanente hasta intervención de Admin (más seguro, peor UX)
    /// - Bloqueo incremental: 5min, 15min, 1h, 24h (mejor balance)
    /// - Sin bloqueo, solo CAPTCHA (mejor UX, menos seguro)
    /// </remarks>
    public DateTime? LockedOutUntil { get; private set; }

    /// <summary>
    /// Constructor privado para EF Core.
    /// </summary>
    /// <remarks>
    /// EF Core necesita un constructor parameterless para crear instancias al
    /// leer de la base de datos.
    ///
    /// private: No se puede usar fuera de la clase.
    /// Para crear usuarios nuevos, usar el factory method Create().
    ///
    /// Esto es necesario porque:
    /// - EF Core usa reflection para instanciar
    /// - No queremos permitir new User() en código de negocio
    /// - Forzamos uso de Create() que valida
    /// </remarks>
    private User()
    {
        // Constructor vacío para EF Core
        // Las propiedades se setean via reflection al hidratar desde DB
    }

    /// <summary>
    /// Factory method para crear un nuevo usuario.
    /// </summary>
    /// <param name="email">Email del usuario (será validado).</param>
    /// <param name="passwordHash">Hash BCrypt de la contraseña.</param>
    /// <param name="role">Rol del usuario (default: User).</param>
    /// <returns>Nueva instancia de User válida.</returns>
    /// <remarks>
    /// PATRÓN FACTORY METHOD:
    ///
    /// En lugar de: new User(email, password)
    /// Usamos: User.Create(email, password)
    ///
    /// Ventajas:
    /// ✅ Valida que los datos son correctos
    /// ✅ Inicializa campos con valores por defecto seguros
    /// ✅ Nombre más expresivo
    /// ✅ Puede tener múltiples factory methods (CreateAdmin, CreateFromExternalAuth, etc.)
    ///
    /// Validaciones:
    /// - Email es validado automáticamente (es un Value Object)
    /// - PasswordHash debe estar ya hasheado (responsabilidad del caller)
    ///
    /// Valores iniciales seguros:
    /// - FailedLoginAttempts = 0 (sin intentos fallidos)
    /// - IsLockedOut = false (cuenta activa)
    /// - Role = User (rol menos privilegiado por defecto)
    ///
    /// IMPORTANTE: passwordHash debe venir YA HASHEADO con BCrypt.
    /// Este método NO hashea la contraseña (eso se hace en Application layer).
    ///
    /// Ejemplo de uso:
    /// var passwordHash = _passwordHasher.Hash(plainPassword);
    /// var user = User.Create(Email.Create("test@example.com"), passwordHash);
    /// _context.Users.Add(user);
    /// await _context.SaveChangesAsync();
    /// </remarks>
    public static User Create(Email email, string passwordHash, UserRole role = UserRole.User)
    {
        // Validación: passwordHash no puede estar vacío
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash cannot be empty");
        }

        // Crear usuario con valores seguros por defecto
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            FailedLoginAttempts = 0,
            IsLockedOut = false,
            LockedOutUntil = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Registra un intento de login fallido y bloquea la cuenta si es necesario.
    /// </summary>
    /// <remarks>
    /// LÓGICA DE BLOQUEO:
    ///
    /// 1. Incrementa contador de intentos fallidos
    /// 2. Si llega a 5 o más, bloquea la cuenta por 15 minutos
    ///
    /// Llamar este método desde el LoginCommandHandler cuando la password no coincide.
    ///
    /// Ejemplo de uso:
    /// var user = await _userRepository.GetByEmailAsync(request.Email);
    /// if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
    /// {
    ///     user.RecordFailedLogin();
    ///     await _context.SaveChangesAsync();
    ///     return Result.Failure("Invalid credentials");
    /// }
    ///
    /// SEGURIDAD - TIMING ATTACKS:
    /// Importante hacer el SaveChangesAsync() SIEMPRE, no solo en el caso de fallo,
    /// para evitar revelar si el usuario existe o no basándose en el tiempo de respuesta.
    ///
    /// Mejor práctica:
    /// // Timing-safe: Siempre toma el mismo tiempo
    /// if (user == null || !_passwordHasher.Verify(password, user.PasswordHash))
    /// {
    ///     if (user != null)
    ///         user.RecordFailedLogin();
    ///
    ///     await Task.Delay(100); // Delay constante
    ///     return Failure("Invalid credentials"); // Mensaje genérico
    /// }
    /// </remarks>
    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        UpdatedAt = DateTime.UtcNow;

        // Bloquear cuenta si alcanza el límite
        const int maxAttempts = 5;
        if (FailedLoginAttempts >= maxAttempts)
        {
            IsLockedOut = true;
            LockedOutUntil = DateTime.UtcNow.AddMinutes(15);
        }
    }

    /// <summary>
    /// Resetea el contador de intentos fallidos y desbloquea la cuenta.
    /// </summary>
    /// <remarks>
    /// Llamar este método:
    /// 1. Tras login exitoso
    /// 2. Tras desbloqueo automático (en CanLogin())
    /// 3. Tras desbloqueo manual por Admin (future)
    ///
    /// Ejemplo de uso:
    /// var user = await _userRepository.GetByEmailAsync(request.Email);
    /// if (_passwordHasher.Verify(request.Password, user.PasswordHash))
    /// {
    ///     user.ResetLoginAttempts(); // Login exitoso, resetear
    ///     await _context.SaveChangesAsync();
    ///     return Success(generatedToken);
    /// }
    /// </remarks>
    public void ResetLoginAttempts()
    {
        FailedLoginAttempts = 0;
        IsLockedOut = false;
        LockedOutUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Verifica si el usuario puede intentar login (no bloqueado o bloqueo expirado).
    /// </summary>
    /// <returns>true si puede login, false si está bloqueado.</returns>
    /// <remarks>
    /// LÓGICA DE AUTO-DESBLOQUEO:
    ///
    /// 1. Si no está bloqueado → return true
    /// 2. Si está bloqueado pero el tiempo expiró → auto-desbloquear y return true
    /// 3. Si está bloqueado y el tiempo NO expiró → return false
    ///
    /// Ejemplo de uso:
    /// var user = await _userRepository.GetByEmailAsync(request.Email);
    /// if (!user.CanLogin())
    /// {
    ///     return Result.Failure("Account is locked. Try again later.");
    /// }
    ///
    /// IMPORTANTE: Este método MODIFICA el estado (auto-desbloquea).
    /// Llamar SaveChangesAsync() después para persistir el desbloqueo.
    ///
    /// if (user.CanLogin())
    /// {
    ///     // Proceder con login...
    ///     await _context.SaveChangesAsync(); // Guardar posible desbloqueo
    /// }
    ///
    /// AUTO-DESBLOQUEO:
    /// El desbloqueo es automático sin intervención de Admin.
    /// Esto mejora UX para usuarios legítimos que olvidaron su password.
    /// Para cuentas sospechosas, considerar bloqueo permanente (future feature).
    /// </remarks>
    public bool CanLogin()
    {
        // Si no está bloqueada, puede login
        if (!IsLockedOut)
            return true;

        // Si está bloqueada pero el tiempo expiró, auto-desbloquear
        if (LockedOutUntil.HasValue && DateTime.UtcNow > LockedOutUntil.Value)
        {
            ResetLoginAttempts(); // Auto-desbloqueo
            return true;
        }

        // Está bloqueada y el tiempo no expiró
        return false;
    }

    /// <summary>
    /// Actualiza el email del usuario.
    /// </summary>
    /// <param name="newEmail">Nuevo email (será validado).</param>
    /// <remarks>
    /// REGLAS DE NEGOCIO:
    ///
    /// 1. No se puede cambiar email si la cuenta está bloqueada
    /// 2. El nuevo email será validado automáticamente (es Value Object)
    /// 3. Se debe verificar unicidad en Application layer (no aquí)
    ///
    /// Ejemplo de uso en Handler:
    /// var user = await _userRepository.GetByIdAsync(userId);
    ///
    /// // Verificar que el nuevo email no existe
    /// if (await _userRepository.EmailExistsAsync(newEmail))
    ///     return Result.Failure("Email already in use");
    ///
    /// user.UpdateEmail(newEmail);
    /// await _context.SaveChangesAsync();
    ///
    /// MEJORA FUTURA: Email Verification
    /// - Enviar email con código de verificación
    /// - No cambiar hasta que se verifique
    /// - EmailPendingVerification property
    /// </remarks>
    public void UpdateEmail(Email newEmail)
    {
        if (IsLockedOut)
        {
            throw new DomainException("Cannot update email of locked out account");
        }

        Email = newEmail;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza el hash de la contraseña del usuario.
    /// </summary>
    /// <param name="newPasswordHash">Nuevo hash de contraseña (BCrypt).</param>
    /// <remarks>
    /// REGLAS DE NEGOCIO:
    ///
    /// 1. No se puede cambiar password si la cuenta está bloqueada
    /// 2. El hash debe estar ya hasheado (responsabilidad del caller)
    ///
    /// Ejemplo de uso en ChangePasswordHandler:
    /// var user = await _userRepository.GetByIdAsync(userId);
    ///
    /// // Verificar que la password actual es correcta
    /// if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
    ///     return Result.Failure("Current password is incorrect");
    ///
    /// // Hashear nueva password
    /// var newPasswordHash = _passwordHasher.Hash(request.NewPassword);
    ///
    /// user.UpdatePassword(newPasswordHash);
    /// await _context.SaveChangesAsync();
    ///
    /// MEJORA FUTURA: Password History
    /// - Guardar historial de últimas N passwords
    /// - No permitir reutilizar passwords recientes
    /// - Previene que usuarios roten passwords para reutilizar favoritas
    /// </remarks>
    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new DomainException("Password hash cannot be empty");
        }

        if (IsLockedOut)
        {
            throw new DomainException("Cannot update password of locked out account");
        }

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cambia el rol del usuario.
    /// </summary>
    /// <param name="newRole">Nuevo rol a asignar.</param>
    /// <remarks>
    /// REGLAS DE NEGOCIO:
    ///
    /// 1. Solo un Admin puede cambiar roles (validar en Application layer)
    /// 2. Se debe loguear todos los cambios de rol en auditoría
    ///
    /// Ejemplo de uso en ChangeRoleHandler:
    /// // Verificar que el caller es Admin
    /// if (_currentUser.Role != UserRole.Admin)
    ///     return Result.Failure("Only admins can change roles");
    ///
    /// var user = await _userRepository.GetByIdAsync(request.UserId);
    /// user.ChangeRole(request.NewRole);
    ///
    /// // Loguear cambio en auditoría
    /// _auditLogger.LogRoleChange(_currentUser.UserId, user.Id, user.Role, request.NewRole);
    ///
    /// await _context.SaveChangesAsync();
    ///
    /// SEGURIDAD:
    /// - Verificar authorization en Application/API layer
    /// - Loguear TODOS los cambios de rol
    /// - Considerar notificar al usuario por email
    /// </remarks>
    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
    }
}
