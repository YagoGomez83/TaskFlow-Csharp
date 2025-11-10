namespace TaskManagement.Domain.ValueObjects;

/// <summary>
/// Clase base abstracta para Value Objects en Domain-Driven Design.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE VALUE OBJECTS:
///
/// Un Value Object es un objeto que se define por sus valores, no por su identidad.
/// A diferencia de las Entities (que tienen identidad única vía Id), los Value Objects
/// son inmutables y se comparan por sus propiedades.
///
/// CARACTERÍSTICAS DE UN VALUE OBJECT:
///
/// 1. INMUTABILIDAD:
///    - Una vez creado, no puede cambiar
///    - No tiene setters públicos
///    - Para "modificar" se crea un nuevo instancia
///
/// 2. IGUALDAD POR VALOR:
///    - Dos Value Objects son iguales si sus propiedades son iguales
///    - No importa la referencia en memoria
///    - Ejemplo: Email("test@example.com") == Email("test@example.com")
///
/// 3. SIN IDENTIDAD:
///    - No tiene propiedad Id
///    - Se identifica completamente por sus valores
///
/// 4. AUTOVALIDACIÓN:
///    - Valida sus propiedades en el constructor
///    - No permite crear instancias inválidas
///    - "Make illegal states unrepresentable"
///
/// VENTAJAS:
/// ✅ Encapsula validación en un solo lugar
/// ✅ Previene estados inválidos
/// ✅ Más expresivo que tipos primitivos (Email vs string)
/// ✅ Facilita testing
/// ✅ Evita "Primitive Obsession" (code smell)
///
/// EJEMPLOS DE VALUE OBJECTS:
/// - Email: Encapsula validación de formato email
/// - Address: Calle, ciudad, código postal como una unidad
/// - Money: Cantidad + moneda (no solo un decimal)
/// - DateRange: Fecha inicio + fecha fin con validación
/// - PhoneNumber: Número con validación de formato
///
/// CUÁNDO USAR VALUE OBJECT vs PRIMITIVO:
/// - Usa Value Object si:
///   * Tiene reglas de validación complejas
///   * Tiene comportamiento asociado
///   * Aparece en múltiples lugares
///   * Tiene significado de negocio importante
///
/// - Usa primitivo si:
///   * Es un valor simple sin reglas
///   * Solo aparece en un lugar
///   * No tiene comportamiento
/// </remarks>
public abstract class ValueObject
{
    /// <summary>
    /// Obtiene los componentes que definen la igualdad del Value Object.
    /// </summary>
    /// <remarks>
    /// Este método abstracto debe ser implementado por las clases derivadas
    /// para especificar qué propiedades se usan en la comparación de igualdad.
    ///
    /// Ejemplo para Email:
    /// protected override IEnumerable<object> GetEqualityComponents()
    /// {
    ///     yield return Value.ToLowerInvariant(); // Email es case-insensitive
    /// }
    ///
    /// Ejemplo para Address (múltiples propiedades):
    /// protected override IEnumerable<object> GetEqualityComponents()
    /// {
    ///     yield return Street;
    ///     yield return City;
    ///     yield return PostalCode;
    ///     yield return Country;
    /// }
    /// </remarks>
    protected abstract IEnumerable<object> GetEqualityComponents();

    /// <summary>
    /// Determina si dos Value Objects son iguales comparando sus componentes.
    /// </summary>
    /// <remarks>
    /// La igualdad se basa en los componentes retornados por GetEqualityComponents().
    /// Usamos SequenceEqual() para comparar las secuencias de componentes.
    ///
    /// Ejemplo:
    /// var email1 = Email.Create("test@example.com");
    /// var email2 = Email.Create("test@example.com");
    /// email1.Equals(email2); // true - mismo valor
    ///
    /// var email3 = Email.Create("other@example.com");
    /// email1.Equals(email3); // false - diferente valor
    /// </remarks>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Genera un hash code basado en los componentes del Value Object.
    /// </summary>
    /// <remarks>
    /// Importante: GetHashCode() y Equals() deben ser consistentes.
    /// Si dos objetos son iguales (Equals = true), deben tener el mismo hash code.
    ///
    /// Usamos el hash code para:
    /// - Almacenar en HashSet, Dictionary
    /// - Optimizar búsquedas
    /// - Comparaciones rápidas
    ///
    /// Implementación:
    /// - Combinamos los hash codes de todos los componentes
    /// - Usamos XOR (^) para combinarlos
    /// - Multiplicamos por un primo para mejor distribución
    /// </remarks>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    /// <summary>
    /// Operador de igualdad (==) para Value Objects.
    /// </summary>
    /// <remarks>
    /// Permite usar == en lugar de .Equals()
    ///
    /// Ejemplo:
    /// if (email1 == email2) { ... }
    ///
    /// Sin esta sobrecarga, == compararía referencias, no valores.
    /// </remarks>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// Operador de desigualdad (!=) para Value Objects.
    /// </summary>
    /// <remarks>
    /// Complemento del operador ==
    ///
    /// Ejemplo:
    /// if (email1 != email2) { ... }
    /// </remarks>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Crea una copia del Value Object.
    /// </summary>
    /// <remarks>
    /// Aunque los Value Objects son inmutables, a veces necesitamos copias
    /// para crear nuevas instancias con ligeras variaciones.
    ///
    /// Implementación por defecto usa MemberwiseClone() que hace copia superficial.
    /// Las clases derivadas pueden override si necesitan deep copy.
    /// </remarks>
    protected ValueObject GetCopy()
    {
        return (ValueObject)MemberwiseClone();
    }
}
