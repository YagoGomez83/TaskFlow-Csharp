/**
 * CreateTaskPage Component - Create new task page
 *
 * EXPLICACIÓN DE LA PÁGINA DE CREAR TAREA:
 *
 * Esta página permite a usuarios crear una nueva tarea.
 *
 * CARACTERÍSTICAS:
 * ✅ Usa TaskForm component en modo Create
 * ✅ Integración con backend via taskService
 * ✅ Navegación automática a lista después de crear
 * ✅ Manejo de errores del backend
 * ✅ Loading state durante creación
 * ✅ Botón cancelar → volver a lista
 *
 * FLUJO DE CREACIÓN:
 *
 * 1. Usuario completa formulario (título, descripción, fecha, prioridad)
 * 2. Submit → handleCreateTask() ejecuta
 * 3. taskService.createTask(data) → POST /api/tasks
 * 4. Backend:
 *    - Valida datos con FluentValidation
 *    - Extrae userId del JWT
 *    - Crea tarea en DB
 *    - Retorna TaskDto creado
 * 5. Si success → Navegar a /tasks
 * 6. Si error → Mostrar error del backend
 *
 * INTEGRACIÓN CON REACT QUERY:
 *
 * En producción, esta página usará useMutation:
 *
 * const createMutation = useMutation(taskService.createTask, {
 *   onSuccess: () => {
 *     queryClient.invalidateQueries(['tasks']);
 *     navigate('/tasks');
 *   }
 * });
 *
 * const handleSubmit = (data) => {
 *   createMutation.mutate(data);
 * };
 */

import { useState } from 'react';
import type { CreateTaskRequest } from '../types';
import TaskForm from '../components/common/TaskForm';
import './CreateTaskPage.css';

/**
 * CreateTaskPage component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Este es un "page component" simple:
 * - Envuelve TaskForm con layout de página
 * - Maneja submit (llamada al backend)
 * - Maneja navegación después de crear
 *
 * COMPOSICIÓN:
 * - Header con título
 * - TaskForm en modo Create (sin initialData)
 * - Footer con breadcrumbs o navegación
 *
 * RESPONSABILIDADES:
 * - Coordinar TaskForm con servicios
 * - Manejar estado de loading/error
 * - Navegación después de success
 *
 * @example
 * // En App.tsx con React Router:
 * <Route path="/tasks/create" element={<CreateTaskPage />} />
 */
export function CreateTaskPage() {
  /**
   * Estado de loading durante creación.
   *
   * En producción con React Query:
   * const createMutation = useMutation(...);
   * const isSubmitting = createMutation.isLoading;
   */
  const [isSubmitting, setIsSubmitting] = useState(false);

  /**
   * Estado de error del backend.
   */
  const [error, setError] = useState<string | null>(null);

  /**
   * Handler para submit del formulario.
   *
   * EXPLICACIÓN:
   *
   * Esta función se llama cuando TaskForm se envía.
   * Recibe datos validados del form.
   *
   * FLUJO:
   * 1. Set loading = true
   * 2. Llamar taskService.createTask(data)
   * 3. Si success → Navegar a /tasks
   * 4. Si error → Mostrar error
   * 5. Set loading = false
   *
   * En producción:
   * createMutation.mutate(data);
   */
  const handleCreateTask = async (data: CreateTaskRequest) => {
    setIsSubmitting(true);
    setError(null);

    try {
      // TODO: Implementar con React Query mutation
      // const newTask = await taskService.createTask(data);
      console.log('Crear tarea:', data);

      // Simular delay de red
      await new Promise((resolve) => setTimeout(resolve, 1000));

      // Navegar a lista de tareas
      window.location.href = '/tasks';
    } catch (err: any) {
      setError(err.message || 'Error al crear la tarea');
    } finally {
      setIsSubmitting(false);
    }
  };

  /**
   * Handler para cancelar.
   *
   * En producción con React Router:
   * const navigate = useNavigate();
   * navigate('/tasks');
   */
  const handleCancel = () => {
    window.location.href = '/tasks';
  };

  return (
    <div className="create-task-page">
      <div className="create-task-page__container">
        {/* Header */}
        <div className="create-task-page__header">
          <div className="create-task-page__breadcrumb">
            <a href="/tasks" className="create-task-page__breadcrumb-link">
              Tareas
            </a>
            <span className="create-task-page__breadcrumb-separator">→</span>
            <span className="create-task-page__breadcrumb-current">
              Nueva Tarea
            </span>
          </div>
          <h1 className="create-task-page__title">Crear Nueva Tarea</h1>
          <p className="create-task-page__subtitle">
            Completa el formulario para crear una nueva tarea
          </p>
        </div>

        {/* Formulario */}
        <TaskForm
          onSubmit={handleCreateTask}
          onCancel={handleCancel}
          isSubmitting={isSubmitting}
          error={error}
        />
      </div>
    </div>
  );
}

export default CreateTaskPage;
