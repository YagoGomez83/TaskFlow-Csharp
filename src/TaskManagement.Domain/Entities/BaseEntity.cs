namespace TaskManagement.Domain.Entities;

/// <summary>
/// Clase base para todas las entidades del dominio.
/// Implementa propiedades comunes como Id, CreatedAt, UpdatedAt, IsDeleted.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE PROPIEDADES:
///
/// - Id (Guid): Identificador único global. Usamos Guid en lugar de int para:
///   * Evitar problemas de colisión en sistemas distribuidos
///   * Permitir generación del ID en cliente sin depender de la DB
///   * Mayor seguridad (no se puede enumerar como con int secuenciales)
///
/// - CreatedAt (DateTime): Timestamp de creación. Usado para:
///   * Auditoría: saber cuándo se creó el registro
///   * Ordenamiento cronológico
///   * Análisis temporal de datos
///
/// - UpdatedAt (DateTime): Timestamp de última modificación. Usado para:
///   * Auditoría: rastrear cuándo fue la última vez que se modificó
///   * Resolución de conflictos (optimistic concurrency)
///   * Cache invalidation (invalidar cache si el dato cambió)
///
/// - IsDeleted (bool): Flag de soft delete. Por qué soft delete:
///   * Mantener integridad referencial (no romper FKs)
///   * Auditoría: poder rastrear qué se eliminó y cuándo
///   * Recuperación: posibilidad de restaurar datos eliminados accidentalmente
///   * Cumplimiento legal: algunas regulaciones requieren mantener historial
///
/// - DeletedAt (DateTime?): Timestamp de eliminación lógica. Nullable porque:
///   * Solo tiene valor si IsDeleted = true
///   * Permite saber exactamente cuándo se eliminó
///   * Útil para políticas de retención (ej: eliminar definitivamente después de 30 días)
/// </remarks>
public abstract class BaseEntity
{
    /// <summary>
    /// Identificador único de la entidad.
    /// </summary>
    /// <remarks>
    /// Guid (Globally Unique Identifier):
    /// - 128 bits de longitud
    /// - Probabilidad de colisión: ~0 (prácticamente imposible)
    /// - Formato: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
    /// </remarks>
    public Guid Id { get; set; }

    /// <summary>
    /// Fecha y hora de creación del registro (UTC).
    /// </summary>
    /// <remarks>
    /// IMPORTANTE: Siempre usar UTC para evitar problemas con zonas horarias.
    /// - UTC (Coordinated Universal Time): estándar global
    /// - No se ve afectado por cambios de horario de verano
    /// - Facilita operaciones con usuarios en diferentes zonas horarias
    ///
    /// Convertir a hora local en presentación:
    /// var localTime = utcTime.ToLocalTime();
    /// </remarks>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Fecha y hora de última actualización del registro (UTC).
    /// </summary>
    /// <remarks>
    /// Se actualiza automáticamente en cada modificación del registro.
    /// Ver: ApplicationDbContext.SaveChangesAsync() para implementación automática.
    /// </remarks>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Indica si el registro ha sido eliminado lógicamente (soft delete).
    /// </summary>
    /// <remarks>
    /// Soft Delete vs Hard Delete:
    ///
    /// SOFT DELETE (IsDeleted = true):
    /// ✅ Mantiene integridad referencial
    /// ✅ Permite auditoría completa
    /// ✅ Posibilidad de recuperación
    /// ✅ Cumplimiento legal (GDPR, etc.)
    /// ❌ Queries más complejos (WHERE IsDeleted = false)
    /// ❌ Uso de espacio en DB
    ///
    /// HARD DELETE (DELETE FROM tabla):
    /// ✅ Limpieza real de datos
    /// ✅ Queries simples
    /// ❌ Pérdida permanente de datos
    /// ❌ Puede romper integridad referencial
    /// ❌ Sin posibilidad de auditoría
    ///
    /// En este proyecto usamos SOFT DELETE por defecto.
    /// Implementamos Global Query Filter en EF Core para excluir automáticamente
    /// los registros con IsDeleted = true de todas las queries.
    /// </remarks>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Fecha y hora de eliminación lógica (UTC). Null si no está eliminado.
    /// </summary>
    /// <remarks>
    /// Nullable (DateTime?) porque:
    /// - Solo tiene valor si IsDeleted = true
    /// - null = no eliminado
    /// - Valor = timestamp de eliminación
    ///
    /// Usos:
    /// 1. Auditoría: saber cuándo se eliminó
    /// 2. Políticas de retención: eliminar definitivamente después de X días
    /// 3. Reportes: análisis de eliminaciones
    /// 4. Recuperación: validar si es muy reciente para restaurar
    /// </remarks>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Marca el registro como eliminado lógicamente.
    /// </summary>
    /// <remarks>
    /// Este método encapsula la lógica de soft delete para mantener consistencia.
    /// Siempre usar este método en lugar de setear las propiedades manualmente.
    ///
    /// Ejemplo de uso:
    /// var task = await _context.Tasks.FindAsync(id);
    /// task.Delete(); // Marca como eliminado
    /// await _context.SaveChangesAsync(); // Persiste el cambio
    /// </remarks>
    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow; // También actualizamos el timestamp de modificación
    }

    /// <summary>
    /// Restaura un registro previamente eliminado.
    /// </summary>
    /// <remarks>
    /// Permite recuperar datos eliminados accidentalmente.
    /// Útil para funcionalidad de "Papelera de reciclaje".
    ///
    /// Ejemplo de uso:
    /// var task = await _context.Tasks
    ///     .IgnoreQueryFilters() // Importante: incluir los eliminados
    ///     .FirstOrDefaultAsync(t => t.Id == id);
    /// task.Restore(); // Restaura el registro
    /// await _context.SaveChangesAsync();
    /// </remarks>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
