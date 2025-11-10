/**
 * Authentication Service
 *
 * EXPLICACIÓN DEL SERVICIO DE AUTENTICACIÓN:
 *
 * Este servicio encapsula toda la lógica de autenticación:
 * ✅ Login - Autenticar usuario con email/password
 * ✅ Register - Registrar nuevo usuario
 * ✅ Logout - Cerrar sesión y limpiar tokens
 * ✅ Token Management - Guardar/limpiar tokens
 * ✅ User Info - Decodificar JWT para obtener info del usuario
 *
 * VENTAJAS DE TENER UN SERVICIO DEDICADO:
 *
 * 1. SEPARACIÓN DE RESPONSABILIDADES:
 *    - Componentes no necesitan saber cómo funciona la autenticación
 *    - Componentes solo llaman: authService.login(email, password)
 *    - Toda la lógica compleja está aquí
 *
 * 2. REUTILIZACIÓN:
 *    - Múltiples componentes pueden usar el mismo servicio
 *    - No duplicar código de autenticación
 *
 * 3. TESTEABLE:
 *    - Fácil de testear en aislamiento
 *    - Mock del servicio para tests de componentes
 *
 * 4. MANTENIBILIDAD:
 *    - Cambiar lógica de auth en un solo lugar
 *    - Ej: Cambiar de localStorage a cookies
 */

import api, { setTokens, clearTokens, getAccessToken } from './api';
import type { LoginRequest, RegisterRequest, AuthResponse, User, UserRole } from '../types';

// ============================================================================
// JWT DECODING
// ============================================================================

/**
 * Estructura de los claims del JWT (payload decodificado).
 *
 * EXPLICACIÓN DE JWT:
 *
 * JWT (JSON Web Token) tiene 3 partes separadas por puntos:
 * header.payload.signature
 *
 * Ejemplo real:
 * eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
 *
 * El payload (parte del medio) contiene los claims:
 * {
 *   "sub": "user-id-here",           // Subject: ID del usuario
 *   "email": "user@example.com",     // Email del usuario
 *   "role": "User",                  // Rol del usuario
 *   "exp": 1234567890                // Expiration: Timestamp UTC
 * }
 *
 * IMPORTANTE:
 * - JWT NO está encriptado, solo firmado (Base64)
 * - Cualquiera puede decodificar el payload
 * - Por eso NO guardamos info sensible en el JWT
 * - La firma previene modificaciones (backend valida)
 */
interface JwtPayload {
  sub: string;        // User ID
  email: string;      // User email
  role: UserRole;     // User role
  exp: number;        // Expiration timestamp (seconds since epoch)
  iat?: number;       // Issued at (opcional)
  nbf?: number;       // Not before (opcional)
}

/**
 * Decodifica un JWT y extrae el payload (claims).
 *
 * EXPLICACIÓN:
 *
 * JWT = "header.payload.signature"
 *
 * Pasos:
 * 1. Split por '.' para separar las 3 partes
 * 2. Tomar la segunda parte (payload)
 * 3. Decode de Base64 a string
 * 4. Parse de string a objeto JSON
 *
 * ADVERTENCIA:
 * - Esta función NO valida la firma
 * - Solo para leer claims en el frontend
 * - Backend siempre valida la firma antes de confiar en el JWT
 * - Nunca tomar decisiones de seguridad solo basándose en el JWT decodificado
 *
 * @param token - JWT string
 * @returns Payload decodificado o null si es inválido
 */
function decodeJwt(token: string): JwtPayload | null {
  try {
    // Split token en 3 partes: header.payload.signature
    const parts = token.split('.');

    if (parts.length !== 3) {
      console.error('Invalid JWT format: Expected 3 parts');
      return null;
    }

    // Parte 2 es el payload (claims)
    const payload = parts[1];

    // Decode Base64 a string
    // atob() es built-in del browser para Base64 decode
    const decoded = atob(payload);

    // Parse JSON string a objeto
    return JSON.parse(decoded) as JwtPayload;
  } catch (error) {
    console.error('Failed to decode JWT:', error);
    return null;
  }
}

/**
 * Obtiene la información del usuario desde el JWT actual.
 *
 * EXPLICACIÓN:
 *
 * Extrae el accessToken de localStorage, lo decodifica y retorna
 * la información del usuario.
 *
 * CUÁNDO USAR:
 * - Al cargar la app para restaurar sesión
 * - Después de login/register para obtener info del usuario
 * - Para verificar si hay sesión activa
 *
 * CUÁNDO NO USAR:
 * - Para decisiones de seguridad críticas
 * - Backend siempre debe validar el JWT
 *
 * @returns User object o null si no hay sesión
 */
