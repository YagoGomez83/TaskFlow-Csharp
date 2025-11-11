/**
 * TaskForm Component - Create/Edit task form with validation
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * TaskForm es un formulario reutilizable para crear o editar tareas.
 *
 * CARACTERÍSTICAS:
 * ✅ Modo Create o Edit (basado en initialData)
 * ✅ Validación de campos (required, maxLength)
 * ✅ Manejo de fechas (Date picker, formato ISO)
 * ✅ Dropdown para prioridad y estado
 * ✅ Mensajes de error
 * ✅ Loading state durante submit
 * ✅ Accesibilidad completa
 *
 * CONTROLLED COMPONENTS:
 *
 * Este form usa "controlled components":
 * - Cada input tiene value={state}
 * - Cada input tiene onChange que actualiza state
 * - React es "single source of truth"
 * - No usamos DOM refs (uncontrolled)
 *
 * BENEFICIOS:
 * - Validación en tiempo real
 * - Conditional logic fácil
 * - State predictible
 *
 * FORM STATE:
 *
 * useState() maneja estado del form:
 * - formData: Valores de todos los campos
 * - errors: Mensajes de error por campo
 * - Cada cambio actualiza state → re-render
 */

import { useState, useEffect } from 'react';
import type { FormEvent, ChangeEvent } from 'react';
import type {
  TaskDto,
  CreateTaskRequest,
  UpdateTaskRequest,
} from '../../types';
import { TaskPriority, TaskStatus } from '../../types';
import Input from './Input';
import Button from './Button';
import Card from './Card';
import Alert from './Alert';
import './TaskForm.css';

/**
 * Props del TaskForm component.
 *
 * EXPLICACIÓN:
 * - initialData: Datos de tarea a editar (null para modo create)
 * - onSubmit: Callback cuando se envía el form
 * - onCancel: Callback cuando se cancela
 * - isSubmitting: Flag de loading durante submit
 * - error: Mensaje de error del submit
 */
export interface TaskFormProps {
  /**
   * Datos iniciales de la tarea (para modo edit).
   * Si es null, el form está en modo create.
   */
  initialData?: TaskDto | null;

  /**
   * Callback cuando se envía el form válido.
   * @param data - Datos del form (CreateTaskRequest o UpdateTaskRequest)
   */
  onSubmit: (data: CreateTaskRequest | UpdateTaskRequest) => void;

  /**
   * Callback cuando se cancela el form.
   */
  onCancel?: () => void;

  /**
   * Flag de loading durante submit.
   * @default false
   */
  isSubmitting?: boolean;

  /**
   * Mensaje de error del submit (del backend).
   */
  error?: string | null;
}

/**
 * Tipo del estado del form.
 *
 * EXPLICACIÓN:
 *
 * Estado interno que maneja valores de todos los campos.
 * Separado de props para modificabilidad.
 */
interface FormData {
  title: string;
  description: string;
  dueDate: string;  // HTML date input usa "YYYY-MM-DD"
  priority: TaskPriority;
  status: TaskStatus;
}

/**
 * Tipo de errores de validación.
 *
 * EXPLICACIÓN:
 *
 * Record<keyof FormData, string | null>:
 * - Key es nombre de campo ('title', 'description', etc.)
 * - Value es mensaje de error (null si no hay error)
 *
 * Ej: { title: 'El título es requerido', description: null }
 */
type FormErrors = {
  [K in keyof FormData]?: string | null;
};

/**
 * TaskForm component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Este form maneja dos modos:
 *
 * 1. CREATE MODE (initialData = null):
 *    - Campos vacíos
 *    - Priority = Medium (default)
 *    - Status = Pending (auto-asignado)
 *    - Botón "Crear Tarea"
 *
 * 2. EDIT MODE (initialData = TaskDto):
 *    - Campos pre-poblados con datos existentes
 *    - Permite cambiar status
 *    - Botón "Actualizar Tarea"
 *
 * VALIDACIÓN:
 *
 * Frontend valida antes de enviar:
 * - title: Required, max 200 chars
 * - dueDate: Optional, debe ser futura
 * - priority: Required (dropdown)
 * - status: Required en edit mode
 *
 * Backend re-valida con FluentValidation (defense in depth).
 *
 * FLUJO DE SUBMIT:
 *
 * 1. Usuario click en "Crear/Actualizar"
 * 2. handleSubmit() ejecuta
 * 3. Validar campos (validateForm)
 * 4. Si hay errores, mostrar y detener
 * 5. Si válido, llamar onSubmit(formData)
 * 6. Parent maneja la petición API
 * 7. Si error, parent pasa error prop
 * 8. Si success, parent navega a lista
 *
 * @example
 * // Create mode
 * <TaskForm
 *   onSubmit={(data) => createMutation.mutate(data)}
 *   onCancel={() => navigate('/tasks')}
 *   isSubmitting={createMutation.isLoading}
 *   error={createMutation.error?.message}
 * />
 *
 * @example
 * // Edit mode
 * <TaskForm
 *   initialData={task}
 *   onSubmit={(data) => updateMutation.mutate({ id: task.id, ...data })}
 *   onCancel={() => navigate('/tasks')}
 *   isSubmitting={updateMutation.isLoading}
 *   error={updateMutation.error?.message}
 * />
 */
