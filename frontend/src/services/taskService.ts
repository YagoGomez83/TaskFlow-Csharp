/**
 * Task Service - CRUD operations for tasks
 *
 * EXPLICACIÓN DEL SERVICIO DE TAREAS:
 *
 * Este servicio encapsula todas las operaciones CRUD (Create, Read, Update, Delete)
 * para la gestión de tareas:
 *
 * ✅ CREATE - Crear nueva tarea
 * ✅ READ   - Obtener tareas (lista paginada o individual)
 * ✅ UPDATE - Actualizar tarea existente
 * ✅ DELETE - Eliminar tarea (soft delete)
 *
 * VENTAJAS DE SEPARAR EN SERVICIO:
 *
 * 1. REUTILIZACIÓN:
 *    - Múltiples componentes pueden usar las mismas funciones
 *    - TaskList, TaskDetail, TaskForm todos usan este servicio
 *
 * 2. MANTENIBILIDAD:
 *    - Cambiar URLs o lógica en un solo lugar
 *    - Ej: Cambiar de REST a GraphQL solo aquí
 *
 * 3. TESTABILIDAD:
 *    - Fácil de testear en aislamiento
 *    - Mock del servicio para tests de componentes
 *
 * 4. TIPADO:
 *    - TypeScript garantiza tipos correctos
 *    - IntelliSense muestra parámetros disponibles
 *
 * PATRÓN DE DISEÑO:
 *
 * Este servicio sigue el patrón Repository en el frontend:
 * - Abstrae la fuente de datos (API REST en este caso)
 * - Componentes no saben si viene de API, localStorage, etc.
 * - Fácil cambiar implementación sin tocar componentes
 */

import api from './api';
import type {
  TaskDto,
  CreateTaskRequest,
  UpdateTaskRequest,
  PaginatedList,
  TaskQueryParams,
} from '../types';

// ============================================================================
// READ OPERATIONS (Query/Get)
// ============================================================================

/**
 * Obtiene una lista paginada de tareas del usuario autenticado.
 *
 * EXPLICACIÓN DEL FLUJO:
 *
 * 1. Frontend construye query params:
 *    - page: Número de página (1-indexed)
 *    - pageSize: Items por página (típicamente 10-20)
 *    - status: Filtro opcional (Pending, InProgress, Completed)
 *    - priority: Filtro opcional (Low, Medium, High)
 *
 * 2. Frontend hace request:
 *    GET /api/tasks?page=1&pageSize=20&status=Pending
 *
 * 3. Backend procesa:
 *    - Obtiene userId del JWT (Authorization header)
 *    - Filtra tareas: WHERE userId = @userId AND !isDeleted
 *    - Aplica filtros opcionales (status, priority)
 *    - Ordena por createdAt DESC (más recientes primero)
 *    - Pagina los resultados
 *
 * 4. Backend retorna PaginatedList:
 *    {
 *      items: [TaskDto, TaskDto, ...],
 *      pageNumber: 1,
 *      pageSize: 20,
 *      totalCount: 45,
 *      hasNextPage: true,
 *      hasPreviousPage: false
 *    }
 *
 * PAGINACIÓN:
 *
 * Paginación mejora performance y UX:
 * - No cargar 1000 tareas a la vez
 * - Cargar solo lo que se muestra en pantalla
 * - Usuario navega página por página
 *
 * Cálculo de total de páginas:
 * totalPages = Math.ceil(totalCount / pageSize)
 * Ej: 45 tareas / 20 por página = 3 páginas
 *
 * FILTROS:
 *
 * Filtros opcionales permiten al usuario encontrar tareas específicas:
 * - Ver solo tareas Pendientes para saber qué hacer
 * - Ver solo tareas Completadas para review
 * - Ver solo tareas de Alta prioridad para urgencias
 *
 * SEGURIDAD:
 *
 * - Backend automáticamente filtra por userId del JWT
 * - Usuario SOLO puede ver sus propias tareas
 * - Admin puede ver todas (validado en backend)
 *
 * @param params - Parámetros de paginación y filtros opcionales
 * @returns Promise con PaginatedList de tareas
 *
 * @throws ApiError si la request falla
 *
 * @example
 * // Obtener primera página de tareas pendientes
 * const tasks = await getTasks({
 *   page: 1,
 *   pageSize: 20,
 *   status: TaskStatus.Pending
 * });
 *
 * console.log(`Showing ${tasks.items.length} of ${tasks.totalCount} tasks`);
 * console.log('Has more pages:', tasks.hasNextPage);
 */
