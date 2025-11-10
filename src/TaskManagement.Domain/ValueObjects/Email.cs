using System.Text.RegularExpressions;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.Domain.ValueObjects;

/// <summary>
/// Value Object que representa una dirección de email válida.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DEL PATRÓN VALUE OBJECT PARA EMAIL:
///
/// POR QUÉ NO USAR SOLO STRING:
///
/// ❌ Problema con string:
/// public string Email { get; set; }
/// - Puede contener cualquier valor ("abc", "123", "")
/// - Validación debe repetirse en múltiples lugares
/// - Fácil olvidar validar
/// - No es type-safe: cualquier string puede asignarse
///
/// ✅ Solución con Value Object:
/// public Email Email { get; private set; }
/// - Solo puede contener emails válidos
/// - Validación centralizada en un lugar
/// - Imposible crear Email inválido
/// - Type-safe: solo Email puede asignarse
///
/// VALIDACIONES IMPLEMENTADAS:
///
/// 1. No puede ser null o vacío
/// 2. Debe cumplir formato básico de email (regex)
/// 3. Se normaliza a lowercase (email es case-insensitive)
/// 4. Se eliminan espacios en blanco
///
/// FORMATO REGEX EXPLICADO:
/// ^[^@\s]+@[^@\s]+\.[^@\s]+$
///
/// ^ = inicio del string
/// [^@\s]+ = uno o más caracteres que NO sean @ o espacio (parte local)
/// @ = símbolo arroba literal
/// [^@\s]+ = uno o más caracteres que NO sean @ o espacio (dominio)
/// \. = punto literal
/// [^@\s]+ = uno o más caracteres que NO sean @ o espacio (TLD)
/// $ = fin del string
///
/// Ejemplos válidos:
/// ✅ test@example.com
/// ✅ user.name+tag@example.co.uk
/// ✅ 123@test.io
///
/// Ejemplos inválidos:
/// ❌ test (sin @)
/// ❌ test@example (sin TLD)
/// ❌ @example.com (sin parte local)
/// ❌ test @example.com (espacio)
///
/// NOTA: Este regex es básico. RFC 5322 completo es muy complejo.
/// Para validación perfecta, considera usar MailAddress.TryParse() o
/// librerías especializadas como FluentEmail.
///
/// NORMALIZACIÓN:
/// - "Test@Example.COM" → "test@example.com"
/// - Esto permite comparación case-insensitive
/// - Evita duplicados como "user@test.com" y "User@Test.COM"
/// </remarks>
public class Email : ValueObject
{
    /// <summary>
    /// Regex para validación básica de formato de email.
    /// </summary>
    /// <remarks>
    /// Compilado con RegexOptions.Compiled para mejor performance.
    /// El patrón se compila una vez y se reutiliza en todas las validaciones.
    ///
    /// Performance:
    /// - Primera llamada: ~10-20ms (compilación)
    /// - Llamadas subsecuentes: ~0.01ms
    ///
    /// Sin Compiled: cada validación tomaría ~0.1ms
    /// </remarks>
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Valor del email normalizado (lowercase, sin espacios).
    /// </summary>
    /// <remarks>
    /// private set: solo puede ser asignado dentro de la clase.
    /// Garantiza inmutabilidad desde el exterior.
    ///
    /// Siempre está en lowercase por consistencia:
    /// - Facilita comparaciones
    /// - Evita duplicados
    /// - Email es case-insensitive por RFC
    /// </remarks>
    public string Value { get; private set; }

