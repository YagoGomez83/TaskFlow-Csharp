/**
 * TaskListPage Component - Main task management page
 *
 * EXPLICACIÓN DE LA PÁGINA DE TAREAS:
 *
 * Esta es la página principal para gestionar tareas.
 * Muestra lista completa con filtros y paginación.
 *
 * CARACTERÍSTICAS:
 * ✅ Lista de tareas con TaskList component
 * ✅ Filtros por estado y prioridad
 * ✅ Paginación
 * ✅ Botón crear nueva tarea
 * ✅ Acciones: editar, eliminar, cambiar estado
 * ✅ Integración con backend via services
 *
 * DATOS Y ESTADO:
 *
 * En producción, esta página usará React Query:
 *
 * const { data, isLoading, error } = useQuery(
 *   ['tasks', page, filters],
 *   () => taskService.getTasks({ page, pageSize: 10, ...filters })
 * );
 *
 * const deleteMutation = useMutation(taskService.deleteTask);
 * const updateMutation = useMutation(taskService.updateTask);
 *
 * Esta versión usa estado local con datos dummy para demostración.
 *
 * FILTROS:
 *
 * Usuario puede filtrar por:
 * - Estado: Todas, Pendiente, En Progreso, Completada
 * - Prioridad: Todas, Baja, Media, Alta
 *
 * Filtros se aplican en backend:
 * GET /api/tasks?page=1&pageSize=10&status=Pending&priority=High
 *
 * PAGINACIÓN:
 *
 * Backend retorna PaginatedList<TaskDto>:
 * {
 *   items: TaskDto[],
 *   pageNumber: 1,
 *   pageSize: 10,
 *   totalCount: 45,
 *   hasNextPage: true,
 *   hasPreviousPage: false
 * }
 */

import { useState } from 'react';
import type { TaskDto, TaskStatus, TaskPriority, PaginatedList } from '../types';
import TaskList from '../components/common/TaskList';
import Button from '../components/common/Button';
import './TaskListPage.css';

/**
 * Estado de filtros.
 */
interface Filters {
  status?: TaskStatus;
  priority?: TaskPriority;
}

/**
 * TaskListPage component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Este es un "page component" complejo:
 * - Coordina múltiples componentes
 * - Maneja estado de filtros y paginación
 * - Conecta con servicios de backend
 * - Maneja navegación
 *
 * ESTRUCTURA:
 * - Header con título y botón "Nueva Tarea"
 * - Barra de filtros (status, priority)
 * - TaskList component con tareas
 * - Paginación (manejada por TaskList)
 *
 * @example
 * // En App.tsx con React Router:
 * <Route path="/tasks" element={<TaskListPage />} />
 */