export async function getTasks(
  params: TaskQueryParams = { page: 1, pageSize: 20 }
): Promise<PaginatedList<TaskDto>> {
  // Construir query string desde params
  // URLSearchParams maneja encoding automáticamente
  const queryParams = new URLSearchParams({
    page: params.page.toString(),
    pageSize: params.pageSize.toString(),
  });

  // Agregar filtros opcionales solo si están presentes
  if (params.status) {
    queryParams.append('status', params.status);
  }

  if (params.priority) {
    queryParams.append('priority', params.priority);
  }

  // GET /api/tasks?page=1&pageSize=20&status=Pending
  const response = await api.get<PaginatedList<TaskDto>>(
    `/tasks?${queryParams.toString()}`
  );

  return response.data;
}

/**
 * Obtiene una tarea específica por su ID.
 *
 * EXPLICACIÓN DEL FLUJO:
 *
 * 1. Frontend envía request:
 *    GET /api/tasks/{id}
 *
 * 2. Backend valida:
 *    - Tarea existe
 *    - No está eliminada (isDeleted = false)
 *    - Usuario es owner O es Admin
 *
 * 3. Backend retorna TaskDto completo
 *
 * CASOS DE USO:
 *
 * - Ver detalles completos de una tarea
 * - Editar tarea (cargar datos actuales en formulario)
 * - Vista de detalle/modal de tarea
 *
 * VALIDACIÓN DE OWNERSHIP:
 *
 * Backend verifica que la tarea pertenece al usuario:
 * if (task.userId !== currentUser.userId && currentUser.role !== "Admin")
 *   return 403 Forbidden
 *
 * SOFT DELETE:
 *
 * Tareas eliminadas (isDeleted = true) no son retornadas.
 * Backend tiene global query filter: WHERE !isDeleted
 *
 * MANEJO DE ERRORES:
 *
 * - 404: Tarea no existe o está eliminada
 * - 403: Usuario no tiene permiso para ver esta tarea
 * - 401: No autenticado
 *
 * @param id - UUID de la tarea
 * @returns Promise con TaskDto
 *
 * @throws ApiError si la tarea no existe o no tienes permiso
 *
 * @example
 * const task = await getTaskById('123e4567-e89b-12d3-a456-426614174000');
 * console.log('Task title:', task.title);
 * console.log('Status:', task.status);
 */
export async function getTaskById(id: string): Promise<TaskDto> {
  // GET /api/tasks/{id}
  const response = await api.get<TaskDto>(`/tasks/${id}`);

  return response.data;
}

// ============================================================================
// CREATE OPERATION
// ============================================================================

/**
 * Crea una nueva tarea.
 *
 * EXPLICACIÓN DEL FLUJO:
 *
 * 1. Frontend construye CreateTaskRequest:
 *    {
 *      title: "Implementar login",
 *      description: "Crear página de login con validación",
 *      dueDate: "2025-12-31T23:59:59Z",  // ISO 8601 UTC
 *      priority: TaskPriority.High
 *    }
 *
 * 2. Frontend valida (antes de enviar):
 *    - title no vacío
 *    - dueDate es fecha futura (si se proporciona)
 *    - priority es válido
 *
 * 3. Frontend envía request:
 *    POST /api/tasks
 *    Body: CreateTaskRequest
 *
 * 4. Backend valida con FluentValidation:
 *    - title: required, max 200 chars
 *    - description: optional, max 2000 chars
 *    - dueDate: optional, debe ser futura
 *    - priority: required, enum válido
 *
 * 5. Backend crea tarea:
 *    - userId = currentUser.userId (del JWT)
 *    - status = Pending (inicial)
 *    - createdAt = DateTime.UtcNow
 *    - Guarda en DB
 *
 * 6. Backend retorna TaskDto completo:
 *    - Incluye ID generado
 *    - Incluye campos auto-generados (createdAt, updatedAt)
 *
 * VALORES POR DEFECTO:
 *
 * - status: Siempre Pending (backend lo asigna)
 * - userId: Del JWT (backend lo asigna)
 * - priority: Medium si no se especifica (backend)
 * - description: null si no se proporciona
 * - dueDate: null si no se proporciona
 *
 * FECHAS:
 *
 * IMPORTANTE: Enviar fechas en formato ISO 8601 UTC:
 * - Correcto: "2025-12-31T23:59:59Z"
 * - Incorrecto: "31/12/2025" o "2025-12-31"
 *
 * JavaScript Date a ISO string:
 * const dueDate = new Date('2025-12-31');
 * const isoString = dueDate.toISOString(); // "2025-12-31T00:00:00.000Z"
 *
 * VALIDACIÓN EN FRONTEND:
 *
 * Validar antes de enviar para mejor UX:
 * - Mostrar errores inmediatamente
 * - No hacer round-trip innecesario al backend
 * - Backend SIEMPRE valida de nuevo (defense in depth)
 *
 * @param taskData - Datos de la nueva tarea
 * @returns Promise con TaskDto de la tarea creada
 *
 * @throws ApiError si validación falla o error del servidor
 *
 * @example
 * const newTask = await createTask({
 *   title: 'Revisar PR #123',
 *   description: 'Revisar cambios en autenticación',
 *   dueDate: new Date('2025-11-15').toISOString(),
 *   priority: TaskPriority.High
 * });
 *
 * console.log('Created task with ID:', newTask.id);
 * navigate(`/tasks/${newTask.id}`);
 */
