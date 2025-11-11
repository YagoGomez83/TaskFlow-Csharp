/**
 * EditTaskPage Component - Edit existing task page
 *
 * EXPLICACIÓN DE LA PÁGINA DE EDITAR TAREA:
 *
 * Esta página permite a usuarios editar una tarea existente.
 *
 * CARACTERÍSTICAS:
 * ✅ Usa TaskForm component en modo Edit
 * ✅ Carga datos de tarea desde backend
 * ✅ Permite cambiar estado (Pending → InProgress → Completed)
 * ✅ Integración con backend via taskService
 * ✅ Navegación automática después de actualizar
 * ✅ Manejo de errores del backend
 * ✅ Loading states (fetch + submit)
 * ✅ Botón cancelar → volver a lista
 *
 * FLUJO DE EDICIÓN:
 *
 * 1. Página monta → Extraer taskId de URL
 * 2. Fetch tarea del backend:
 *    GET /api/tasks/{taskId}
 * 3. Mostrar TaskForm con initialData (tarea cargada)
 * 4. Usuario modifica campos
 * 5. Submit → handleUpdateTask() ejecuta
 * 6. taskService.updateTask(taskId, data) → PUT /api/tasks/{taskId}
 * 7. Backend:
 *    - Valida datos con FluentValidation
 *    - Verifica ownership (userId del JWT == userId de la tarea)
 *    - Actualiza tarea en DB
 *    - Retorna TaskDto actualizado
 * 8. Si success → Navegar a /tasks
 * 9. Si error → Mostrar error del backend
 *
 * INTEGRACIÓN CON REACT QUERY:
 *
 * En producción:
 *
 * // Fetch tarea
 * const { data: task, isLoading } = useQuery(
 *   ['task', taskId],
 *   () => taskService.getTaskById(taskId)
 * );
 *
 * // Update mutation
 * const updateMutation = useMutation(
 *   (data) => taskService.updateTask(taskId, data),
 *   {
 *     onSuccess: () => {
 *       queryClient.invalidateQueries(['tasks']);
 *       queryClient.invalidateQueries(['task', taskId]);
 *       navigate('/tasks');
 *     }
 *   }
 * );
 *
 * OBTENER taskId DE URL:
 *
 * Con React Router:
 *
 * // En App.tsx:
 * <Route path="/tasks/:taskId/edit" element={<EditTaskPage />} />
 *
 * // En EditTaskPage:
 * const { taskId } = useParams();
 */

import { useState, useEffect } from 'react';
import type { TaskDto, UpdateTaskRequest } from '../types';
import TaskForm from '../components/common/TaskForm';
import Spinner from '../components/common/Spinner';
import Alert from '../components/common/Alert';
import './EditTaskPage.css';

/**
 * EditTaskPage component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Este es un "page component" que maneja dos loading states:
 * 1. Loading tarea del backend (fetch)
 * 2. Loading durante actualización (submit)
 *
 * ESTADOS:
 * - isLoading: Cargando datos de tarea
 * - isSubmitting: Enviando actualización
 * - task: Datos de la tarea
 * - error: Error del fetch o submit
 *
 * RESPONSABILIDADES:
 * - Fetch tarea por ID
 * - Pasar datos a TaskForm como initialData
 * - Manejar submit (actualización)
 * - Navegación después de success
 * - Manejo de errores
 *
 * @example
 * // En App.tsx con React Router:
 * <Route path="/tasks/:taskId/edit" element={<EditTaskPage />} />
 */
