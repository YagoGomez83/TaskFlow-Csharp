/**
 * Configuración de Axios con interceptores para JWT
 *
 * EXPLICACIÓN DE AXIOS INTERCEPTORS:
 *
 * Los interceptors son funciones que se ejecutan antes/después de cada request/response.
 * Son perfectos para:
 * ✅ Agregar headers de autenticación automáticamente
 * ✅ Manejar refresh de tokens expirados
 * ✅ Logging de requests/responses
 * ✅ Manejo centralizado de errores
 *
 * FLUJO DE AUTENTICACIÓN:
 *
 * 1. Usuario hace login → Recibe accessToken + refreshToken
 * 2. Cada request automáticamente incluye: Authorization: Bearer {accessToken}
 * 3. Si accessToken expira (15 min) → Interceptor detecta 401
 * 4. Interceptor automáticamente hace refresh con refreshToken
 * 5. Retry del request original con nuevo accessToken
 * 6. Si refreshToken también expiró → Redirect a login
 */

import axios, { AxiosError, AxiosRequestConfig, InternalAxiosRequestConfig } from 'axios';
import type { AuthResponse, ApiError } from '../types';

// ============================================================================
// CONFIGURACIÓN BASE
// ============================================================================

/**
 * URL base del API backend.
 *
 * EXPLICACIÓN:
 * - Development: http://localhost:5000 (puerto del backend .NET)
 * - Production: Variable de entorno VITE_API_URL
 * - Vite expone variables que empiezan con VITE_ al bundle
 *
 * Configurar en .env:
 * VITE_API_URL=https://api.taskflow.com
 */
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

/**
 * Instancia de Axios configurada para el API.
 *
 * EXPLICACIÓN:
 * - baseURL: Prefijo para todos los requests
 * - timeout: Cancelar request después de 30 segundos
 * - headers: Content-Type para JSON
 */
export const api = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// ============================================================================
// TOKEN STORAGE
// ============================================================================

/**
 * Keys para localStorage.
 *
 * EXPLICACIÓN:
 * - Usar constantes previene typos
 * - Prefijo 'taskflow_' evita colisiones con otras apps
 * - localStorage persiste entre sesiones (no expira)
 */
const TOKEN_KEYS = {
  ACCESS_TOKEN: 'taskflow_access_token',
  REFRESH_TOKEN: 'taskflow_refresh_token',
} as const;

/**
 * Obtiene el accessToken del localStorage.
 *
 * EXPLICACIÓN:
 * - Llamado por request interceptor antes de cada request
 * - Retorna null si no hay token (usuario no autenticado)
 */
export const getAccessToken = (): string | null => {
  return localStorage.getItem(TOKEN_KEYS.ACCESS_TOKEN);
};

/**
 * Obtiene el refreshToken del localStorage.
 *
 * EXPLICACIÓN:
 * - Usado solo cuando accessToken expira
 * - No se envía en cada request (solo access token)
 */
export const getRefreshToken = (): string | null => {
  return localStorage.getItem(TOKEN_KEYS.REFRESH_TOKEN);
};

/**
 * Guarda ambos tokens en localStorage.
 *
 * EXPLICACIÓN:
 * - Llamado después de login/register exitoso
 * - También después de refresh exitoso (token rotation)
 * - localStorage es síncrono, no necesita await
 *
 * SEGURIDAD:
 * - localStorage es vulnerable a XSS
 * - Alternativa más segura: httpOnly cookies (requiere cambios en backend)
 * - Para producción, considerar usar cookies + CSRF protection
 */
export const setTokens = (authResponse: AuthResponse): void => {
  localStorage.setItem(TOKEN_KEYS.ACCESS_TOKEN, authResponse.accessToken);
  localStorage.setItem(TOKEN_KEYS.REFRESH_TOKEN, authResponse.refreshToken);
};

/**
 * Elimina ambos tokens del localStorage.
 *
 * EXPLICACIÓN:
 * - Llamado en logout
 * - También si refresh falla (tokens inválidos)
 * - Limpia la sesión del usuario
 */
export const clearTokens = (): void => {
  localStorage.removeItem(TOKEN_KEYS.ACCESS_TOKEN);
  localStorage.removeItem(TOKEN_KEYS.REFRESH_TOKEN);
};

// ============================================================================
// REQUEST INTERCEPTOR
// ============================================================================

/**
 * Request interceptor: Agrega Authorization header automáticamente.
 *
 * EXPLICACIÓN:
 *
 * Este interceptor se ejecuta ANTES de cada request.
 * Si hay un accessToken guardado, lo agrega al header:
 * Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
 *
 * VENTAJAS:
 * - No necesitas agregar el header manualmente en cada request
 * - Centralizado: Un lugar para manejar autenticación
 * - Consistente: Todos los requests usan el mismo formato
 *
 * EJEMPLO SIN INTERCEPTOR (malo):
 * const tasks = await axios.get('/tasks', {
 *   headers: { Authorization: `Bearer ${token}` } // Repetir en cada request
 * });
 *
 * EJEMPLO CON INTERCEPTOR (bueno):
 * const tasks = await api.get('/tasks'); // Token agregado automáticamente
 */
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = getAccessToken();

    // Si hay token, agregarlo al header
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error: AxiosError) => {
    // Error al preparar el request (raro, pero posible)
    return Promise.reject(error);
  }
);

// ============================================================================
// RESPONSE INTERCEPTOR
// ============================================================================

/**
 * Flag para prevenir múltiples refreshes simultáneos.
 *
 * EXPLICACIÓN:
 *
 * Problema sin flag:
 * - 5 requests simultáneos expiran a la vez
 * - Todos intentan refresh simultáneamente
 * - Backend recibe 5 requests de refresh
 * - Solo el primero tiene éxito, otros fallan (token rotation)
 *
 * Solución con flag:
 * - Primer request que detecta 401 setea flag a true
 * - Otros requests esperan a que el refresh termine
 * - Todos usan el nuevo token
 */