export async function createTask(taskData: CreateTaskRequest): Promise<TaskDto> {
  // POST /api/tasks
  const response = await api.post<TaskDto>('/tasks', taskData);

  return response.data;
}

// ============================================================================
// UPDATE OPERATION
// ============================================================================

/**
 * Actualiza una tarea existente.
 *
 * EXPLICACIÓN DEL FLUJO:
 *
 * 1. Frontend obtiene tarea actual:
 *    const task = await getTaskById(id);
 *
 * 2. Frontend muestra formulario pre-llenado con valores actuales
 *
 * 3. Usuario modifica campos (ej: cambiar status a InProgress)
 *
 * 4. Frontend construye UpdateTaskRequest:
 *    - Incluye TODOS los campos (PUT semántica)
 *    - Campos no modificados mantienen valor actual
 *    {
 *      title: task.title,              // Sin cambios
 *      description: task.description,  // Sin cambios
 *      dueDate: task.dueDate,         // Sin cambios
 *      priority: task.priority,       // Sin cambios
 *      status: TaskStatus.InProgress  // MODIFICADO
 *    }
 *
 * 5. Frontend envía request:
 *    PUT /api/tasks/{id}
 *    Body: UpdateTaskRequest
 *
 * 6. Backend valida:
 *    - Tarea existe
 *    - Usuario es owner O es Admin
 *    - Validación de campos (FluentValidation)
 *
 * 7. Backend actualiza tarea:
 *    - task.UpdateTitle(request.Title)
 *    - task.UpdateStatus(request.Status)
 *    - etc.
 *    - updatedAt = DateTime.UtcNow (auto)
 *
 * 8. Backend retorna TaskDto actualizado
 *
 * PUT vs PATCH:
 *
 * Este endpoint usa PUT (reemplazar completo):
 * - Debe enviar TODOS los campos
 * - Backend reemplaza toda la tarea
 * - Más simple de implementar
 *
 * Alternativa PATCH (update parcial):
 * - Enviar solo campos modificados
 * - Más complejo de implementar
 * - Útil para updates específicos (ej: solo cambiar status)
 *
 * VALIDACIÓN DE OWNERSHIP:
 *
 * Backend verifica que puedes modificar esta tarea:
 * if (task.userId !== currentUser.userId && currentUser.role !== "Admin")
 *   return 403 Forbidden
 *
 * Solo owner o Admin pueden actualizar una tarea.
 *
 * TRANSICIONES DE ESTADO:
 *
 * Backend permite cualquier transición de estado:
 * - Pending → InProgress (comenzar tarea)
 * - InProgress → Completed (terminar tarea)
 * - Completed → Pending (reabrir tarea)
 * - etc.
 *
 * Si necesitas restricciones, agrégalas en backend.
 *
 * MANEJO DE ERRORES:
 *
 * - 404: Tarea no existe
 * - 403: No tienes permiso para editar esta tarea
 * - 400: Validación fallida (ej: title vacío)
 *
 * @param id - UUID de la tarea a actualizar
 * @param taskData - Nuevos valores de la tarea
 * @returns Promise con TaskDto actualizado
 *
 * @throws ApiError si validación falla o no tienes permiso
 *
 * @example
 * // Cambiar status de tarea a completada
 * const updated = await updateTask(taskId, {
 *   ...currentTask,
 *   status: TaskStatus.Completed
 * });
 *
 * console.log('Task completed!');
 * showNotification('Task marked as completed');
 */