export function EditTaskPage() {
  /**
   * Task ID desde URL.
   *
   * En producción con React Router:
   * const { taskId } = useParams();
   *
   * Por ahora, extraemos manualmente de window.location:
   */
  const getTaskIdFromUrl = (): string | null => {
    const path = window.location.pathname;
    const match = path.match(/\/tasks\/([^/]+)\/edit/);
    return match ? match[1] : null;
  };

  const taskId = getTaskIdFromUrl();

  /**
   * Estado de loading durante fetch de tarea.
   */
  const [isLoading, setIsLoading] = useState(true);

  /**
   * Estado de loading durante actualización.
   */
  const [isSubmitting, setIsSubmitting] = useState(false);

  /**
   * Datos de la tarea.
   */
  const [task] = useState<TaskDto | null>(null);

  /**
   * Error del fetch o submit.
   */
  const [error, setError] = useState<string | null>(null);

  /**
   * Fetch tarea al montar componente.
   *
   * EXPLICACIÓN:
   *
   * useEffect con [] como dependency array:
   * - Ejecuta una vez al montar
   * - Equivalente a componentDidMount
   *
   * En producción:
   * const { data, isLoading, error } = useQuery(...);
   */
  useEffect(() => {
    const fetchTask = async () => {
      if (!taskId) {
        setError('ID de tarea inválido');
        setIsLoading(false);
        return;
      }

      try {
        // TODO: Implementar con React Query
        // const taskData = await taskService.getTaskById(taskId);
        // setTask(taskData);

        console.log('Fetch tarea:', taskId);

        // Simular delay de red
        await new Promise((resolve) => setTimeout(resolve, 1000));

        // Por ahora, mostrar error porque no hay datos reales
        setError('Esta página requiere React Router y datos del backend');
      } catch (err: any) {
        setError(err.message || 'Error al cargar la tarea');
      } finally {
        setIsLoading(false);
      }
    };

    fetchTask();
  }, [taskId]);

  /**
   * Handler para submit del formulario.
   */
  const handleUpdateTask = async (data: UpdateTaskRequest | any) => {
    if (!taskId) return;

    setIsSubmitting(true);
    setError(null);

    try {
      // TODO: Implementar con React Query mutation
      // const updatedTask = await taskService.updateTask(taskId, data);
      console.log('Actualizar tarea:', taskId, data);

      // Simular delay de red
      await new Promise((resolve) => setTimeout(resolve, 1000));

      // Navegar a lista de tareas
      window.location.href = '/tasks';
    } catch (err: any) {
      setError(err.message || 'Error al actualizar la tarea');
    } finally {
      setIsSubmitting(false);
    }
  };

  /**
   * Handler para cancelar.
   */
  const handleCancel = () => {
    window.location.href = '/tasks';
  };

  /**
   * Estado: Loading.
   *
   * Mientras carga la tarea, mostrar spinner.
   */
  if (isLoading) {
    return (
      <div className="edit-task-page">
        <Spinner overlay label="Cargando tarea..." />
      </div>
    );
  }

  /**
   * Estado: Error sin tarea.
   *
   * Si hubo error al cargar, mostrar alert.
   */
  if (error && !task) {
    return (
      <div className="edit-task-page">
        <div className="edit-task-page__container">
          <Alert variant="error" title="Error al cargar tarea">
            {error}
          </Alert>
          <button
            onClick={() => (window.location.href = '/tasks')}
            className="edit-task-page__back-button"
          >
            ← Volver a tareas
          </button>
        </div>
      </div>
    );
  }

  /**
   * Estado: Success (tarea cargada).
   *
   * Mostrar formulario con datos de la tarea.
   */
  return (
    <div className="edit-task-page">
      <div className="edit-task-page__container">
        {/* Header */}
        <div className="edit-task-page__header">
          <div className="edit-task-page__breadcrumb">
            <a href="/tasks" className="edit-task-page__breadcrumb-link">
              Tareas
            </a>
            <span className="edit-task-page__breadcrumb-separator">→</span>
            <span className="edit-task-page__breadcrumb-current">
              Editar Tarea
            </span>
          </div>
          <h1 className="edit-task-page__title">Editar Tarea</h1>
          <p className="edit-task-page__subtitle">
            Modifica los campos que desees actualizar
          </p>
        </div>

        {/* Formulario */}
        <TaskForm
          initialData={task}
          onSubmit={handleUpdateTask}
          onCancel={handleCancel}
          isSubmitting={isSubmitting}
          error={error}
        />
      </div>
    </div>
  );
}

export default EditTaskPage;