export function TaskListPage() {
  /**
   * Estado de paginación.
   */
  const [, setPage] = useState(1);

  /**
   * Estado de filtros.
   */
  const [filters, setFilters] = useState<Filters>({});

  /**
   * Estado de loading.
   *
   * En producción, vendría de React Query:
   * const { isLoading } = useQuery(...);
   */
  const [isLoading] = useState(false);

  /**
   * Estado de error.
   */
  const [error] = useState<string | null>(null);

  /**
   * Datos de tareas (dummy data).
   *
   * En producción:
   * const { data: tasks } = useQuery(...);
   */
  const tasks: PaginatedList<TaskDto> | undefined = undefined;

  /**
   * ID de tarea siendo eliminada.
   *
   * En producción:
   * const deleteMutation = useMutation(taskService.deleteTask);
   * const deletingTaskId = deleteMutation.variables?.id;
   */
  const [deletingTaskId] = useState<string | null>(null);

  /**
   * Handler para cambiar filtro de estado.
   */
  const handleStatusFilter = (status?: TaskStatus) => {
    setFilters((prev) => ({ ...prev, status }));
    setPage(1); // Reset a página 1 al cambiar filtro
  };

  /**
   * Handler para cambiar filtro de prioridad.
   */
  const handlePriorityFilter = (priority?: TaskPriority) => {
    setFilters((prev) => ({ ...prev, priority }));
    setPage(1);
  };

  /**
   * Handler para editar tarea.
   *
   * En producción con React Router:
   * const navigate = useNavigate();
   * navigate(`/tasks/${task.id}/edit`);
   */
  const handleEdit = (task: TaskDto) => {
    console.log('Editar tarea:', task.id);
    window.location.href = `/tasks/${task.id}/edit`;
  };

  /**
   * Handler para eliminar tarea.
   *
   * En producción:
   * const deleteMutation = useMutation(taskService.deleteTask, {
   *   onSuccess: () => {
   *     queryClient.invalidateQueries(['tasks']);
   *   }
   * });
   * deleteMutation.mutate(taskId);
   */
  const handleDelete = (taskId: string) => {
    console.log('Eliminar tarea:', taskId);
    // TODO: Implementar con React Query mutation
  };

  /**
   * Handler para cambiar estado de tarea.
   *
   * En producción:
   * const updateMutation = useMutation(
   *   ({ id, status }) => taskService.updateTask(id, { ...task, status }),
   *   { onSuccess: () => queryClient.invalidateQueries(['tasks']) }
   * );
   */
  const handleStatusChange = (taskId: string, newStatus: TaskStatus) => {
    console.log('Cambiar estado:', taskId, newStatus);
    // TODO: Implementar con React Query mutation
  };

  /**
   * Handler para cambiar página.
   */
  const handlePageChange = (newPage: number) => {
    setPage(newPage);
    // React Query refetch automáticamente cuando cambia 'page' dependency
  };

  /**
   * Navegar a crear tarea.
   */
  const handleCreateTask = () => {
    window.location.href = '/tasks/create';
  };

  return (
    <div className="task-list-page">
      {/* Header */}
      <div className="task-list-page__header">
        <div>
          <h1 className="task-list-page__title">Mis Tareas</h1>
          <p className="task-list-page__subtitle">
            Gestiona y organiza todas tus tareas
          </p>
        </div>
        <Button variant="primary" size="lg" onClick={handleCreateTask}>
          + Nueva Tarea
        </Button>
      </div>

      {/* Filtros */}
      <div className="task-list-page__filters">
        {/* Filtro por estado */}
        <div className="task-list-page__filter-group">
          <label className="task-list-page__filter-label">Estado:</label>
          <div className="task-list-page__filter-buttons">
            <button
              className={`task-list-page__filter-button ${
                !filters.status ? 'task-list-page__filter-button--active' : ''
              }`}
              onClick={() => handleStatusFilter(undefined)}
            >
              Todas
            </button>
            <button
              className={`task-list-page__filter-button ${
                filters.status === 'Pending'
                  ? 'task-list-page__filter-button--active'
                  : ''
              }`}
              onClick={() => handleStatusFilter('Pending' as TaskStatus)}
            >
              Pendiente
            </button>
            <button
              className={`task-list-page__filter-button ${
                filters.status === 'InProgress'
                  ? 'task-list-page__filter-button--active'
                  : ''
              }`}
              onClick={() => handleStatusFilter('InProgress' as TaskStatus)}
            >
              En Progreso
            </button>
            <button
              className={`task-list-page__filter-button ${
                filters.status === 'Completed'
                  ? 'task-list-page__filter-button--active'
                  : ''
              }`}
              onClick={() => handleStatusFilter('Completed' as TaskStatus)}
            >
              Completada
            </button>
          </div>
        </div>

        {/* Filtro por prioridad */}
        <div className="task-list-page__filter-group">
          <label className="task-list-page__filter-label">Prioridad:</label>
          <div className="task-list-page__filter-buttons">
            <button
              className={`task-list-page__filter-button ${
                !filters.priority ? 'task-list-page__filter-button--active' : ''
              }`}
              onClick={() => handlePriorityFilter(undefined)}
            >
              Todas
            </button>
            <button
              className={`task-list-page__filter-button ${
                filters.priority === 'Low'
                  ? 'task-list-page__filter-button--active'
                  : ''
              }`}
              onClick={() => handlePriorityFilter('Low' as TaskPriority)}
            >
              Baja
            </button>
            <button
              className={`task-list-page__filter-button ${
                filters.priority === 'Medium'
                  ? 'task-list-page__filter-button--active'
                  : ''
              }`}
              onClick={() => handlePriorityFilter('Medium' as TaskPriority)}
            >
              Media
            </button>
            <button
              className={`task-list-page__filter-button ${
                filters.priority === 'High'
                  ? 'task-list-page__filter-button--active'
                  : ''
              }`}
              onClick={() => handlePriorityFilter('High' as TaskPriority)}
            >
              Alta
            </button>
          </div>
        </div>
      </div>

      {/* Lista de tareas */}
      <TaskList
        tasks={tasks}
        isLoading={isLoading}
        error={error}
        onEdit={handleEdit}
        onDelete={handleDelete}
        onStatusChange={handleStatusChange}
        onPageChange={handlePageChange}
        deletingTaskId={deletingTaskId}
      />
    </div>
  );
}

export default TaskListPage;