export async function updateTask(
  id: string,
  taskData: UpdateTaskRequest
): Promise<TaskDto> {
  // PUT /api/tasks/{id}
  const response = await api.put<TaskDto>(`/tasks/${id}`, taskData);

  return response.data;
}

// ============================================================================
// DELETE OPERATION
// ============================================================================

/**
 * Elimina una tarea (soft delete).
 *
 * EXPLICACIÓN DEL FLUJO:
 *
 * 1. Frontend pide confirmación al usuario:
 *    "¿Estás seguro de eliminar esta tarea?"
 *
 * 2. Usuario confirma
 *
 * 3. Frontend envía request:
 *    DELETE /api/tasks/{id}
 *
 * 4. Backend valida:
 *    - Tarea existe
 *    - Usuario es owner O es Admin
 *
 * 5. Backend hace SOFT DELETE:
 *    - task.MarkAsDeleted()
 *    - isDeleted = true
 *    - deletedAt = DateTime.UtcNow
 *    - La tarea NO se elimina de la DB
 *
 * 6. Backend retorna 204 No Content
 *
 * 7. Frontend actualiza UI:
 *    - Remueve tarea de la lista
 *    - Muestra notificación de éxito
 *
 * SOFT DELETE vs HARD DELETE:
 *
 * SOFT DELETE (implementado):
 * ✅ Tarea se marca como eliminada (isDeleted = true)
 * ✅ Permanece en DB para auditoría
 * ✅ Puede recuperarse si es necesario
 * ✅ No rompe relaciones en DB
 * ✅ Admin puede ver tareas eliminadas
 *
 * HARD DELETE (NO implementado):
 * ❌ Tarea se elimina permanentemente de DB
 * ❌ No se puede recuperar
 * ❌ Dificulta auditoría
 * ❌ Puede romper foreign keys
 *
 * FILTRO GLOBAL:
 *
 * Backend tiene global query filter en EF Core:
 * entity.HasQueryFilter(e => !e.IsDeleted)
 *
 * Todas las queries automáticamente filtran tareas eliminadas:
 * - getTasks() no retorna tareas eliminadas
 * - getTaskById() retorna 404 si está eliminada
 *
 * Admin puede desactivar el filtro para ver eliminadas:
 * context.Tasks.IgnoreQueryFilters()
 *
 * RECUPERAR TAREA ELIMINADA:
 *
 * No hay endpoint para recuperar, pero se podría agregar:
 *
 * POST /api/tasks/{id}/restore
 * - Valida que user es owner o Admin
 * - task.IsDeleted = false
 * - task.DeletedAt = null
 *
 * VALIDACIÓN DE OWNERSHIP:
 *
 * Solo owner o Admin pueden eliminar:
 * if (task.userId !== currentUser.userId && currentUser.role !== "Admin")
 *   return 403 Forbidden
 *
 * MANEJO DE ERRORES:
 *
 * - 404: Tarea no existe o ya está eliminada
 * - 403: No tienes permiso para eliminar esta tarea
 * - 401: No autenticado
 *
 * @param id - UUID de la tarea a eliminar
 * @returns Promise<void> - No retorna data (204 No Content)
 *
 * @throws ApiError si la tarea no existe o no tienes permiso
 *
 * @example
 * // Eliminar tarea con confirmación
 * const confirmDelete = window.confirm('¿Eliminar esta tarea?');
 * if (confirmDelete) {
 *   await deleteTask(taskId);
 *   showNotification('Task deleted successfully');
 *   navigate('/tasks');
 * }
 */
export async function deleteTask(id: string): Promise<void> {
  // DELETE /api/tasks/{id}
  // 204 No Content (no retorna data)
  await api.delete(`/tasks/${id}`);
}

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

