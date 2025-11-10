namespace TaskManagement.Domain.Enums;

/// <summary>
/// Estados posibles de una tarea en su ciclo de vida.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DEL CICLO DE VIDA DE UNA TAREA:
///
/// El estado de una tarea representa en qué punto del flujo de trabajo se encuentra.
/// Este diseño sigue el patrón de "Estado" (State Pattern) donde una entidad
/// puede transicionar entre diferentes estados con reglas específicas.
///
/// FLUJO DE ESTADOS:
///
///   [Pending] ──────────> [InProgress] ──────────> [Completed]
///      │                       │                         │
///      │                       │                         │
///      └───────────────────────┴─────────────────────────┘
///                    (Puede volver a cualquier estado)
///
/// TRANSICIONES VÁLIDAS:
/// - Pending → InProgress: Usuario comienza a trabajar en la tarea
/// - InProgress → Completed: Usuario termina la tarea
/// - Completed → Pending: Usuario reabre la tarea (ej: encontró un error)
/// - InProgress → Pending: Usuario pausa/pospone la tarea
/// - Cualquier estado → Cualquier estado (permitimos flexibilidad)
///
/// MÉTRICAS QUE PODEMOS CALCULAR:
/// - Tiempo en cada estado (para análisis de productividad)
/// - Tasa de completitud (Completed / Total)
/// - Tareas estancadas (InProgress por más de X días)
/// - Velocidad (tiempo promedio de Pending → Completed)
///
/// FUTURAS EXPANSIONES:
/// En sistemas más complejos, podrías agregar estados como:
/// - Blocked: Esperando algo externo
/// - Review: En proceso de revisión
/// - Approved: Aprobada por manager
/// - Rejected: Rechazada
/// - Cancelled: Cancelada (diferente de eliminada)
/// </remarks>
public enum TaskStatus
{
    /// <summary>
    /// Tarea pendiente, aún no iniciada.
    /// </summary>
    /// <remarks>
    /// Estado inicial de toda tarea recién creada.
    ///
    /// Características:
    /// - La tarea está en el backlog
    /// - No se ha comenzado a trabajar en ella
    /// - Puede tener fecha límite asignada
    /// - Puede ser priorizada
    ///
    /// Acciones disponibles:
    /// - Comenzar trabajo (cambiar a InProgress)
    /// - Editar detalles
    /// - Cambiar prioridad
    /// - Eliminar
    ///
    /// Métricas:
    /// - Tiempo en Pending = DueDate - CreatedAt (cuánto tiempo queda)
    /// - Si DueDate < Now y Status = Pending → Tarea atrasada
    /// </remarks>
    Pending = 0,

    /// <summary>
    /// Tarea en progreso, actualmente siendo trabajada.
    /// </summary>
    /// <remarks>
    /// Indica que alguien está trabajando activamente en la tarea.
    ///
    /// Características:
    /// - La tarea está siendo trabajada
    /// - No está completada aún
    /// - Puede tener progreso parcial
    ///
    /// Acciones disponibles:
    /// - Completar (cambiar a Completed)
    /// - Pausar (cambiar a Pending)
    /// - Editar detalles
    /// - Eliminar
    ///
    /// Métricas:
    /// - Work In Progress (WIP): cuántas tareas están InProgress simultáneamente
    /// - Tiempo en progreso: Now - (timestamp cuando cambió a InProgress)
    /// - Si está InProgress por más de X días → Posible bloqueo o problema
    ///
    /// Alertas:
    /// - Si DueDate está cerca y sigue InProgress → Notificar usuario
    /// - Si InProgress por más de N días → Alertar sobre posible estancamiento
    /// </remarks>
    InProgress = 1,

    /// <summary>
    /// Tarea completada exitosamente.
    /// </summary>
    /// <remarks>
    /// Estado final (normalmente) de una tarea.
    ///
    /// Características:
    /// - El trabajo está terminado
    /// - La tarea alcanzó su objetivo
    /// - Se completó dentro o fuera de la fecha límite
    ///
    /// Acciones disponibles:
    /// - Reabrir (cambiar a Pending o InProgress)
    /// - Archivar
    /// - Eliminar
    ///
    /// Métricas:
    /// - Tasa de completitud: % de tareas completed
    /// - Lead time: CreatedAt → CompletedAt (tiempo total)
    /// - Cycle time: InProgress start → CompletedAt (tiempo activo)
    /// - On-time rate: % completadas antes del DueDate
    ///
    /// Análisis:
    /// - Comparar DueDate vs CompletedAt:
    ///   * CompletedAt <= DueDate: ✅ A tiempo
    ///   * CompletedAt > DueDate: ⚠️ Atrasada
    /// - Identificar patrones: ¿Qué prioridades se completan más rápido?
    /// </remarks>
    Completed = 2
}
