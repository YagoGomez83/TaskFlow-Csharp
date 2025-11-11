/**
 * TaskCard Component - Display individual task with actions
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * TaskCard es un componente especializado para mostrar una tarea.
 *
 * CARACTERÍSTICAS:
 * ✅ Muestra todos los detalles de la tarea (título, descripción, fechas)
 * ✅ Badge visual para prioridad y estado
 * ✅ Acciones (editar, eliminar, cambiar estado)
 * ✅ Formato de fechas legible
 * ✅ Accesibilidad completa
 * ✅ Responsive design
 *
 * USO:
 * - En TaskList para mostrar lista de tareas
 * - En TaskDetail para vista individual
 * - Reutilizable en cualquier contexto
 */

import type { TaskDto } from '../../types';
import { TaskStatus, TaskPriority } from '../../types';
import Button from './Button';
import Card from './Card';
import './TaskCard.css';

/**
 * Props del TaskCard component.
 *
 * EXPLICACIÓN:
 * - task: Datos de la tarea a mostrar
 * - onEdit: Callback cuando usuario hace click en editar
 * - onDelete: Callback cuando usuario hace click en eliminar
 * - onStatusChange: Callback cuando usuario cambia el estado
 * - isDeleting: Flag para mostrar loading en botón delete
 */
export interface TaskCardProps {
  /**
   * Tarea a mostrar.
   */
  task: TaskDto;

  /**
   * Callback cuando se hace click en editar.
   * @param task - La tarea a editar
   */
  onEdit?: (task: TaskDto) => void;

  /**
   * Callback cuando se hace click en eliminar.
   * @param taskId - ID de la tarea a eliminar
   */
  onDelete?: (taskId: string) => void;

  /**
   * Callback cuando se cambia el estado.
   * @param taskId - ID de la tarea
   * @param newStatus - Nuevo estado
   */
  onStatusChange?: (taskId: string, newStatus: TaskStatus) => void;

  /**
   * Flag de loading durante operación de eliminación.
   * @default false
   */
  isDeleting?: boolean;
}

/**
 * TaskCard component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Este componente envuelve Card y muestra:
 * 1. Header con título y badges (prioridad, estado)
 * 2. Body con descripción y fechas
 * 3. Footer con botones de acción
 *
 * FORMATO DE FECHAS:
 *
 * Las fechas vienen del backend en formato ISO 8601 (UTC):
 * "2025-11-10T14:30:00Z"
 *
 * new Date(isoString) crea objeto Date en timezone local.
 * toLocaleDateString() formatea según configuración del navegador.
 *
 * BADGES:
 *
 * Priority badge:
 * - High: Rojo (urgente)
 * - Medium: Amarillo (normal)
 * - Low: Azul (baja prioridad)
 *
 * Status badge:
 * - Pending: Gris (sin empezar)
 * - InProgress: Azul (trabajando)
 * - Completed: Verde (terminado)
 *
 * ACCIONES:
 *
 * Quick status change:
 * - Pending → botón "Iniciar" (cambia a InProgress)
 * - InProgress → botón "Completar" (cambia a Completed)
 * - Completed → sin botón rápido
 *
 * Botones siempre disponibles:
 * - Editar (secondary)
 * - Eliminar (danger)
 *
 * @example
 * <TaskCard
 *   task={task}
 *   onEdit={(task) => navigate(`/tasks/${task.id}/edit`)}
 *   onDelete={(id) => deleteTaskMutation.mutate(id)}
 *   onStatusChange={(id, status) => updateStatusMutation.mutate({ id, status })}
 *   isDeleting={deleteTaskMutation.isLoading}
 * />
 */