/**
 * Convierte una fecha de JavaScript a ISO string para el backend.
 *
 * EXPLICACIÓN:
 *
 * Backend espera fechas en formato ISO 8601 UTC:
 * "2025-12-31T23:59:59.000Z"
 *
 * Esta función helper simplifica la conversión:
 * - Acepta Date object o null
 * - Retorna ISO string o undefined
 *
 * undefined vs null:
 * - undefined: Campo opcional no presente en JSON
 * - null: Campo explícitamente sin valor
 *
 * Backend acepta ambos para campos opcionales.
 *
 * @param date - Date object o null
 * @returns ISO string o undefined
 *
 * @example
 * const request: CreateTaskRequest = {
 *   title: 'My task',
 *   dueDate: formatDateForApi(selectedDate), // Convierte automáticamente
 *   priority: TaskPriority.Medium
 * };
 */
export function formatDateForApi(date: Date | null): string | undefined {
  if (!date) {
    return undefined;
  }

  return date.toISOString();
}

/**
 * Convierte un ISO string del backend a Date object.
 *
 * EXPLICACIÓN:
 *
 * Backend retorna fechas en formato ISO 8601:
 * "2025-12-31T23:59:59.000Z"
 *
 * Esta función convierte a Date para manipulación en JavaScript.
 *
 * ZONA HORARIA:
 *
 * - Backend siempre envía UTC (Z al final)
 * - Date object en JS es timezone-aware
 * - Mostrar en UI: date.toLocaleDateString() convierte a zona local
 *
 * @param isoString - ISO 8601 string o null
 * @returns Date object o null
 *
 * @example
 * const task = await getTaskById(id);
 * const dueDate = parseDateFromApi(task.dueDate);
 * if (dueDate) {
 *   console.log('Due:', dueDate.toLocaleDateString());
 * }
 */
export function parseDateFromApi(isoString: string | null): Date | null {
  if (!isoString) {
    return null;
  }

  return new Date(isoString);
}

// ============================================================================
// EXPORTS
// ============================================================================

/**
 * Task service con todas las operaciones CRUD.
 *
 * EJEMPLO DE USO COMPLETO EN COMPONENTE:
 *
 * import { taskService } from '@/services/taskService';
 * import { TaskStatus, TaskPriority } from '@/types';
 *
 * // Componente TaskList
 * const TaskList = () => {
 *   const [tasks, setTasks] = useState<PaginatedList<TaskDto>>();
 *   const [page, setPage] = useState(1);
 *
 *   useEffect(() => {
 *     const fetchTasks = async () => {
 *       const data = await taskService.getTasks({
 *         page,
 *         pageSize: 20,
 *         status: TaskStatus.Pending
 *       });
 *       setTasks(data);
 *     };
 *
 *     fetchTasks();
 *   }, [page]);
 *
 *   return (
 *     <div>
 *       {tasks?.items.map(task => (
 *         <TaskCard key={task.id} task={task} />
 *       ))}
 *       <Pagination
 *         page={page}
 *         totalPages={Math.ceil(tasks.totalCount / tasks.pageSize)}
 *         onPageChange={setPage}
 *       />
 *     </div>
 *   );
 * };
 *
 * // Componente CreateTask
 * const CreateTask = () => {
 *   const handleSubmit = async (data: CreateTaskRequest) => {
 *     try {
 *       const newTask = await taskService.createTask(data);
 *       navigate(`/tasks/${newTask.id}`);
 *     } catch (error) {
 *       showError(error.message);
 *     }
 *   };
 *
 *   return <TaskForm onSubmit={handleSubmit} />;
 * };
 *
 * // Componente TaskDetail
 * const TaskDetail = ({ id }: { id: string }) => {
 *   const [task, setTask] = useState<TaskDto>();
 *
 *   useEffect(() => {
 *     taskService.getTaskById(id).then(setTask);
 *   }, [id]);
 *
 *   const handleComplete = async () => {
 *     await taskService.updateTask(id, {
 *       ...task,
 *       status: TaskStatus.Completed
 *     });
 *     setTask({ ...task, status: TaskStatus.Completed });
 *   };
 *
 *   const handleDelete = async () => {
 *     if (confirm('Delete task?')) {
 *       await taskService.deleteTask(id);
 *       navigate('/tasks');
 *     }
 *   };
 *
 *   return (
 *     <div>
 *       <h1>{task?.title}</h1>
 *       <p>{task?.description}</p>
 *       <button onClick={handleComplete}>Complete</button>
 *       <button onClick={handleDelete}>Delete</button>
 *     </div>
 *   );
 * };
 */
export const taskService = {
  getTasks,
  getTaskById,
  createTask,
  updateTask,
  deleteTask,
  formatDateForApi,
  parseDateFromApi,
};

export default taskService;