export function TaskForm({
  initialData = null,
  onSubmit,
  onCancel,
  isSubmitting = false,
  error = null,
}: TaskFormProps) {
  /**
   * Determinar modo del form.
   *
   * EXPLICACIÓN:
   *
   * Si initialData tiene valor → Edit mode
   * Si initialData es null → Create mode
   */
  const isEditMode = initialData !== null;

  /**
   * Estado del form.
   *
   * EXPLICACIÓN:
   *
   * useState() con valores iniciales:
   * - Create mode: Campos vacíos, priority = Medium
   * - Edit mode: Campos con datos de initialData
   *
   * formatDateForInput():
   * - Convierte ISO 8601 a "YYYY-MM-DD" para <input type="date">
   * - Ej: "2025-11-10T14:30:00Z" → "2025-11-10"
   */
  const [formData, setFormData] = useState<FormData>({
    title: initialData?.title || '',
    description: initialData?.description || '',
    dueDate: initialData?.dueDate ? formatDateForInput(initialData.dueDate) : '',
    priority: initialData?.priority || TaskPriority.Medium,
    status: initialData?.status || TaskStatus.Pending,
  });

  /**
   * Estado de errores de validación.
   *
   * EXPLICACIÓN:
   *
   * Inicialmente vacío (sin errores).
   * Se actualiza cuando usuario submit o cuando campo pierde focus.
   */
  const [errors, setErrors] = useState<FormErrors>({});

  /**
   * Efecto para resetear form cuando cambia initialData.
   *
   * EXPLICACIÓN:
   *
   * Si estamos editando tarea A y luego navegamos a editar tarea B,
   * initialData cambia → form debe actualizarse con nuevos datos.
   *
   * useEffect() se ejecuta cuando initialData cambia.
   *
   * Dependency array [initialData]:
   * - Si initialData cambia → ejecutar efecto
   * - Si no cambia → no ejecutar
   */
  useEffect(() => {
    if (initialData) {
      setFormData({
        title: initialData.title,
        description: initialData.description || '',
        dueDate: initialData.dueDate ? formatDateForInput(initialData.dueDate) : '',
        priority: initialData.priority,
        status: initialData.status,
      });
    }
  }, [initialData]);

  /**
   * Formatear fecha ISO 8601 a formato de input[type="date"].
   *
   * EXPLICACIÓN:
   *
   * Backend retorna: "2025-11-10T14:30:00Z" (ISO 8601 UTC)
   * Input date espera: "2025-11-10" (YYYY-MM-DD)
   *
   * split('T')[0] corta el string en 'T' y toma primera parte.
   */
  function formatDateForInput(isoString: string): string {
    return isoString.split('T')[0];
  }

  /**
   * Formatear fecha de input a ISO 8601 para API.
   *
   * EXPLICACIÓN:
   *
   * Input date retorna: "2025-11-10" (YYYY-MM-DD)
   * Backend espera: "2025-11-10T00:00:00Z" (ISO 8601 UTC)
   *
   * new Date(dateString) crea Date a medianoche local.
   * toISOString() convierte a UTC ISO 8601.
   */
  function formatDateForApi(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toISOString();
  }

  /**
   * Validar todo el form.
   *
   * EXPLICACIÓN:
   *
   * Retorna objeto con errores encontrados.
   * Si objeto está vacío → form válido.
   *
   * REGLAS:
   * - title: Required, max 200 chars
   * - dueDate: Si se especifica, debe ser futura
   * - priority: Siempre válido (dropdown)
   * - status: Siempre válido (dropdown)
   */
  function validateForm(): FormErrors {
    const newErrors: FormErrors = {};

    // Validar title
    if (!formData.title.trim()) {
      newErrors.title = 'El título es requerido';
    } else if (formData.title.length > 200) {
      newErrors.title = 'El título no puede superar los 200 caracteres';
    }

    // Validar dueDate (opcional, pero si se ingresa debe ser futura)
    if (formData.dueDate) {
      const dueDate = new Date(formData.dueDate);
      const today = new Date();
      today.setHours(0, 0, 0, 0); // Comparar solo fechas, no horas

      if (dueDate < today) {
        newErrors.dueDate = 'La fecha de vencimiento debe ser futura';
      }
    }

    return newErrors;
  }

  /**
   * Handler para cambios en inputs.
   *
   * EXPLICACIÓN:
   *
   * ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>:
   * - Evento de cambio puede venir de input, textarea, o select
   * - TypeScript unifica tipos
   *
   * e.target.name → nombre del campo ('title', 'priority', etc.)
   * e.target.value → valor nuevo
   *
   * setFormData(prev => ...):
   * - Functional update (recibe estado anterior)
   * - Spread operator {...prev} copia estado actual
   * - [name]: value sobrescribe campo específico
   *
   * Limpiar error:
   * - Cuando usuario escribe, limpiar error de ese campo
   * - UX: No mostrar error mientras está corrigiendo
   */
  function handleChange(
    e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
  ) {
    const { name, value } = e.target;

    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));

    // Limpiar error del campo cuando usuario empieza a corregir
    if (errors[name as keyof FormData]) {
      setErrors((prev) => ({
        ...prev,
        [name]: null,
      }));
    }
  }

  /**
   * Handler para submit del form.
   *
   * EXPLICACIÓN:
   *
   * e.preventDefault():
   * - Previene comportamiento default del <form>
   * - Default: Recargar página con datos en query string
   * - Queremos manejar submit con JavaScript (SPA)
   *
   * FLUJO:
   * 1. Validar form
   * 2. Si hay errores, actualizar estado y detener
   * 3. Si válido, preparar payload según modo
   * 4. Llamar onSubmit() con payload
   * 5. Parent maneja petición API
   */
  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();

    // Validar
    const validationErrors = validateForm();

    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    // Preparar payload según modo
    const payload: CreateTaskRequest | UpdateTaskRequest = {
      title: formData.title.trim(),
      description: formData.description.trim() || undefined,
      dueDate: formData.dueDate ? formatDateForApi(formData.dueDate) : undefined,
      priority: formData.priority,
      ...(isEditMode && { status: formData.status }),
    };

    // Llamar callback
    onSubmit(payload);
  }

  return (
    <Card variant="elevated" padding="lg">
      <form onSubmit={handleSubmit} className="task-form">
        {/* Título del form */}
        <h2 className="task-form__title">
          {isEditMode ? 'Editar Tarea' : 'Nueva Tarea'}
        </h2>

        {/* Error del submit (del backend) */}
        {error && (
          <Alert variant="error" title="Error al guardar" dismissible>
            {error}
          </Alert>
        )}

        {/* Campo: Title */}
        <Input
          label="Título"
          name="title"
          type="text"
          value={formData.title}
          onChange={handleChange}
          error={errors.title || undefined}
          required
          fullWidth
          placeholder="Ej: Completar reporte mensual"
          disabled={isSubmitting}
        />

        {/* Campo: Description */}
        <div className="task-form__field">
          <label htmlFor="description" className="task-form__label">
            Descripción
            <span className="task-form__optional"> (opcional)</span>
          </label>
          <textarea
            id="description"
            name="description"
            value={formData.description}
            onChange={handleChange}
            className="task-form__textarea"
            rows={4}
            placeholder="Describe los detalles de la tarea..."
            disabled={isSubmitting}
          />
          {errors.description && (
            <span className="task-form__error">{errors.description}</span>
          )}
        </div>

        {/* Campo: Due Date */}
        <Input
          label="Fecha de vencimiento"
          name="dueDate"
          type="date"
          value={formData.dueDate}
          onChange={handleChange}
          error={errors.dueDate || undefined}
          helperText="Opcional - Selecciona una fecha futura"
          fullWidth
          disabled={isSubmitting}
        />

        {/* Campo: Priority */}
        <div className="task-form__field">
          <label htmlFor="priority" className="task-form__label">
            Prioridad <span className="task-form__required">*</span>
          </label>
          <select
            id="priority"
            name="priority"
            value={formData.priority}
            onChange={handleChange}
            className="task-form__select"
            required
            disabled={isSubmitting}
          >
            <option value={TaskPriority.Low}>Baja</option>
            <option value={TaskPriority.Medium}>Media</option>
            <option value={TaskPriority.High}>Alta</option>
          </select>
        </div>

        {/* Campo: Status (solo en edit mode) */}
        {isEditMode && (
          <div className="task-form__field">
            <label htmlFor="status" className="task-form__label">
              Estado <span className="task-form__required">*</span>
            </label>
            <select
              id="status"
              name="status"
              value={formData.status}
              onChange={handleChange}
              className="task-form__select"
              required
              disabled={isSubmitting}
            >
              <option value={TaskStatus.Pending}>Pendiente</option>
              <option value={TaskStatus.InProgress}>En Progreso</option>
              <option value={TaskStatus.Completed}>Completada</option>
            </select>
          </div>
        )}

        {/* Botones de acción */}
        <div className="task-form__actions">
          <Button
            type="submit"
            variant="primary"
            size="lg"
            isLoading={isSubmitting}
            fullWidth
          >
            {isSubmitting
              ? isEditMode
                ? 'Actualizando...'
                : 'Creando...'
              : isEditMode
              ? 'Actualizar Tarea'
              : 'Crear Tarea'}
          </Button>

          {onCancel && (
            <Button
              type="button"
              variant="secondary"
              size="lg"
              onClick={onCancel}
              disabled={isSubmitting}
              fullWidth
            >
              Cancelar
            </Button>
          )}
        </div>
      </form>
    </Card>
  );
}

export default TaskForm;
