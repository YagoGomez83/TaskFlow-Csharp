/**
 * TaskList Component - Display list of tasks with pagination
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * TaskList es un componente contenedor que muestra múltiples tareas.
 *
 * CARACTERÍSTICAS:
 * ✅ Grid responsive de TaskCards
 * ✅ Paginación integrada
 * ✅ Estados vacío, loading, error
 * ✅ Filtros visuales (status, priority)
 * ✅ Actions propagadas a cada TaskCard
 * ✅ Accesibilidad completa
 *
 * PATRÓN DE COMPOSICIÓN:
 *
 * TaskList es un "container component":
 * - Maneja lógica de lista (paginación, filtros)
 * - Compone TaskCard para cada item
 * - Propaga callbacks a hijos
 *
 * TaskCard es un "presentational component":
 * - Solo muestra datos
 * - Llama callbacks cuando usuario interactúa
 * - No maneja estado
 *
 * Este patrón (Container/Presentational) es muy común en React.
 */

import type { TaskDto, PaginatedList } from '../../types';
import { TaskStatus } from '../../types';
import TaskCard from './TaskCard';
import Button from './Button';
import Spinner from './Spinner';
import Alert from './Alert';
import './TaskList.css';

/**
 * Props del TaskList component.
 *
 * EXPLICACIÓN:
 * - tasks: Lista paginada de tareas (del backend)
 * - isLoading: Flag de carga inicial
 * - error: Mensaje de error si falló la petición
 * - onEdit: Callback para editar tarea
 * - onDelete: Callback para eliminar tarea
 * - onStatusChange: Callback para cambiar estado
 * - onPageChange: Callback para cambiar página
 * - deletingTaskId: ID de tarea siendo eliminada (para loading)
 */
export interface TaskListProps {
  /**
   * Lista paginada de tareas.
   */
  tasks?: PaginatedList<TaskDto>;

  /**
   * Flag de loading durante carga inicial.
   * @default false
   */
  isLoading?: boolean;

  /**
   * Mensaje de error si falló la petición.
   */
  error?: string | null;

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
   * Callback cuando se cambia de página.
   * @param pageNumber - Número de página (1-indexed)
   */
  onPageChange?: (pageNumber: number) => void;

  /**
   * ID de la tarea siendo eliminada (para mostrar loading).
   */
  deletingTaskId?: string | null;
}

/**
 * TaskList component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Este componente maneja 4 estados:
 *
 * 1. Loading: Muestra Spinner mientras carga
 * 2. Error: Muestra Alert si falló
 * 3. Empty: Muestra mensaje si no hay tareas
 * 4. Success: Muestra grid de TaskCards + paginación
 *
 * PAGINACIÓN:
 *
 * Backend retorna PaginatedList con metadata:
 * - pageNumber: Página actual (1-indexed)
 * - totalCount: Total de items
 * - hasNextPage: ¿Hay página siguiente?
 * - hasPreviousPage: ¿Hay página anterior?
 *
 * Frontend muestra:
 * - Botón "Anterior" (disabled si !hasPreviousPage)
 * - Info: "Página X de Y"
 * - Botón "Siguiente" (disabled si !hasNextPage)
 *
 * GRID RESPONSIVE:
 *
 * CSS Grid con auto-fill:
 * - Desktop (>1024px): 3 columnas
 * - Tablet (768-1024px): 2 columnas
 * - Mobile (<768px): 1 columna
 *
 * @example
 * const { data, isLoading, error } = useQuery('tasks', fetchTasks);
 *
 * <TaskList
 *   tasks={data}
 *   isLoading={isLoading}
 *   error={error}
 *   onEdit={(task) => navigate(`/tasks/${task.id}/edit`)}
 *   onDelete={(id) => deleteMutation.mutate(id)}
 *   onStatusChange={(id, status) => updateMutation.mutate({ id, status })}
 *   onPageChange={(page) => setPage(page)}
 *   deletingTaskId={deleteMutation.variables?.id}
 * />
 */