export function getCurrentUser(): User | null {
  const token = getAccessToken();

  if (!token) {
    return null;
  }

  const payload = decodeJwt(token);

  if (!payload) {
    return null;
  }

  // Mapear claims del JWT a User object
  return {
    id: payload.sub,
    email: payload.email,
    role: payload.role,
  };
}

/**
 * Verifica si el JWT ha expirado.
 *
 * EXPLICACIÓN:
 *
 * El claim 'exp' contiene el timestamp (segundos desde 1970) cuando
 * el token expira.
 *
 * Comparamos con Date.now() / 1000 (convertir milisegundos a segundos).
 *
 * IMPORTANTE:
 * - Esta verificación es aproximada
 * - Puede haber diferencia de segundos entre cliente y servidor
 * - El interceptor de Axios maneja expiración automáticamente
 * - Esta función es solo para UX (mostrar warning antes de expirar)
 *
 * @param token - JWT string
 * @returns true si expiró, false si aún válido
 */
export function isTokenExpired(token: string): boolean {
  const payload = decodeJwt(token);

  if (!payload || !payload.exp) {
    return true;
  }

  // exp está en segundos, Date.now() en milisegundos
  const currentTime = Math.floor(Date.now() / 1000);

  return payload.exp < currentTime;
}

// ============================================================================
// AUTHENTICATION FUNCTIONS
// ============================================================================

/**
 * Autentica un usuario con email y password.
 *
 * EXPLICACIÓN DEL FLUJO DE LOGIN:
 *
 * 1. Frontend envía email + password al backend
 *    POST /api/auth/login
 *    Body: { email: "user@example.com", password: "SecurePass123!" }
 *
 * 2. Backend valida credenciales:
 *    - Busca usuario por email
 *    - Verifica password con BCrypt
 *    - Verifica que cuenta no esté bloqueada
 *
 * 3. Backend genera tokens:
 *    - accessToken: JWT de 15 minutos
 *    - refreshToken: Token de 7 días guardado en DB
 *
 * 4. Backend retorna:
 *    {
 *      accessToken: "eyJhbGc...",
 *      refreshToken: "random-string-here",
 *      expiresIn: 900,
 *      tokenType: "Bearer"
 *    }
 *
 * 5. Frontend guarda tokens en localStorage
 *
 * 6. Frontend decodifica accessToken para obtener user info
 *
 * MANEJO DE ERRORES:
 * - 400: Credenciales inválidas o validación fallida
 * - 401: Usuario no existe o password incorrecta
 * - 423: Cuenta bloqueada (5 intentos fallidos)
 * - 500: Error del servidor
 *
 * @param credentials - Email y password
 * @returns Promise con AuthResponse (tokens) o lanza error
 *
 * @throws ApiError con mensaje de error del backend
 */
export async function login(credentials: LoginRequest): Promise<AuthResponse> {
  // POST /api/auth/login
  const response = await api.post<AuthResponse>('/auth/login', credentials);

  // Guardar tokens en localStorage
  // Esto permite persistir la sesión entre recargas de página
  setTokens(response.data);

  return response.data;
}

/**
 * Registra un nuevo usuario.
 *
 * EXPLICACIÓN DEL FLUJO DE REGISTRO:
 *
 * 1. Frontend valida que password y confirmPassword coincidan
 *    - Esto debe hacerse ANTES de enviar al backend
 *    - Ahorra un round-trip si no coinciden
 *
 * 2. Frontend envía datos al backend
 *    POST /api/auth/register
 *    Body: {
 *      email: "newuser@example.com",
 *      password: "SecurePass123!",
 *      confirmPassword: "SecurePass123!"
 *    }
 *
 * 3. Backend valida:
 *    - Email no existe (único)
 *    - Password cumple requisitos (8+ chars, mayúscula, número, especial)
 *    - Passwords coinciden
 *
 * 4. Backend crea usuario:
 *    - Hashea password con BCrypt (cost 12)
 *    - Guarda en DB con rol User por defecto
 *
 * 5. Backend auto-loguea al usuario:
 *    - Genera tokens JWT
 *    - Retorna mismo formato que login
 *
 * 6. Frontend guarda tokens
 *    - Usuario queda autenticado automáticamente
 *    - No necesita hacer login después de register
 *
 * REQUISITOS DE PASSWORD (validados en backend):
 * ✅ Mínimo 8 caracteres
 * ✅ Al menos 1 mayúscula (A-Z)
 * ✅ Al menos 1 minúscula (a-z)
 * ✅ Al menos 1 número (0-9)
 * ✅ Al menos 1 carácter especial (!@#$%^&*...)
 *
 * MANEJO DE ERRORES:
 * - 400: Email ya existe o password débil
 * - 422: Validación fallida (passwords no coinciden)
 * - 500: Error del servidor
 *
 * @param userData - Email, password y confirmPassword
 * @returns Promise con AuthResponse (tokens + auto-login) o lanza error
 *
 * @throws ApiError con mensaje de error del backend
 */