    /// <summary>
    /// Constructor privado para forzar el uso del factory method Create().
    /// </summary>
    /// <remarks>
    /// PATRÓN FACTORY METHOD:
    ///
    /// En lugar de: new Email(value)
    /// Usamos: Email.Create(value)
    ///
    /// Ventajas:
    /// ✅ Nombre más expresivo (Create, CreateFromUserInput, etc.)
    /// ✅ Permite retornar null o Result<Email> en caso de error
    /// ✅ Puede tener múltiples factory methods con diferentes parámetros
    /// ✅ Encapsula lógica de creación compleja
    ///
    /// Ejemplo de uso:
    /// try
    /// {
    ///     var email = Email.Create(userInput);
    ///     // email es garantizado válido
    /// }
    /// catch (DomainException ex)
    /// {
    ///     // Manejar email inválido
    ///     Console.WriteLine(ex.Message);
    /// }
    /// </remarks>
    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Factory method para crear una instancia de Email validada.
    /// </summary>
    /// <param name="value">Dirección de email a validar.</param>
    /// <returns>Instancia de Email validada.</returns>
    /// <exception cref="DomainException">
    /// Si el email es null, vacío o no cumple el formato válido.
    /// </exception>
    /// <remarks>
    /// FLUJO DE VALIDACIÓN:
    ///
    /// 1. Verifica que no sea null/vacío
    /// 2. Limpia espacios en blanco
    /// 3. Convierte a lowercase
    /// 4. Valida formato con regex
    /// 5. Si todo OK, retorna Email válido
    /// 6. Si falla, lanza DomainException
    ///
    /// EXCEPCIONES EN DOMAIN:
    ///
    /// Lanzamos DomainException (no ValidationException) porque:
    /// - Estamos en la capa Domain
    /// - Es una violación de reglas de negocio
    /// - No es un error de validación de input de usuario (eso es en Application)
    ///
    /// Las excepciones de Domain se capturan en:
    /// - Application Layer: se convierten en Result<T> failures
    /// - API Layer: se convierten en 400 Bad Request
    ///
    /// Alternativa sin excepciones (Result Pattern):
    /// public static Result<Email> Create(string value)
    /// {
    ///     if (string.IsNullOrWhiteSpace(value))
    ///         return Result.Failure<Email>("Email cannot be empty");
    ///
    ///     // ... más validaciones
    ///
    ///     return Result.Success(new Email(normalized));
    /// }
    /// </remarks>
    public static Email Create(string value)
    {
        // Validación 1: No null o vacío
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Email cannot be empty");
        }

        // Normalización: limpiar y lowercase
        var normalized = value.Trim().ToLowerInvariant();

        // Validación 2: Formato válido
        if (!IsValidEmail(normalized))
        {
            throw new DomainException($"Invalid email format: {value}");
        }

        // Email válido, crear instancia
        return new Email(normalized);
    }

    /// <summary>
    /// Valida si una cadena cumple el formato de email.
    /// </summary>
    /// <param name="email">Cadena a validar.</param>
    /// <returns>true si es válido, false en caso contrario.</returns>
    /// <remarks>
    /// Validación en dos pasos:
    /// 1. Longitud razonable (max 254 caracteres según RFC 5321)
    /// 2. Cumple regex básico
    ///
    /// RFC 5321 especifica:
    /// - Parte local (antes de @): max 64 caracteres
    /// - Dominio (después de @): max 255 caracteres
    /// - Total: max 254 caracteres (320 en versiones anteriores)
    ///
    /// Validación adicional opcional (no implementada aquí):
    /// - Verificar que el dominio existe (DNS lookup)
    /// - Verificar que el mailbox existe (SMTP)
    /// - Lista negra de dominios desechables
    /// - Validación de caracteres especiales según RFC 5322
    /// </remarks>
    private static bool IsValidEmail(string email)
    {
        // Validación de longitud según RFC 5321
        if (email.Length > 254)
        {
            return false;
        }

        // Validación de formato con regex
        return EmailRegex.IsMatch(email);
    }

    /// <summary>
    /// Retorna los componentes que definen la igualdad del Email.
    /// </summary>
    /// <remarks>
    /// Para Email, solo la propiedad Value define la igualdad.
    /// Ya está en lowercase, así que la comparación es directa.
    ///
    /// Ejemplo:
    /// var email1 = Email.Create("test@example.com");
    /// var email2 = Email.Create("TEST@EXAMPLE.COM");
    /// email1 == email2; // true - ambos normalizados a lowercase
    /// </remarks>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>
    /// Retorna la representación en string del Email.
    /// </summary>
    /// <remarks>
    /// Útil para:
    /// - Logging: Console.WriteLine(email)
    /// - Debugging: Inspeccionar valor en debugger
    /// - Serialización: JSON, XML
    ///
    /// Retorna el valor normalizado (lowercase).
    /// </remarks>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    /// Conversión implícita de Email a string.
    /// </summary>
    /// <remarks>
    /// Permite usar Email donde se espera string sin casting explícito.
    ///
    /// Ejemplo:
    /// Email email = Email.Create("test@example.com");
    /// string emailString = email; // Conversión implícita
    /// Console.WriteLine($"Email: {email}"); // También funciona
    ///
    /// Sin este operador:
    /// string emailString = email.Value; // Sería necesario
    /// </remarks>
    public static implicit operator string(Email email)
    {
        return email.Value;
    }
}