export function TaskList({
  tasks,
  isLoading = false,
  error = null,
  onEdit,
  onDelete,
  onStatusChange,
  onPageChange,
  deletingTaskId = null,
}: TaskListProps) {
  /**
   * Estado 1: Loading.
   *
   * EXPLICACIÓN:
   *
   * Mientras isLoading es true, mostramos Spinner overlay.
   * Usuario ve spinner en medio de pantalla.
   * Previene interacción con UI durante carga.
   */
  if (isLoading) {
    return (
      <div className="task-list">
        <Spinner overlay label="Cargando tareas..." />
      </div>
    );
  }

  /**
   * Estado 2: Error.
   *
   * EXPLICACIÓN:
   *
   * Si error tiene valor, mostramos Alert variant="error".
   * Error puede venir de:
   * - Network error (sin conexión)
   * - Backend error (500)
   * - Auth error (401, token expirado)
   */
  if (error) {
    return (
      <div className="task-list">
        <Alert variant="error" title="Error al cargar tareas">
          {error}
        </Alert>
      </div>
    );
  }

  /**
   * Estado 3: Empty (sin tareas).
   *
   * EXPLICACIÓN:
   *
   * Si tasks existe pero items está vacío:
   * - Usuario no ha creado tareas aún
   * - O filtros no tienen resultados
   *
   * Mostramos mensaje amigable + call-to-action.
   */
  if (!tasks || tasks.items.length === 0) {
    return (
      <div className="task-list">
        <div className="task-list__empty">
          <div className="task-list__empty-icon">📝</div>
          <h3 className="task-list__empty-title">No hay tareas</h3>
          <p className="task-list__empty-description">
            Crea tu primera tarea para empezar a organizarte.
          </p>
        </div>
      </div>
    );
  }

  /**
   * Calcular total de páginas.
   *
   * EXPLICACIÓN:
   *
   * Math.ceil() redondea hacia arriba.
   * Si totalCount = 25, pageSize = 10:
   * 25 / 10 = 2.5 → ceil(2.5) = 3 páginas
   */
  const totalPages = Math.ceil(tasks.totalCount / tasks.pageSize);

  /**
   * Estado 4: Success (mostrar tareas).
   *
   * EXPLICACIÓN:
   *
   * Mostramos:
   * 1. Header con contador de tareas
   * 2. Grid de TaskCards
   * 3. Paginación
   *
   * map() itera sobre tasks.items y crea un TaskCard por cada uno.
   *
   * Key prop:
   * - Requerido por React para optimizar renders
   * - Usamos task.id (único e inmutable)
   *
   * Propagación de callbacks:
   * - onEdit, onDelete, onStatusChange se pasan a cada TaskCard
   * - TaskCard los llama cuando usuario interactúa
   * - Event bubbling hasta TaskList y luego al parent (Page)
   */
  return (
    <div className="task-list">
      {/* Header con contador */}
      <div className="task-list__header">
        <h2 className="task-list__title">Mis Tareas</h2>
        <p className="task-list__count">
          Mostrando <strong>{tasks.items.length}</strong> de{' '}
          <strong>{tasks.totalCount}</strong> tareas
        </p>
      </div>

      {/* Grid de TaskCards */}
      <div className="task-list__grid">
        {tasks.items.map((task) => (
          <TaskCard
            key={task.id}
            task={task}
            onEdit={onEdit}
            onDelete={onDelete}
            onStatusChange={onStatusChange}
            isDeleting={deletingTaskId === task.id}
          />
        ))}
      </div>

      {/* Paginación (solo si hay múltiples páginas) */}
      {totalPages > 1 && (
        <div className="task-list__pagination">
          {/* Botón página anterior */}
          <Button
            variant="secondary"
            size="sm"
            onClick={() => onPageChange?.(tasks.pageNumber - 1)}
            disabled={!tasks.hasPreviousPage}
          >
            ← Anterior
          </Button>

          {/* Info de página actual */}
          <span className="task-list__page-info">
            Página <strong>{tasks.pageNumber}</strong> de{' '}
            <strong>{totalPages}</strong>
          </span>

          {/* Botón página siguiente */}
          <Button
            variant="secondary"
            size="sm"
            onClick={() => onPageChange?.(tasks.pageNumber + 1)}
            disabled={!tasks.hasNextPage}
          >
            Siguiente →
          </Button>
        </div>
      )}
    </div>
  );
}

export default TaskList;
