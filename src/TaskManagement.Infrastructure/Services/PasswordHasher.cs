using TaskManagement.Application.Common.Interfaces;
using BCrypt.Net;

namespace TaskManagement.Infrastructure.Services;

/// <summary>
/// Servicio para hash y verificación de contraseñas usando BCrypt.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE BCRYPT:
///
/// BCrypt es un algoritmo de hashing de contraseñas diseñado específicamente
/// para ser LENTO (computacionalmente costoso), lo que dificulta ataques de fuerza bruta.
///
/// ¿POR QUÉ NO USAR SHA256, MD5, etc.?
///
/// SHA256, MD5, SHA1 son algoritmos de hash RÁPIDOS, diseñados para checksums.
/// Pueden calcular millones de hashes por segundo con GPU.
///
/// Ejemplo de ataque con SHA256:
/// - GPU moderna: 10,000,000,000 hashes/segundo
/// - Contraseña de 8 caracteres (a-z, A-Z, 0-9): 62^8 = 218 trillones
/// - Tiempo para romper: 218,000,000,000,000 / 10,000,000,000 = 6 horas
///
/// Con BCrypt (work factor 12):
/// - ~10 hashes/segundo (1,000,000x más lento)
/// - Mismo ataque: 6 horas * 1,000,000 = 684,000 horas = 78 AÑOS
///
/// FUNCIONAMIENTO DE BCRYPT:
///
/// 1. SALT (Sal):
/// - Valor aleatorio único por contraseña
/// - Previene rainbow tables (tablas precalculadas de hashes)
/// - BCrypt genera salt automáticamente
///
/// 2. WORK FACTOR (Cost):
/// - Número de rondas de hashing (2^work_factor iteraciones)
/// - Work factor 12 = 2^12 = 4,096 iteraciones
/// - Cada incremento duplica el tiempo de cálculo
///
/// 3. ALGORITMO:
/// - Genera salt aleatorio
/// - Aplica Blowfish cipher 2^cost veces
/// - Combina salt + hash en un solo string
///
/// FORMATO DEL HASH:
///
/// $2a$12$R9h/cIPz0gi.URNNX3kh2OPST9/PgBkqquzi.Ss7KIUgO2t0jWMUW
/// │ │ │ │                        └── Hash (31 chars)
/// │ │ │ └────────────────────────────── Salt (22 chars)
/// │ │ └──────────────────────────────── Work factor (12)
/// │ └────────────────────────────────── Versión de BCrypt (2a)
/// └──────────────────────────────────── Identificador de BCrypt
///
/// Ejemplo desglosado:
/// $2a          → Versión de BCrypt
/// $12          → Work factor (2^12 = 4,096 iteraciones)
/// $R9h...h2OPS → Salt (22 caracteres en Base64)
/// T9...WMUy    → Hash de la contraseña (31 caracteres en Base64)
///
/// VENTAJAS DE BCRYPT:
///
/// 1. SALT AUTOMÁTICO:
/// - No necesitas generar y almacenar salt por separado
/// - Cada hash incluye su propio salt
///
/// 2. RESISTENTE AL TIEMPO:
/// - Puedes incrementar work factor cuando hardware mejora
/// - Mantiene seguridad a largo plazo
///
/// 3. DESIGNED FOR PASSWORDS:
/// - Límite de 72 bytes para prevenir DoS
/// - Tiempo constante para prevenir timing attacks
///
/// WORK FACTOR RECOMENDADO:
///
/// Work Factor | Tiempo (aprox) | Uso recomendado
/// ------------|----------------|------------------
/// 10          | ~100ms         | Desarrollo/Testing
/// 12          | ~250ms         | Producción normal (RECOMENDADO)
/// 13          | ~500ms         | Alta seguridad
/// 14          | ~1s            | Muy alta seguridad
/// 15+         | ~2s+           | Sistemas críticos (bancos)
///
/// Para este proyecto usamos 12 (balance seguridad/performance).
///
/// COMPARACIÓN CON OTROS ALGORITMOS:
///
/// MD5 (NO USAR):
/// ❌ Rápido (vulnerable a GPU)
/// ❌ Sin salt automático
/// ❌ Colisiones conocidas
///
/// SHA256 (NO USAR para contraseñas):
/// ❌ Rápido (vulnerable a GPU)
/// ❌ Sin salt automático
/// ⚠️ OK para checksums, NO para contraseñas
///
/// PBKDF2 (Alternativa válida):
/// ✅ Lento y configurable
/// ✅ Estándar (NIST)
/// ⚠️ Requiere gestión manual de salt
/// ⚠️ Vulnerable a ataques con hardware especializado
///
/// Argon2 (Más moderno):
/// ✅ Ganador de Password Hashing Competition (2015)
/// ✅ Resistente a GPU y ASIC
/// ✅ Configurable (memoria + CPU)
/// ⚠️ Menos maduro que BCrypt
/// ⚠️ Mayor consumo de memoria
///
/// BCrypt (RECOMENDADO para mayoría de casos):
/// ✅ Maduro y probado (1999)
/// ✅ Ampliamente usado y auditado
/// ✅ Fácil de implementar
/// ✅ Buen balance seguridad/performance
///
/// MIGRACIÓN DE HASHES:
///
/// Si tienes hashes antiguos (SHA256, MD5), puedes migrar:
///
/// 1. Agregar columna "HashType" a User
/// 2. Durante login, verificar HashType:
///    - Si es "Legacy", verificar con algoritmo viejo
///    - Si válido, re-hashear con BCrypt
///    - Actualizar hash y HashType en BD
/// 3. Eventualmente todos serán BCrypt
///
/// EJEMPLO DE MIGRACIÓN:
///
/// public bool VerifyPassword(string password, string hash, string hashType)
/// {
///     if (hashType == "BCrypt")
///         return BCrypt.Net.BCrypt.Verify(password, hash);
///
///     if (hashType == "SHA256-Legacy")
///     {
///         var legacyHash = ComputeSHA256(password + salt);
///         if (legacyHash == hash)
///         {
///             // Re-hashear con BCrypt
///             var newHash = BCrypt.Net.BCrypt.HashPassword(password, 12);
///             UpdateUserHash(userId, newHash, "BCrypt");
///             return true;
///         }
///     }
///
///     return false;
/// }
///
/// RATE LIMITING:
///
/// Aunque BCrypt es lento, agregar rate limiting adicional:
///
/// - Limitar intentos de login (5 intentos en 15 minutos)
/// - Implementado en User.RecordFailedLogin() y User.CanLogin()
/// - Previene ataques de fuerza bruta distribuidos
///
/// VERIFICACIÓN:
///
/// BCrypt.Verify() es SEGURO contra timing attacks.
/// Usa tiempo constante para comparar hashes.
///
/// NO HACER (vulnerable):
/// if (hash == storedHash)  // Timing attack vulnerable
///
/// HACER (seguro):
/// if (BCrypt.Verify(password, storedHash))  // Timing-safe
///
/// TESTING:
///
/// [Fact]
/// public void Hash_ShouldGenerateDifferentHashForSamePassword()
/// {
///     var password = "MyPassword123!";
///
///     var hash1 = _passwordHasher.Hash(password);
///     var hash2 = _passwordHasher.Hash(password);
///
///     // Diferentes porque cada hash tiene salt diferente
///     Assert.NotEqual(hash1, hash2);
/// }
///
/// [Fact]
/// public void Verify_ValidPassword_ShouldReturnTrue()
/// {
///     var password = "MyPassword123!";
///     var hash = _passwordHasher.Hash(password);
///
///     var result = _passwordHasher.Verify(password, hash);
///
///     Assert.True(result);
/// }
///
/// [Fact]
/// public void Verify_InvalidPassword_ShouldReturnFalse()
/// {
///     var hash = _passwordHasher.Hash("CorrectPassword123!");
///     var result = _passwordHasher.Verify("WrongPassword123!", hash);
///
///     Assert.False(result);
/// }
///
/// [Fact]
/// public void Hash_ShouldTakeLessThan500ms()
/// {
///     var sw = Stopwatch.StartNew();
///     _passwordHasher.Hash("TestPassword123!");
///     sw.Stop();
///
///     Assert.True(sw.ElapsedMilliseconds < 500);
/// }
///
/// MEJORES PRÁCTICAS:
///
/// 1. NUNCA loggear contraseñas (ni siquiera parcialmente)
/// 2. NUNCA enviar contraseñas por email
/// 3. SIEMPRE usar HTTPS (contraseñas en tránsito)
/// 4. Implementar rate limiting en login
/// 5. Implementar reset de contraseña seguro (token de un solo uso)
/// 6. Requerir contraseñas fuertes (implementado en RegisterCommandValidator)
/// 7. Considerar 2FA para cuentas sensibles
///
/// OWASP TOP 10:
///
/// BCrypt ayuda a prevenir:
/// - A02:2021 – Cryptographic Failures
/// - A07:2021 – Identification and Authentication Failures
///
/// Pero NO previene:
/// - Phishing
/// - Keyloggers
/// - Man-in-the-middle (usar HTTPS)
/// - Credential stuffing (usar rate limiting)
/// </remarks>
public class PasswordHasher : IPasswordHasher
{
    // Work factor 12 = 2^12 = 4,096 iteraciones
    // Tiempo de cálculo: ~250ms
    // Balance óptimo entre seguridad y performance
    private const int WorkFactor = 12;