export async function register(userData: RegisterRequest): Promise<AuthResponse> {
  // POST /api/auth/register
  const response = await api.post<AuthResponse>('/auth/register', userData);

  // Guardar tokens (usuario ya autenticado después de register)
  setTokens(response.data);

  return response.data;
}

/**
 * Cierra la sesión del usuario.
 *
 * EXPLICACIÓN DEL FLUJO DE LOGOUT:
 *
 * 1. Limpiar tokens de localStorage
 *    - Elimina accessToken
 *    - Elimina refreshToken
 *
 * 2. (Opcional) Notificar al backend
 *    - En este proyecto: No hay endpoint de logout en backend
 *    - Alternativa: POST /api/auth/logout para revocar refreshToken
 *    - Backend marcaría refreshToken como revocado en DB
 *
 * 3. Redirect a página de login
 *    - Debe manejarse en el componente que llama logout
 *    - Ej: navigate('/login') con React Router
 *
 * RAZÓN PARA NO NOTIFICAR BACKEND:
 *
 * - RefreshTokens expiran automáticamente después de 7 días
 * - Si usuario no usa el refreshToken, se limpia automáticamente
 * - Implementar revocación de tokens es opcional
 *
 * CUÁNDO IMPLEMENTAR LOGOUT EN BACKEND:
 *
 * - Si necesitas cerrar sesión inmediatamente en todos los dispositivos
 * - Si quieres tracking de cuándo usuarios hacen logout
 * - Si quieres "logout from all devices" feature
 *
 * IMPLEMENTACIÓN CON BACKEND LOGOUT:
 *
 * export async function logout(): Promise<void> {
 *   const refreshToken = getRefreshToken();
 *
 *   if (refreshToken) {
 *     try {
 *       // Notificar backend para revocar token
 *       await api.post('/auth/logout', { refreshToken });
 *     } catch (error) {
 *       // Ignorar errores de logout, igual limpiamos tokens
 *       console.error('Logout failed:', error);
 *     }
 *   }
 *
 *   // Siempre limpiar tokens locales, incluso si backend falla
 *   clearTokens();
 * }
 *
 * SEGURIDAD:
 * - Limpiar tokens del localStorage previene reuso
 * - Usuario debe autenticarse nuevamente
 * - AccessToken expira en 15 min de todas formas
 */
export function logout(): void {
  // Limpiar tokens de localStorage
  clearTokens();

  // Nota: En producción, considerar notificar al backend
  // para revocar el refreshToken en la base de datos
}

/**
 * Verifica si hay un usuario autenticado.
 *
 * EXPLICACIÓN:
 *
 * Función helper simple que verifica si hay un accessToken válido.
 * Útil para:
 * - Renderizar UI condicional (mostrar/ocultar botones)
 * - Decidir qué mostrar en la navbar
 * - Protected routes en React Router
 *
 * IMPORTANTE:
 * - Esta verificación es solo para UX
 * - Backend SIEMPRE debe validar el JWT
 * - No confiar solo en esta función para seguridad
 *
 * @returns true si hay usuario autenticado, false si no
 */
export function isAuthenticated(): boolean {
  const token = getAccessToken();
  return token !== null && !isTokenExpired(token);
}

// ============================================================================
// EXPORTS
// ============================================================================

/**
 * Auth service con todas las funciones de autenticación.
 *
 * EJEMPLO DE USO EN COMPONENTE:
 *
 * import { authService } from '@/services/authService';
 *
 * // En un componente de Login
 * const handleLogin = async (email: string, password: string) => {
 *   try {
 *     await authService.login({ email, password });
 *     // Tokens guardados automáticamente
 *     const user = authService.getCurrentUser();
 *     console.log('Logged in as:', user.email);
 *     navigate('/dashboard');
 *   } catch (error) {
 *     console.error('Login failed:', error.message);
 *     setError(error.message);
 *   }
 * };
 *
 * // En un componente de Navbar
 * const handleLogout = () => {
 *   authService.logout();
 *   navigate('/login');
 * };
 *
 * // En un efecto para restaurar sesión
 * useEffect(() => {
 *   const user = authService.getCurrentUser();
 *   if (user) {
 *     setCurrentUser(user);
 *   }
 * }, []);
 */
export const authService = {
  login,
  register,
  logout,
  getCurrentUser,
  isAuthenticated,
  isTokenExpired,
};

export default authService;