let isRefreshing = false;

/**
 * Cola de requests fallidos esperando nuevo token.
 *
 * EXPLICACIÓN:
 *
 * Cuando detectamos que el token expiró:
 * 1. Pausamos todos los requests nuevos
 * 2. Hacemos refresh del token
 * 3. Reintentamos todos los requests pausados con nuevo token
 *
 * Los requests pausados se guardan en esta cola como Promises.
 */
let failedQueue: Array<{
  resolve: (value?: unknown) => void;
  reject: (reason?: unknown) => void;
}> = [];

/**
 * Procesa la cola de requests fallidos después de refresh.
 *
 * EXPLICACIÓN:
 *
 * @param error - Si refresh falló, rechazar todos los requests
 * @param token - Nuevo accessToken si refresh tuvo éxito
 *
 * Llama resolve() o reject() en cada Promise en la cola.
 * Limpia la cola después de procesar.
 */
const processQueue = (error: Error | null, token: string | null = null): void => {
  failedQueue.forEach((promise) => {
    if (error) {
      promise.reject(error);
    } else {
      promise.resolve(token);
    }
  });

  failedQueue = [];
};

/**
 * Response interceptor: Maneja refresh automático de tokens.
 *
 * EXPLICACIÓN:
 *
 * Este interceptor se ejecuta DESPUÉS de cada response.
 *
 * FLUJO NORMAL (response exitoso):
 * 1. Request completa con 200/201/204
 * 2. Interceptor retorna response sin modificar
 * 3. Componente recibe la data
 *
 * FLUJO CON TOKEN EXPIRADO:
 * 1. Request falla con 401 Unauthorized
 * 2. Interceptor detecta que token expiró
 * 3. Hace refresh con refreshToken
 * 4. Guarda nuevo accessToken
 * 5. Reinten

ta request original con nuevo token
 * 6. Retorna response del retry
 *
 * FLUJO CON REFRESH TOKEN EXPIRADO:
 * 1. Request falla con 401
 * 2. Refresh también falla con 401
 * 3. Limpia tokens
 * 4. Redirect a login
 * 5. Usuario debe autenticarse nuevamente
 */
api.interceptors.response.use(
  // Response exitoso: Pasar sin modificar
  (response) => response,

  // Response con error: Intentar refresh si es 401
  async (error: AxiosError) => {
    const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean };

    // Si no es 401 o ya reintentamos, rechazar
    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(createApiError(error));
    }

    // Si ya hay un refresh en progreso, esperar a que termine
    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        failedQueue.push({ resolve, reject });
      })
        .then(() => {
          // Refresh terminó exitosamente, reintentar request original
          return api(originalRequest);
        })
        .catch((err) => {
          return Promise.reject(err);
        });
    }

    // Marcar que vamos a hacer refresh
    originalRequest._retry = true;
    isRefreshing = true;

    const refreshToken = getRefreshToken();

    // Si no hay refreshToken, redirect a login
    if (!refreshToken) {
      processQueue(new Error('No refresh token available'), null);
      isRefreshing = false;
      clearTokens();
      window.location.href = '/login';
      return Promise.reject(createApiError(error));
    }

    try {
      // Hacer refresh del token
      const response = await axios.post<AuthResponse>(
        `${API_BASE_URL}/api/auth/refresh`,
        { refreshToken }
      );

      const { accessToken } = response.data;

      // Guardar nuevos tokens
      setTokens(response.data);

      // Procesar cola de requests fallidos
      processQueue(null, accessToken);

      // Reintentar request original con nuevo token
      if (originalRequest.headers) {
        originalRequest.headers.Authorization = `Bearer ${accessToken}`;
      }

      return api(originalRequest);
    } catch (refreshError) {
      // Refresh falló: Limpiar tokens y redirect a login
      processQueue(refreshError as Error, null);
      clearTokens();
      window.location.href = '/login';
      return Promise.reject(createApiError(error));
    } finally {
      isRefreshing = false;
    }
  }
);

// ============================================================================
// ERROR HANDLING
// ============================================================================

/**
 * Crea un ApiError estandarizado desde AxiosError.
 *
 * EXPLICACIÓN:
 *
 * Axios lanza diferentes tipos de errores:
 * - Network error (sin conexión)
 * - Timeout
 * - Response con 4xx/5xx
 *
 * Esta función normaliza todos a un formato consistente.
 * Extrae ProblemDetails del backend si está disponible.
 */
function createApiError(error: AxiosError): ApiError {
  // Network error o timeout
  if (!error.response) {
    return {
      message: error.message || 'Network error',
      status: 0,
    };
  }

  // Response con error del backend
  const status = error.response.status;
  const data = error.response.data as any;

  return {
    message: data?.title || data?.message || error.message,
    status,
    details: data?.type ? data : undefined,
  };
}

// ============================================================================
// EXPORTS
// ============================================================================

/**
 * API client configurado y listo para usar.
 *
 * EJEMPLO DE USO:
 *
 * import { api } from '@/services/api';
 *
 * // GET request
 * const tasks = await api.get<PaginatedList<TaskDto>>('/tasks');
 *
 * // POST request
 * const newTask = await api.post<TaskDto>('/tasks', createTaskRequest);
 *
 * // PUT request
 * const updated = await api.put<TaskDto>(`/tasks/${id}`, updateTaskRequest);
 *
 * // DELETE request
 * await api.delete(`/tasks/${id}`);
 *
 * Todos los requests incluyen Authorization header automáticamente.
 * Token refresh es transparente, el componente no nota la diferencia.
 */
export default api;
