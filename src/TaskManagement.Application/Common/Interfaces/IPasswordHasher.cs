namespace TaskManagement.Application.Common.Interfaces;

/// <summary>
/// Define el contrato para hashing y verificación de contraseñas.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE PASSWORD HASHING:
///
/// NUNCA almacenar contraseñas en texto plano en la base de datos.
/// Si la BD es comprometida, el atacante tendría todas las contraseñas.
///
/// ❌ INSEGURO - Texto plano:
/// | UserId | Email           | Password  |
/// |--------|-----------------|-----------|
/// | 1      | john@email.com  | Pass123!  |
/// | 2      | jane@email.com  | Secret99  |
///
/// Si hay una brecha de seguridad:
/// - Atacante tiene todas las contraseñas
/// - Puede usarlas en otros sitios (credential stuffing)
/// - Puede impersonar usuarios
///
/// ✅ SEGURO - Hash:
/// | UserId | Email           | PasswordHash                                                 |
/// |--------|-----------------|--------------------------------------------------------------|
/// | 1      | john@email.com  | $2a$12$KIXxLVQy.jW8Zq5Y8mP9.eO7z... (60 caracteres)            |
/// | 2      | jane@email.com  | $2a$12$8mP9.eO7zKIXxLVQy.jW8Zq5Y... (diferente)                |
///
/// Si hay una brecha:
/// - Atacante solo tiene hashes
/// - NO puede revertir hash a contraseña original (computacionalmente inviable)
/// - Cada hash es único (gracias al salt)
///
/// HASHING vs ENCRYPTION:
///
/// HASHING (one-way):
/// - Input: "Password123"
/// - Output: "$2a$12$KIXxLVQy.jW8Zq5Y8mP9.eO7z..."
/// - NO se puede revertir (one-way function)
/// - Mismo input → mismo output (determinístico)
/// - Usado para contraseñas, checksums
///
/// ENCRYPTION (two-way):
/// - Input: "Password123"
/// - Output: "Qx7mK9..." (con clave)
/// - SE puede revertir con la clave
/// - Usado para datos que necesitas recuperar
///
/// Para contraseñas, SIEMPRE usar hashing (one-way).
///
/// TIPOS DE HASHING:
///
/// 1. FAST HASHES (❌ NO usar para passwords):
///    - MD5, SHA1, SHA256
///    - Diseñados para ser RÁPIDOS
///    - Problema: Atacante puede probar millones de contraseñas por segundo
///    - MD5: 50 mil millones hashes/segundo en GPU moderna
///    - SHA256: 10 mil millones hashes/segundo
///
///    ❌ SHA256("Password123") = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918"
///    Un atacante puede probar todo el diccionario en segundos.
///
/// 2. SLOW HASHES (✅ usar para passwords):
///    - BCrypt, Argon2, PBKDF2, scrypt
///    - Diseñados para ser LENTOS (computacionalmente costosos)
///    - BCrypt: ~10 hashes/segundo (configurable con work factor)
///    - Atacante solo puede probar miles (no millones) por segundo
///
/// BCRYPT:
///
/// BCrypt es un algoritmo de hashing diseñado específicamente para contraseñas.
/// Basado en el cipher Blowfish.
///
/// Características:
/// - Incorpora salt automáticamente (no necesitas generarlo manualmente)
/// - Configurable work factor (a mayor factor, más lento y más seguro)
/// - Resistente a ataques de fuerza bruta
/// - Resistente a rainbow tables (gracias al salt)
///
/// ESTRUCTURA DE UN BCRYPT HASH:
///
/// $2a$12$KIXxLVQy.jW8Zq5Y8mP9.eO7zKIXxLVQy.jW8Zq5Y8mP9.eO7zKIX
/// │ │  │  │                    │                                  │
/// │ │  │  │                    │                                  └─ Hash (31 chars)
/// │ │  │  │                    └─ Salt (22 chars)
/// │ │  │  └─ Work factor (12 = 2^12 = 4096 iterations)
/// │ │  └─ Formato versión (minor revision)
/// │ └─ BCrypt version (2a = latest)
/// └─ Identificador de algoritmo ($)
///
/// Ejemplo real:
/// Input: "MySecurePassword123!"
/// Output: "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYzXq8nJ5u6"
///
/// COMPONENTES:
/// - $2a: Versión de BCrypt
/// - $12: Work factor (2^12 = 4096 iteraciones)
/// - LQv3c1yqBWVHxkd0LHAkCO: Salt (22 caracteres, base64)
/// - Yz6TtxMQJqhN8/LewY5GyYzXq8nJ5u6: Hash (31 caracteres, base64)
///
/// SALT:
///
/// Un salt es un valor aleatorio que se agrega a la contraseña antes de hashear.
///
/// Sin salt:
/// hash("Password123") → siempre produce el mismo hash
/// Problema: Rainbow tables (tablas pre-computadas de hashes comunes)
///
/// Con salt:
/// hash("Password123" + "KIXxLVQy") → hash único
/// hash("Password123" + "8mP9eO7z") → hash diferente
/// Misma contraseña → hashes diferentes
///
/// BCrypt genera salt automáticamente:
/// var hash1 = BCrypt.HashPassword("Password123");
/// var hash2 = BCrypt.HashPassword("Password123");
/// // hash1 ≠ hash2 (diferentes salts)
///
/// El salt se almacena en el hash (primeros 29 caracteres).
/// Al verificar, BCrypt extrae el salt del hash y lo reutiliza.
///
/// WORK FACTOR:
///
/// El work factor determina cuántas iteraciones se aplican al hash.
/// A mayor work factor, más tiempo toma hashear y verificar.
///
/// Work Factor | Iteraciones | Tiempo  | Seguridad
/// ------------|-------------|---------|----------
/// 10          | 2^10 (1024) | ~100ms  | Mínimo aceptable
/// 12          | 2^12 (4096) | ~250ms  | Recomendado (default)
/// 14          | 2^14 (16K)  | ~1s     | Alta seguridad
/// 16          | 2^16 (65K)  | ~4s     | Muy alta (puede ser UX issue)
///
/// Recomendación: Usar 12 (balance seguridad/performance)
///
/// Con el tiempo, el hardware mejora, así que el work factor debe incrementarse:
/// - 2010: work factor 10 era suficiente
/// - 2024: work factor 12 es recomendado
/// - 2030: probablemente work factor 14
///
/// IMPORTANTE: Mayor work factor también afecta performance de login.
/// Si work factor = 16, cada login tarda ~4 segundos (malo para UX).
///
/// VERIFICACIÓN:
///
/// Para verificar una contraseña:
/// 1. Usuario envía contraseña en texto plano (HTTPS)
/// 2. Server busca hash almacenado en BD
/// 3. Server extrae salt del hash
/// 4. Server hashea contraseña con ese salt
/// 5. Server compara hash generado con hash almacenado
///
/// var isValid = BCrypt.Verify(plainPassword, storedHash);
///
/// BCrypt.Verify("Password123", "$2a$12$KIXxLVQy.jW8Zq5Y8mP9.eO7z...");
/// // 1. Extrae salt: "KIXxLVQy.jW8Zq5Y8mP9.eO7z"
/// // 2. Hashea "Password123" con ese salt
/// // 3. Compara resultado con hash almacenado
/// // 4. Retorna true si coinciden
///
/// TIMING ATTACKS:
///
/// BCrypt.Verify() usa comparación timing-safe para prevenir timing attacks.
///
/// ❌ Comparación naive (vulnerable):
/// bool Verify(string input, string hash)
/// {
///     return Hash(input) == hash;  // Retorna en cuanto encuentra diferencia
/// }
///
/// Problema: Atacante mide tiempo de respuesta:
/// - Si contraseña correcta hasta el carácter 5: tarda 5µs
/// - Si contraseña correcta hasta el carácter 10: tarda 10µs
/// - Puede deducir contraseña carácter por carácter
///
/// ✅ Comparación timing-safe:
/// bool Verify(string input, string hash)
/// {
///     var inputHash = Hash(input);
///     // Compara TODOS los caracteres, sin importar si ya encontró diferencia
///     return ConstantTimeEquals(inputHash, hash);
/// }
///
/// BCrypt.Verify() ya implementa esto, no necesitas preocuparte.
///
/// SEGURIDAD ADICIONAL - PEPPER:
///
/// Opcionalmente, puedes agregar un "pepper" (secret key) a las contraseñas:
///
/// var passwordWithPepper = password + Environment.GetEnvironmentVariable("PEPPER");
/// var hash = BCrypt.HashPassword(passwordWithPepper);
///
/// Pepper:
/// - Secret almacenado en variables de entorno (NO en BD)
/// - Mismo para todos los usuarios
/// - Si BD es comprometida, atacante NO tiene el pepper
/// - Hace inviable el cracking incluso con hashes
///
/// Desventaja:
/// - Si pierdes el pepper, todos los passwords son inválidos
/// - No puedes rotarlo fácilmente
///
/// Para este proyecto NO usamos pepper (simplicidad).
/// Para alta seguridad, considera usarlo.
///
/// ROTACIÓN DE WORK FACTOR:
///
/// Con el tiempo, querrás incrementar el work factor.
/// Estrategia de migración:
///
/// public void UpgradeHashIfNeeded(User user, string plainPassword)
/// {
///     var currentWorkFactor = ExtractWorkFactor(user.PasswordHash);
///
///     if (currentWorkFactor < 12)
///     {
///         // Re-hashear con work factor nuevo
///         var newHash = BCrypt.HashPassword(plainPassword, 12);
///         user.UpdatePassword(newHash);
///         _context.SaveChangesAsync();
///     }
/// }
///
/// Llamar esto al momento del login (cuando tienes plaintext password).
///
/// POLÍTICAS DE CONTRASEÑAS:
///
/// Además de hashing, implementar políticas:
///
/// 1. Longitud mínima: 8 caracteres (mejor 12+)
/// 2. Complejidad: Mayúscula, minúscula, número, símbolo
/// 3. No permitir contraseñas comunes (password123, qwerty, etc.)
/// 4. No permitir contraseñas comprometidas (check HaveIBeenPwned API)
/// 5. Expiración: Opcional (NIST ya no lo recomienda)
/// 6. No reutilizar últimas N contraseñas
///
/// Validar con FluentValidation:
///
/// public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
/// {
///     public RegisterCommandValidator()
///     {
///         RuleFor(x => x.Password)
///             .MinimumLength(8)
///             .WithMessage("Password must be at least 8 characters")
///             .Matches(@"[A-Z]")
///             .WithMessage("Password must contain at least one uppercase letter")
///             .Matches(@"[a-z]")
///             .WithMessage("Password must contain at least one lowercase letter")
///             .Matches(@"\d")
///             .WithMessage("Password must contain at least one number")
///             .Matches(@"[\W_]")
///             .WithMessage("Password must contain at least one special character");
///     }
/// }
///
/// EJEMPLO DE USO EN HANDLER:
///
/// // REGISTER
/// public async Task<Result> Handle(RegisterCommand request, CancellationToken ct)
/// {
///     // 1. Validar que email no existe
///     var existingUser = await _context.Users
///         .AnyAsync(u => u.Email == Email.Create(request.Email), ct);
///
///     if (existingUser)
///         return Result.Failure("Email already registered");
///
///     // 2. Hashear contraseña
///     var passwordHash = _passwordHasher.Hash(request.Password);
///
///     // 3. Crear usuario
///     var user = User.Create(
///         Email.Create(request.Email),
///         passwordHash,
///         UserRole.User
///     );
///
///     _context.Users.Add(user);
///     await _context.SaveChangesAsync(ct);
///
///     return Result.Success();
/// }
///
/// // LOGIN
/// public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken ct)
/// {
///     // 1. Buscar usuario
///     var user = await _context.Users
///         .FirstOrDefaultAsync(u => u.Email == Email.Create(request.Email), ct);
///
///     if (user == null)
///         return Result.Failure<AuthResponse>("Invalid credentials");
///
///     // 2. Verificar contraseña
///     if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
///     {
///         user.RecordFailedLogin();
///         await _context.SaveChangesAsync(ct);
///         return Result.Failure<AuthResponse>("Invalid credentials");
///     }
///
///     // 3. Verificar lockout
///     if (!user.CanLogin())
///         return Result.Failure<AuthResponse>($"Account locked until {user.LockedOutUntil}");
///
///     // 4. Reset failed attempts
///     user.ResetLoginAttempts();
///
///     // 5. Generar tokens...
/// }
///
/// // CHANGE PASSWORD
/// public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken ct)
/// {
///     var user = await _context.Users.FindAsync(request.UserId);
///
///     // 1. Verificar contraseña actual
///     if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
///         return Result.Failure("Current password is incorrect");
///
///     // 2. Hashear nueva contraseña
///     var newPasswordHash = _passwordHasher.Hash(request.NewPassword);
///
///     // 3. Actualizar
///     user.UpdatePassword(newPasswordHash);
///     await _context.SaveChangesAsync(ct);
///
///     return Result.Success();
/// }
///
/// BIBLIOTECAS:
///
/// .NET tiene varias opciones para BCrypt:
///
/// 1. BCrypt.Net-Next (recomendado):
///    - Más popular y mantenido
///    - Instalación: dotnet add package BCrypt.Net-Next
///    - Uso: BCrypt.Net.BCrypt.HashPassword(password)
///
/// 2. Microsoft.AspNetCore.Cryptography.KeyDerivation:
///    - Usa PBKDF2 (no BCrypt)
///    - Built-in en ASP.NET Core
///    - Más configuración manual (salt, iterations)
///
/// 3. Argon2 (alternativa moderna):
///    - Ganador de Password Hashing Competition 2015
///    - Más resistente a GPU/ASIC attacks
///    - Instalación: dotnet add package Isopoh.Cryptography.Argon2
///
/// Para este proyecto usaremos BCrypt.Net-Next por ser el estándar de facto.
///
/// TESTING:
///
/// [Fact]
/// public void Hash_SamePasswordTwice_ProducesDifferentHashes()
/// {
///     var password = "TestPassword123!";
///     var hash1 = _passwordHasher.Hash(password);
///     var hash2 = _passwordHasher.Hash(password);
///
///     Assert.NotEqual(hash1, hash2);  // Diferentes salts
/// }
///
/// [Fact]
/// public void Verify_CorrectPassword_ReturnsTrue()
/// {
///     var password = "TestPassword123!";
///     var hash = _passwordHasher.Hash(password);
///
///     var result = _passwordHasher.Verify(password, hash);
///
///     Assert.True(result);
/// }
///
/// [Fact]
/// public void Verify_IncorrectPassword_ReturnsFalse()
/// {
///     var password = "TestPassword123!";
///     var hash = _passwordHasher.Hash(password);
///
///     var result = _passwordHasher.Verify("WrongPassword", hash);
///
///     Assert.False(result);
/// }
/// </remarks>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashea una contraseña usando BCrypt.
    /// </summary>
    /// <param name="password">Contraseña en texto plano.</param>
    /// <returns>Hash BCrypt de la contraseña (60 caracteres).</returns>
    /// <remarks>
    /// Genera un hash BCrypt con:
    /// - Salt aleatorio (generado automáticamente)
    /// - Work factor 12 (2^12 = 4096 iteraciones)
    /// - Algoritmo BCrypt 2a
    ///
    /// Entrada: "MyPassword123!"
    /// Salida: "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYzXq8nJ5u6"
    ///
    /// Propiedades:
    /// - Deterministico con mismo salt, pero salt es aleatorio
    /// - Cada llamada produce hash diferente (diferente salt)
    /// - One-way: NO se puede revertir hash a contraseña
    /// - Lento: ~250ms (previene ataques de fuerza bruta)
    ///
    /// IMPORTANTE:
    /// - NUNCA hashear contraseña en el cliente (frontend)
    /// - SIEMPRE enviar contraseña en texto plano por HTTPS
    /// - Hashear solo en el servidor
    ///
    /// Razón: Si hasheas en frontend, el hash se convierte en la "contraseña".
    /// Atacante puede capturar el hash y enviarlo directamente (pass-the-hash attack).
    ///
    /// Uso:
    /// var hash = _passwordHasher.Hash("UserPassword123");
    /// user.UpdatePassword(hash);
    /// await _context.SaveChangesAsync();
    ///
    /// Performance:
    /// Con work factor 12, tarda ~250ms por hash.
    /// Esto es INTENCIONAL para prevenir fuerza bruta.
    /// No caches el resultado, necesitas generarlo cada vez.
    /// </remarks>
    string Hash(string password);

    /// <summary>
    /// Verifica si una contraseña coincide con un hash BCrypt.
    /// </summary>
    /// <param name="password">Contraseña en texto plano a verificar.</param>
    /// <param name="hash">Hash BCrypt almacenado.</param>
    /// <returns>true si la contraseña es correcta, false si no.</returns>
    /// <remarks>
    /// Proceso:
    /// 1. Extrae salt del hash (primeros 29 caracteres)
    /// 2. Hashea password con ese salt
    /// 3. Compara hash generado con hash almacenado (timing-safe)
    /// 4. Retorna true si coinciden
    ///
    /// Ejemplo:
    /// var isValid = _passwordHasher.Verify("UserPassword123", storedHash);
    /// if (!isValid)
    ///     return Result.Failure("Invalid credentials");
    ///
    /// SEGURIDAD:
    /// - Usa comparación timing-safe (previene timing attacks)
    /// - NO revela si usuario existe (siempre retorna bool)
    /// - Tarda ~250ms (previene fuerza bruta)
    ///
    /// IMPORTANTE - Mensajes de error:
    /// ❌ No hacer:
    /// if (user == null)
    ///     return "User not found";  // Revela que email no está registrado
    /// if (!passwordHasher.Verify(...))
    ///     return "Incorrect password";  // Revela que email existe
    ///
    /// ✅ Hacer:
    /// if (user == null || !passwordHasher.Verify(...))
    ///     return "Invalid credentials";  // Genérico, no revela nada
    ///
    /// Esto previene user enumeration attacks.
    ///
    /// Performance:
    /// Con work factor 12, tarda ~250ms por verificación.
    /// Esto protege contra brute force, pero afecta performance de login.
    /// NO cachees el resultado, necesitas verificarlo cada vez.
    ///
    /// Rate Limiting:
    /// Combinar con rate limiting para prevenir ataques:
    /// - Máximo 5 intentos por IP por minuto
    /// - Lockout después de 5 intentos fallidos
    /// - CAPTCHA después de 3 intentos fallidos
    /// </remarks>
    bool Verify(string password, string hash);
}