    /// <summary>
    /// Genera un hash BCrypt de la contraseña.
    /// </summary>
    /// <param name="password">Contraseña en texto plano.</param>
    /// <returns>Hash BCrypt con salt incluido.</returns>
    /// <remarks>
    /// El hash generado incluye:
    /// - Versión de BCrypt
    /// - Work factor
    /// - Salt (generado automáticamente)
    /// - Hash de la contraseña
    ///
    /// Formato: $2a$12$[22 chars salt][31 chars hash]
    ///
    /// Cada llamada genera un hash diferente debido al salt aleatorio.
    /// </remarks>
    public string Hash(string password)
    {
        // BCrypt.HashPassword genera:
        // 1. Salt aleatorio (22 caracteres en Base64)
        // 2. Aplica Blowfish cipher 2^12 veces
        // 3. Retorna string con versión + work factor + salt + hash
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// Verifica si la contraseña coincide con el hash.
    /// </summary>
    /// <param name="password">Contraseña en texto plano.</param>
    /// <param name="hash">Hash BCrypt almacenado.</param>
    /// <returns>True si la contraseña es correcta, False en caso contrario.</returns>
    /// <remarks>
    /// BCrypt.Verify:
    /// 1. Extrae salt del hash almacenado
    /// 2. Hashea la contraseña proporcionada con ese salt
    /// 3. Compara en tiempo constante (previene timing attacks)
    ///
    /// SEGURO: Usa tiempo constante para comparación.
    /// NO VULNERABLE a timing attacks.
    /// </remarks>
    public bool Verify(string password, string hash)
    {
        try
        {
            // BCrypt.Verify extrae automáticamente el salt del hash
            // y lo usa para hashear la contraseña proporcionada
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // Si el hash está corrupto o en formato inválido, retorna false
            return false;
        }
    }
}
