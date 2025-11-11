/**
 * TypeScript types and interfaces for the TaskFlow application
 *
 * EXPLICACIÓN DE TYPES:
 *
 * En TypeScript, definir tipos explícitos proporciona:
 * ✅ Type safety - El compilador detecta errores antes de ejecutar
 * ✅ IntelliSense - Autocompletado en el IDE
 * ✅ Documentación - Los tipos son documentación viva
 * ✅ Refactoring seguro - Cambios propagados automáticamente
 *
 * Estos tipos deben coincidir exactamente con los DTOs del backend.
 */

// ============================================================================
// AUTH TYPES
// ============================================================================

/**
 * Response del backend después de login/register exitoso.
 *
 * EXPLICACIÓN:
 * - accessToken: JWT de corta duración (15 min) para autenticar requests
 * - refreshToken: Token de larga duración (7 días) para renovar accessToken
 * - expiresIn: Segundos hasta que expira el accessToken (900 = 15 min)
 * - tokenType: Siempre "Bearer" (standard OAuth 2.0)
 */
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  tokenType: string;
}

/**
 * Request para login.
 *
 * EXPLICACIÓN:
 * - Email y password en texto plano
 * - Backend hashea password con BCrypt antes de comparar
 * - Siempre usar HTTPS para proteger credenciales en tránsito
 */
export interface LoginRequest {
  email: string;
  password: string;
}

/**
 * Request para registrar nuevo usuario.
 *
 * EXPLICACIÓN:
 * - confirmPassword: Validado en frontend antes de enviar
 * - Backend valida que password y confirmPassword coincidan
 * - Backend valida fuerza de password (8+ chars, mayúscula, número, especial)
 */
export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
}

/**
 * Request para renovar accessToken.
 *
 * EXPLICACIÓN:
 * - Se envía cuando accessToken expira (cada 15 min)
 * - Backend valida refreshToken y retorna nuevo par de tokens
 * - Implementa token rotation (nuevo refreshToken cada vez)
 */
export interface RefreshTokenRequest {
  refreshToken: string;
}

/**
 * Usuario autenticado extraído del JWT.
 *
 * EXPLICACIÓN:
 * - Información del usuario decodificada del accessToken
 * - Se almacena en AuthContext para acceso global
 * - NO contiene información sensible (sin password)
 */
export interface User {
  id: string;
  email: string;
  role: UserRole;
}

/**
 * Roles de usuario en el sistema.
 *
 * EXPLICACIÓN:
 * - User: Rol por defecto, puede gestionar sus propias tareas
 * - Admin: Puede ver/modificar tareas de todos los usuarios
 * - Enum sincronizado con backend (UserRole enum en Domain)
 */
export const UserRole = {
  User: 'User',
  Admin: 'Admin'
} as const;

export type UserRole = typeof UserRole[keyof typeof UserRole];

// ============================================================================
// TASK TYPES
// ============================================================================

/**
 * DTO de tarea retornado por el backend.
 *
 * EXPLICACIÓN:
 * - Coincide exactamente con TaskDto del backend
 * - Todas las fechas vienen en formato ISO 8601 (UTC)
 * - Frontend convierte a fecha local para mostrar
 * - Enums como strings para facilitar serialización
 */
export interface TaskDto {
  id: string;
  title: string;
  description: string | null;
  dueDate: string | null;  // ISO 8601 string
  priority: TaskPriority;
  status: TaskStatus;
  userId: string;
  createdAt: string;  // ISO 8601 string
  updatedAt: string;  // ISO 8601 string
}

/**
 * Request para crear nueva tarea.
 *
 * EXPLICACIÓN:
 * - userId se asigna automáticamente en backend desde JWT
 * - dueDate es opcional, debe ser fecha futura si se envía
 * - priority por defecto es Medium si no se especifica
 */
export interface CreateTaskRequest {
  title: string;
  description?: string;
  dueDate?: string;  // ISO 8601 string
  priority: TaskPriority;
}

/**
 * Request para actualizar tarea existente.
 *
 * EXPLICACIÓN:
 * - Todos los campos son requeridos (PUT semántica)
 * - Para updates parciales, enviar valores actuales sin cambiar
 * - Backend valida ownership (solo owner o Admin pueden actualizar)
 */
export interface UpdateTaskRequest {
  title: string;
  description?: string;
  dueDate?: string;  // ISO 8601 string
  priority: TaskPriority;
  status: TaskStatus;
}

/**
 * Estados posibles de una tarea.
 *
 * EXPLICACIÓN:
 * - Pending: Tarea creada, no iniciada
 * - InProgress: Usuario trabajando en la tarea
 * - Completed: Tarea terminada
 * - Enum sincronizado con backend (TaskStatus enum en Domain)
 */
export const TaskStatus = {
  Pending: 'Pending',
  InProgress: 'InProgress',
  Completed: 'Completed'
} as const;

export type TaskStatus = typeof TaskStatus[keyof typeof TaskStatus];

/**
 * Prioridades de tarea.
 *
 * EXPLICACIÓN:
 * - Low: Baja prioridad, hacer cuando haya tiempo
 * - Medium: Prioridad normal, hacer en orden
 * - High: Alta prioridad, hacer pronto
 * - Enum sincronizado con backend (TaskPriority enum en Domain)
 */
export const TaskPriority = {
  Low: 'Low',
  Medium: 'Medium',
  High: 'High'
} as const;

export type TaskPriority = typeof TaskPriority[keyof typeof TaskPriority];

// ============================================================================
// PAGINATION TYPES
// ============================================================================

/**
 * Lista paginada de items.
 *
 * EXPLICACIÓN:
 * - Generic type<T> permite reutilizar para cualquier entidad
 * - Coincide con PaginatedList<T> del backend
 * - Frontend puede calcular: totalPages = ceil(totalCount / pageSize)
 */
export interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

/**
 * Parámetros para query paginado.
 *
 * EXPLICACIÓN:
 * - page: 1-indexed (primera página = 1)
 * - pageSize: Cantidad de items por página (típicamente 10-50)
 * - Filtros opcionales específicos por entidad
 */
export interface PaginationParams {
  page: number;
  pageSize: number;
}

/**
 * Filtros para query de tareas.
 *
 * EXPLICACIÓN:
 * - Extends PaginationParams para incluir paginación
 * - status: Filtrar por estado (opcional)
 * - priority: Filtrar por prioridad (opcional)
 * - Backend aplica filtros en query SQL para eficiencia
 */
export interface TaskQueryParams extends PaginationParams {
  status?: TaskStatus;
  priority?: TaskPriority;
}

// ============================================================================
// API RESPONSE TYPES
// ============================================================================

/**
 * Error response estándar del backend.
 *
 * EXPLICACIÓN:
 * - Backend retorna este formato en todos los errores 4xx/5xx
 * - type: Tipo de problema (RFC 7807)
 * - title: Resumen del error
 * - status: HTTP status code
 * - detail: Descripción detallada
 * - errors: Errores de validación (optional)
 */
export interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

/**
 * Generic API error para manejo consistente.
 *
 * EXPLICACIÓN:
 * - Wrapper alrededor de ProblemDetails del backend
 * - Permite agregar metadata adicional si es necesario
 * - message: Mensaje amigable para mostrar al usuario
 */
export interface ApiError {
  message: string;
  status: number;
  details?: ProblemDetails;
}
