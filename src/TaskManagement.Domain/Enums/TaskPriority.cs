namespace TaskManagement.Domain.Enums;

/// <summary>
/// Niveles de prioridad para las tareas.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE PRIORIZACIÓN:
///
/// La prioridad ayuda a determinar en qué orden deben trabajarse las tareas.
/// Es un concepto fundamental en gestión de proyectos y productividad personal.
///
/// MATRIZ DE EISENHOWER (método de priorización):
///
///   Urgente     │     No Urgente
///               │
///  ──────────────┼──────────────────
///               │
///  Importante   │ High Priority   │ Medium Priority
///               │ (Hacer YA)      │ (Planificar)
///  ──────────────┼──────────────────
///               │
///  No Important │ Low Priority    │ Low Priority
///               │ (Delegar)       │ (Eliminar)
///
/// CRITERIOS DE PRIORIZACIÓN:
///
/// 1. IMPACTO: ¿Qué tan importante es el resultado?
/// 2. URGENCIA: ¿Qué tan pronto debe completarse?
/// 3. ESFUERZO: ¿Cuánto tiempo/recursos requiere?
/// 4. DEPENDENCIAS: ¿Otras tareas dependen de esta?
/// 5. VALOR: ¿Cuánto valor aporta al objetivo final?
///
/// CÓMO DECIDIR LA PRIORIDAD:
///
/// HIGH (Alta):
/// - Bloqueante: Otras tareas dependen de esta
/// - Urgente + Importante: Fecha límite cercana y alto impacto
/// - Crítico para el negocio: Afecta operaciones o clientes
/// - Bug crítico: Afecta funcionalidad principal
///
/// MEDIUM (Media):
/// - Importante pero no urgente: Puede planificarse
/// - Impacto moderado: Mejora pero no crítica
/// - Fecha límite lejana: Tiempo para trabajar sin presión
///
/// LOW (Baja):
/// - Nice to have: Deseable pero no necesario
/// - Impacto menor: Cambios cosméticos o mejoras pequeñas
/// - Sin fecha límite: Puede hacerse "cuando haya tiempo"
/// - Tareas de aprendizaje/exploración
///
/// ORDENAMIENTO EN QUERIES:
/// Por defecto, las tareas se ordenan:
/// 1. Por prioridad: High → Medium → Low
/// 2. Por fecha límite: Más cercanas primero
/// 3. Por fecha creación: Más antiguas primero (FIFO)
///
/// ORDER BY Priority DESC, DueDate ASC, CreatedAt ASC
/// </remarks>
public enum TaskPriority
{
    /// <summary>
    /// Prioridad baja - Puede hacerse después.
    /// </summary>
    /// <remarks>
    /// Características de tareas Low Priority:
    /// - No bloquean otras tareas
    /// - Impacto menor en objetivos
    /// - Sin fecha límite urgente o sin fecha límite
    /// - Pueden posponerse sin consecuencias graves
    ///
    /// Ejemplos:
    /// - "Reorganizar archivos del proyecto"
    /// - "Investigar nueva librería para evaluar"
    /// - "Actualizar documentación interna"
    /// - "Refactoring de código no crítico"
    ///
    /// Gestión:
    /// - Trabajar en tiempo libre
    /// - Agrupar varias tareas Low para hacer en batch
    /// - Evaluar periódicamente si sigue siendo relevante o puede eliminarse
    /// - Si lleva mucho tiempo sin trabajarse, considerar eliminar (no es prioritario)
    ///
    /// Valor numérico: 0
    /// Esto permite ordenamiento: ORDER BY Priority DESC pondrá Low al final
    /// </remarks>
    Low = 0,

    /// <summary>
    /// Prioridad media - Debe hacerse pronto.
    /// </summary>
    /// <remarks>
    /// Características de tareas Medium Priority:
    /// - Importante pero no urgente
    /// - Impacto moderado en objetivos
    /// - Fecha límite razonable (no inmediata)
    /// - Debe planificarse en el cronograma
    ///
    /// Ejemplos:
    /// - "Implementar nueva feature solicitada"
    /// - "Optimizar query que es lento"
    /// - "Escribir tests para módulo existente"
    /// - "Actualizar dependencias a versiones recientes"
    ///
    /// Gestión:
    /// - Planificar en sprint actual o próximo
    /// - Asignar tiempo específico en la semana
    /// - Monitorear progreso regularmente
    /// - Si se acerca la fecha límite, escalar a High
    ///
    /// Valor numérico: 1
    /// Prioridad intermedia en ordenamiento
    /// </remarks>
    Medium = 1,

    /// <summary>
    /// Prioridad alta - Debe hacerse ya, urgente.
    /// </summary>
    /// <remarks>
    /// Características de tareas High Priority:
    /// - Urgente E importante (Matriz de Eisenhower, cuadrante 1)
    /// - Bloquea otras tareas o personas
    /// - Fecha límite inmediata o ya pasada
    /// - Alto impacto en negocio/usuarios
    ///
    /// Ejemplos:
    /// - "Bug crítico en producción"
    /// - "Feature bloqueante para release"
    /// - "Tarea con deadline de cliente en 24h"
    /// - "Incidencia de seguridad a resolver"
    ///
    /// Gestión:
    /// - Trabajar INMEDIATAMENTE
    /// - Pausar tareas de menor prioridad
    /// - Comunicar a stakeholders el progreso
    /// - Si hay múltiples High, ordenar por impacto/urgencia
    ///
    /// Alertas:
    /// - Notificar al usuario cuando se crea/asigna una tarea High
    /// - Destacar visualmente en UI (color rojo, icono de urgencia)
    /// - Si permanece InProgress por mucho tiempo, alertar posible bloqueo
    ///
    /// Valor numérico: 2
    /// Máxima prioridad en ordenamiento: ORDER BY Priority DESC pondrá High primero
    /// </remarks>
    High = 2
}