export function TaskCard({
  task,
  onEdit,
  onDelete,
  onStatusChange,
  isDeleting = false,
}: TaskCardProps) {
  /**
   * Formatear fecha ISO a formato legible.
   *
   * EXPLICACIÓN:
   *
   * Backend envía fechas en ISO 8601: "2025-11-10T14:30:00Z"
   *
   * new Date() convierte a objeto Date del navegador.
   * toLocaleDateString() formatea según idioma del navegador:
   * - es-ES: "10/11/2025"
   * - en-US: "11/10/2025"
   * - Auto-detecta timezone del usuario
   *
   * Options:
   * - year: 'numeric' → "2025"
   * - month: 'short' → "Nov"
   * - day: 'numeric' → "10"
   * Resultado: "Nov 10, 2025"
   */
  const formatDate = (isoString: string): string => {
    const date = new Date(isoString);
    return date.toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  /**
   * Obtener clase CSS para badge de prioridad.
   *
   * EXPLICACIÓN:
   *
   * Template literals con clases dinámicas:
   * `task-card__badge--${priority.toLowerCase()}`
   *
   * High → task-card__badge--high (rojo)
   * Medium → task-card__badge--medium (amarillo)
   * Low → task-card__badge--low (azul)
   */
  const getPriorityBadgeClass = (priority: TaskPriority): string => {
    return `task-card__badge task-card__badge--${priority.toLowerCase()}`;
  };

  /**
   * Obtener clase CSS para badge de estado.
   */
  const getStatusBadgeClass = (status: TaskStatus): string => {
    return `task-card__badge task-card__badge--${status.toLowerCase()}`;
  };

  /**
   * Obtener label en español para estado.
   *
   * EXPLICACIÓN:
   *
   * Backend envía estados en inglés (enum).
   * Frontend muestra en español para UX.
   */
  const getStatusLabel = (status: TaskStatus): string => {
    const labels: Record<TaskStatus, string> = {
      [TaskStatus.Pending]: 'Pendiente',
      [TaskStatus.InProgress]: 'En Progreso',
      [TaskStatus.Completed]: 'Completada',
    };
    return labels[status];
  };

  /**
   * Obtener label en español para prioridad.
   */
  const getPriorityLabel = (priority: TaskPriority): string => {
    const labels: Record<TaskPriority, string> = {
      [TaskPriority.Low]: 'Baja',
      [TaskPriority.Medium]: 'Media',
      [TaskPriority.High]: 'Alta',
    };
    return labels[priority];
  };

  /**
   * Verificar si la fecha de vencimiento ya pasó.
   *
   * EXPLICACIÓN:
   *
   * new Date(dueDate) → Fecha de vencimiento
   * new Date() → Fecha actual
   * dueDate < now → Tarea vencida
   */
  const isOverdue = (dueDate: string | null): boolean => {
    if (!dueDate) return false;
    return new Date(dueDate) < new Date();
  };

  /**
   * Obtener próximo estado según flujo de trabajo.
   *
   * EXPLICACIÓN:
   *
   * Flujo: Pending → InProgress → Completed
   *
   * Botón quick action:
   * - Si Pending, botón "Iniciar" (va a InProgress)
   * - Si InProgress, botón "Completar" (va a Completed)
   * - Si Completed, sin botón (ya terminado)
   */
  const getNextStatus = (
    currentStatus: TaskStatus
  ): TaskStatus | null => {
    if (currentStatus === TaskStatus.Pending) {
      return TaskStatus.InProgress;
    }
    if (currentStatus === TaskStatus.InProgress) {
      return TaskStatus.Completed;
    }
    return null; // Completed, no hay siguiente
  };

  /**
   * Obtener label para botón de cambio de estado.
   */
  const getStatusChangeLabel = (currentStatus: TaskStatus): string => {
    if (currentStatus === TaskStatus.Pending) {
      return 'Iniciar';
    }
    if (currentStatus === TaskStatus.InProgress) {
      return 'Completar';
    }
    return '';
  };

  // Estado siguiente para botón quick action
  const nextStatus = getNextStatus(task.status);

  /**
   * Header del Card: Título + Badges.
   */
  const header = (
    <div className="task-card__header">
      <h3 className="task-card__title">{task.title}</h3>
      <div className="task-card__badges">
        <span className={getPriorityBadgeClass(task.priority)}>
          {getPriorityLabel(task.priority)}
        </span>
        <span className={getStatusBadgeClass(task.status)}>
          {getStatusLabel(task.status)}
        </span>
      </div>
    </div>
  );

  /**
   * Footer del Card: Botones de acción.
   */
  const footer = (
    <div className="task-card__actions">
      {/* Botón quick action (si hay siguiente estado) */}
      {nextStatus && onStatusChange && (
        <Button
          variant="primary"
          size="sm"
          onClick={() => onStatusChange(task.id, nextStatus)}
        >
          {getStatusChangeLabel(task.status)}
        </Button>
      )}

      {/* Botón editar */}
      {onEdit && (
        <Button
          variant="secondary"
          size="sm"
          onClick={() => onEdit(task)}
        >
          Editar
        </Button>
      )}

      {/* Botón eliminar */}
      {onDelete && (
        <Button
          variant="danger"
          size="sm"
          onClick={() => onDelete(task.id)}
          isLoading={isDeleting}
        >
          {isDeleting ? 'Eliminando...' : 'Eliminar'}
        </Button>
      )}
    </div>
  );

  return (
    <Card
      variant="elevated"
      header={header}
      footer={footer}
      padding="md"
    >
      {/* Descripción */}
      {task.description && (
        <p className="task-card__description">{task.description}</p>
      )}

      {/* Fechas */}
      <div className="task-card__dates">
        {/* Fecha de vencimiento */}
        {task.dueDate && (
          <div
            className={`task-card__due-date ${
              isOverdue(task.dueDate) && task.status !== TaskStatus.Completed
                ? 'task-card__due-date--overdue'
                : ''
            }`}
          >
            <span className="task-card__date-label">Vence:</span>{' '}
            <span className="task-card__date-value">
              {formatDate(task.dueDate)}
            </span>
            {isOverdue(task.dueDate) &&
              task.status !== TaskStatus.Completed && (
                <span className="task-card__overdue-indicator"> ⚠️ Vencida</span>
              )}
          </div>
        )}

        {/* Fecha de creación */}
        <div className="task-card__created-at">
          <span className="task-card__date-label">Creada:</span>{' '}
          <span className="task-card__date-value">
            {formatDate(task.createdAt)}
          </span>
        </div>
      </div>
    </Card>
  );
}

export default TaskCard;
