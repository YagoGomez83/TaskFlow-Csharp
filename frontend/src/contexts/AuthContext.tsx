/**
 * Authentication Context - Global state management for authentication
 *
 * EXPLICACIÓN DE REACT CONTEXT API:
 *
 * Context API es la solución nativa de React para manejar estado global.
 * Resuelve el problema de "prop drilling" (pasar props por múltiples niveles).
 *
 * PROBLEMA SIN CONTEXT:
 *
 * App
 *  └─ Layout (user prop)
 *      └─ Navbar (user prop)
 *          └─ UserMenu (user prop)
 *              └─ Avatar (FINALMENTE USA user)
 *
 * Pasamos "user" por 3 componentes intermedios que no lo usan.
 *
 * SOLUCIÓN CON CONTEXT:
 *
 * App
 *  └─ AuthProvider (provee user)
 *      └─ Layout
 *          └─ Navbar
 *              └─ UserMenu
 *                  └─ Avatar (usa useAuth() directamente)
 *
 * Cualquier componente puede acceder a "user" sin prop drilling.
 *
 * CUÁNDO USAR CONTEXT:
 *
 * ✅ Authentication state (user, tokens)
 * ✅ Theme (dark/light mode)
 * ✅ Language/i18n
 * ✅ Global settings
 *
 * ❌ NO para estado que cambia frecuentemente (causa re-renders)
 * ❌ NO para estado local de componente
 * ❌ NO para cache de datos (usar React Query)
 *
 * PATRÓN PROVIDER:
 *
 * 1. Crear Context con createContext()
 * 2. Crear Provider component que envuelve la app
 * 3. Crear custom hook (useAuth) para consumir el context
 *
 * VENTAJAS:
 *
 * ✅ Estado centralizado
 * ✅ No prop drilling
 * ✅ Fácil de testear (mock del Provider)
 * ✅ Type-safe con TypeScript
 * ✅ React DevTools muestra el estado
 */

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { authService } from '../services/authService';
import type { User, LoginRequest, RegisterRequest, AuthResponse } from '../types';

// ============================================================================
// TYPES
// ============================================================================

/**
 * Shape del Context value (estado + funciones).
 *
 * EXPLICACIÓN:
 *
 * Este interface define TODO lo que el AuthContext provee:
 *
 * STATE:
 * - user: Usuario actual (null si no autenticado)
 * - isLoading: true mientras carga/verifica sesión
 * - error: Mensaje de error si login/register falla
 *
 * ACTIONS:
 * - login: Autenticar usuario
 * - register: Registrar nuevo usuario
 * - logout: Cerrar sesión
 *
 * COMPUTED:
 * - isAuthenticated: Derivado de user !== null
 *
 * TypeScript garantiza que todos los componentes usan la API correcta.
 */
interface AuthContextType {
  // State
  user: User | null;
  isLoading: boolean;
  error: string | null;

  // Computed
  isAuthenticated: boolean;

  // Actions
  login: (credentials: LoginRequest) => Promise<void>;
  register: (userData: RegisterRequest) => Promise<void>;
  logout: () => void;
  clearError: () => void;
}

/**
 * Props del AuthProvider.
 *
 * EXPLICACIÓN:
 *
 * ReactNode acepta cualquier contenido React válido:
 * - JSX elements
 * - Strings, numbers
 * - Arrays de elements
 * - null, undefined (renderiza nada)
 *
 * children es lo que se envuelve con <AuthProvider>:
 * <AuthProvider>
 *   <App />  ← Este es "children"
 * </AuthProvider>
 */
interface AuthProviderProps {
  children: ReactNode;
}

// ============================================================================
// CONTEXT CREATION
// ============================================================================

/**
 * Create the Auth Context.
 *
 * EXPLICACIÓN:
 *
 * createContext<T>(defaultValue) crea un Context con tipo T.
 *
 * undefined como defaultValue:
 * - Indica que el Context DEBE usarse dentro de un Provider
 * - Si usas useAuth() fuera del Provider, TypeScript advertirá
 * - Forzamos uso correcto del Context
 *
 * Alternativa con defaultValue real:
 * createContext<AuthContextType>({
 *   user: null,
 *   isLoading: false,
 *   // ... todos los valores
 * })
 *
 * Pero esto permite usar Context sin Provider (error silencioso).
 *
 * Con undefined, detectamos el error inmediatamente:
 * if (context === undefined) throw Error("Must use within Provider")
 */
const AuthContext = createContext<AuthContextType | undefined>(undefined);

// ============================================================================
// PROVIDER COMPONENT
// ============================================================================

/**
 * AuthProvider component - Maneja estado de autenticación global.
 *
 * EXPLICACIÓN DEL FLUJO:
 *
 * 1. MOUNT (primera vez que se monta):
 *    - useEffect(() => { restoreSession() }, [])
 *    - Intenta restaurar sesión desde localStorage
 *    - Si hay token válido, decodifica y setea user
 *    - Si no hay token o expiró, user = null
 *
 * 2. LOGIN:
 *    - Usuario envía email/password
 *    - Llama authService.login()
 *    - Servicio guarda tokens en localStorage
 *    - Decodifica JWT y setea user
 *    - Componente re-renderiza con user !== null
 *
 * 3. REGISTER:
 *    - Usuario envía email/password/confirmPassword
 *    - Llama authService.register()
 *    - Backend auto-loguea (retorna tokens)
 *    - Igual que login: guarda tokens y setea user
 *
 * 4. LOGOUT:
 *    - Llama authService.logout()
 *    - Limpia tokens de localStorage
 *    - Setea user = null
 *    - Componente re-renderiza sin usuario
 *
 * STATE MANAGEMENT:
 *
 * Usamos useState para 3 piezas de estado:
 * - user: Usuario actual o null
 * - isLoading: true durante operaciones async
 * - error: Mensaje de error o null
 *
 * Por qué no usamos useReducer:
 * - Estado simple (3 valores independientes)
 * - No hay lógica compleja de transiciones
 * - useState es más directo para este caso
 *
 * Si el estado crece, considerar useReducer:
 * - Múltiples sub-estados relacionados
 * - Lógica compleja de actualización
 * - Necesidad de history/undo
 *
 * @param children - Componentes hijos que tendrán acceso al context
 */
export function AuthProvider({ children }: AuthProviderProps) {
  // STATE
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  /**
   * Restaura la sesión desde localStorage al montar.
   *
   * EXPLICACIÓN:
   *
   * useEffect con [] como dependencias:
   * - Se ejecuta UNA VEZ cuando el componente monta
   * - Equivalente a componentDidMount en class components
   *
   * FLUJO:
   *
   * 1. Usuario recarga la página (F5)
   * 2. AuthProvider monta
   * 3. useEffect se ejecuta
   * 4. Intenta obtener user desde token guardado
   * 5. Si existe y es válido, restaura sesión
   * 6. Si no existe o expiró, user = null (login required)
   *
   * LOADING STATE:
   *
   * - isLoading = true al inicio
   * - Previene flash de "no autenticado" mientras verifica token
   * - Muestra spinner/skeleton mientras carga
   * - isLoading = false cuando termina verificación
   *
   * SEGURIDAD:
   *
   * - Token puede haber expirado mientras app estaba cerrada
   * - isTokenExpired() verifica antes de confiar
   * - Si expiró, axios interceptor manejará refresh automático
   * - Si refresh falla, redirect a login
   *
   * Por qué no async useEffect directamente:
   * useEffect no puede ser async, pero puede llamar funciones async.
   *
   * ❌ Incorrecto:
   * useEffect(async () => { await something() }, [])
   *
   * ✅ Correcto:
   * useEffect(() => {
   *   const doAsync = async () => { await something() }
   *   doAsync()
   * }, [])
   */
  useEffect(() => {
    const restoreSession = () => {
      try {
        // Intentar obtener usuario desde token guardado
        const currentUser = authService.getCurrentUser();

        if (currentUser && authService.isAuthenticated()) {
          // Token existe y es válido
          setUser(currentUser);
        } else {
          // No hay token o expiró
          setUser(null);
        }
      } catch (err) {
        // Error al decodificar token (corrupto?)
        console.error('Failed to restore session:', err);
        setUser(null);
      } finally {
        // Siempre marcar como no loading
        setIsLoading(false);
      }
    };

    restoreSession();
  }, []); // [] = ejecutar solo una vez al montar

  /**
   * Login function - Autentica usuario.
   *
   * EXPLICACIÓN:
   *
   * async function porque llama a API (operación asíncrona).
   * Debe ser await-ed por el componente que la llama.
   *
   * FLUJO:
   *
   * 1. Setear isLoading = true (mostrar spinner)
   * 2. Limpiar error previo (si hubo)
   * 3. Llamar authService.login()
   *    - Envía POST /api/auth/login
   *    - Backend valida credenciales
   *    - Retorna tokens
   *    - authService guarda tokens en localStorage
   * 4. Obtener user desde token decodificado
   * 5. Setear user (trigger re-render)
   * 6. isLoading = false
   *
   * ERROR HANDLING:
   *
   * Si login falla:
   * - catch captura ApiError
   * - Setear error message (mostrar en UI)
   * - user permanece null
   * - isLoading = false
   *
   * EJEMPLO DE USO:
   *
   * const { login, error } = useAuth();
   *
   * const handleSubmit = async (data) => {
   *   try {
   *     await login(data);
   *     navigate('/dashboard'); // Éxito
   *   } catch (err) {
   *     // Error ya está en context.error
   *     // Mostrar en UI
   *   }
   * };
   *
   * @param credentials - Email y password
   * @throws ApiError si login falla (también setea error en state)
   */
  const login = async (credentials: LoginRequest): Promise<void> => {
    try {
      setIsLoading(true);
      setError(null);

      // Llamar servicio de autenticación
      await authService.login(credentials);

      // Obtener usuario desde token decodificado
      const currentUser = authService.getCurrentUser();

      if (currentUser) {
        setUser(currentUser);
      } else {
        throw new Error('Failed to decode user from token');
      }
    } catch (err: any) {
      // Setear error para mostrar en UI
      const errorMessage = err.message || 'Login failed';
      setError(errorMessage);
      setUser(null);

      // Re-throw para que componente pueda manejarlo también
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  /**
   * Register function - Registra nuevo usuario.
   *
   * EXPLICACIÓN:
   *
   * Similar a login, pero llama authService.register().
   *
   * FLUJO:
   *
   * 1. Validar que passwords coinciden (frontend)
   * 2. Setear isLoading = true
   * 3. Llamar authService.register()
   *    - POST /api/auth/register
   *    - Backend valida email único y password fuerte
   *    - Crea usuario en DB
   *    - Auto-loguea (retorna tokens)
   * 4. Tokens guardados automáticamente
   * 5. Decodificar token y setear user
   * 6. Usuario queda autenticado (no necesita login adicional)
   *
   * AUTO-LOGIN DESPUÉS DE REGISTER:
   *
   * Backend retorna tokens después de register exitoso.
   * Esto mejora UX: usuario no necesita login adicional.
   *
   * Alternativa (sin auto-login):
   * - Backend solo crea usuario
   * - Frontend muestra "Check your email to verify"
   * - Usuario hace login después de verificar email
   *
   * Nuestro flujo (con auto-login):
   * - Register → Autenticado automáticamente → Dashboard
   *
   * @param userData - Email, password, confirmPassword
   * @throws ApiError si register falla
   */
  const register = async (userData: RegisterRequest): Promise<void> => {
    try {
      setIsLoading(true);
      setError(null);

      // Llamar servicio de registro
      await authService.register(userData);

      // Obtener usuario desde token decodificado
      const currentUser = authService.getCurrentUser();

      if (currentUser) {
        setUser(currentUser);
      } else {
        throw new Error('Failed to decode user from token');
      }
    } catch (err: any) {
      const errorMessage = err.message || 'Registration failed';
      setError(errorMessage);
      setUser(null);

      // Re-throw para que componente pueda manejarlo
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  /**
   * Logout function - Cierra sesión.
   *
   * EXPLICACIÓN:
   *
   * Sincrónica (no async) porque solo limpia estado local.
   *
   * FLUJO:
   *
   * 1. Llamar authService.logout()
   *    - Limpia tokens de localStorage
   * 2. Setear user = null
   * 3. Limpiar error si existe
   * 4. Componente re-renderiza sin usuario
   *
   * NOTA: No hay loading state para logout
   * - Es operación instantánea
   * - No hay API call (en este proyecto)
   *
   * Si implementas logout en backend:
   * - Hacer async
   * - Agregar isLoading state
   * - POST /api/auth/logout con refreshToken
   * - Backend revoca token en DB
   *
   * REDIRECCIÓN:
   *
   * Esta función NO hace redirect.
   * El componente que llama logout debe hacer redirect:
   *
   * const handleLogout = () => {
   *   logout();
   *   navigate('/login');
   * };
   *
   * Por qué no redirect aquí:
   * - Separación de responsabilidades
   * - Context no debe saber sobre routing
   * - Componente decide adónde ir después de logout
   */
  const logout = (): void => {
    authService.logout();
    setUser(null);
    setError(null);
  };

  /**
   * Limpia el mensaje de error.
   *
   * EXPLICACIÓN:
   *
   * Útil para limpiar errores después de:
   * - Usuario cerró el mensaje de error
   * - Usuario corrigió input y va a reintentar
   * - Navegar a otra página
   *
   * EJEMPLO:
   *
   * const { error, clearError } = useAuth();
   *
   * return (
   *   <div>
   *     {error && (
   *       <Alert onClose={clearError}>
   *         {error}
   *       </Alert>
   *     )}
   *   </div>
   * );
   */
  const clearError = (): void => {
    setError(null);
  };

  /**
   * Computed value: isAuthenticated.
   *
   * EXPLICACIÓN:
   *
   * Derivado de user !== null.
   * Se recalcula automáticamente cuando user cambia.
   *
   * Por qué computed value:
   * - No duplicar lógica (user !== null en múltiples lugares)
   * - Single source of truth
   * - Fácil de cambiar lógica (ej: agregar email verificado)
   *
   * Alternativa (sin computed):
   * Cada componente hace: if (user !== null)
   *
   * Con computed:
   * Componente usa: if (isAuthenticated)
   */
  const isAuthenticated = user !== null;

  /**
   * Value object que se provee a todos los consumers.
   *
   * EXPLICACIÓN:
   *
   * Este objeto contiene TODO lo que el Context provee:
   * - Estado (user, isLoading, error)
   * - Acciones (login, register, logout)
   * - Computed values (isAuthenticated)
   *
   * RE-RENDERS:
   *
   * Cuando cambia cualquier valor en este objeto:
   * - Todos los componentes que usan useAuth() re-renderizan
   * - React compara por referencia
   *
   * OPTIMIZACIÓN:
   *
   * Si el objeto value cambia constantemente, puede causar re-renders innecesarios.
   *
   * Solución 1 - useMemo:
   * const value = useMemo(() => ({ user, login, ... }), [user, isLoading, error])
   *
   * Solución 2 - useCallback para funciones:
   * const login = useCallback(async (creds) => { ... }, [])
   *
   * Para este Context, no es necesario:
   * - user, isLoading, error solo cambian en operaciones significativas
   * - Funciones son estables (no dependen de props)
   * - Re-renders son intencionados y necesarios
   */
  const value: AuthContextType = {
    // State
    user,
    isLoading,
    error,

    // Computed
    isAuthenticated,

    // Actions
    login,
    register,
    logout,
    clearError,
  };

  /**
   * Provider component que envuelve children.
   *
   * EXPLICACIÓN:
   *
   * <AuthContext.Provider value={value}>
   *   {children}
   * </AuthContext.Provider>
   *
   * - Provider hace el value disponible a todos los descendants
   * - Cualquier componente hijo puede usar useAuth()
   * - No importa qué tan profundo esté en el árbol
   *
   * LOADING STATE:
   *
   * Mientras isLoading = true (restaurando sesión):
   * - Mostramos null (nada)
   * - Alternativa: Mostrar splash screen / spinner
   * - Previene flash de contenido incorrecto
   *
   * return isLoading ? <SplashScreen /> : (
   *   <AuthContext.Provider value={value}>
   *     {children}
   *   </AuthContext.Provider>
   * );
   */
  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}

// ============================================================================
// CUSTOM HOOK
// ============================================================================

/**
 * Custom hook para consumir AuthContext.
 *
 * EXPLICACIÓN:
 *
 * useAuth() es un wrapper alrededor de useContext(AuthContext).
 *
 * VENTAJAS:
 *
 * 1. API MÁS LIMPIA:
 *    ✅ const { user } = useAuth()
 *    ❌ const context = useContext(AuthContext)
 *
 * 2. VALIDACIÓN:
 *    - Lanza error si se usa fuera del Provider
 *    - TypeScript detecta el error en tiempo de compilación
 *
 * 3. ENCAPSULACIÓN:
 *    - Esconde implementación (Context)
 *    - Componentes solo saben de useAuth(), no de Context
 *    - Fácil cambiar implementación interna
 *
 * PATRÓN:
 *
 * Este patrón (Provider + custom hook) es estándar en React:
 * - useAuth() para authentication
 * - useTheme() para theme
 * - useTranslation() para i18n
 *
 * ERROR HANDLING:
 *
 * Si context es undefined:
 * - Significa que useAuth() se llamó fuera de <AuthProvider>
 * - Lanzar error descriptivo
 * - Developer debe envolver con Provider
 *
 * EJEMPLO DE ERROR:
 *
 * function MyComponent() {
 *   const { user } = useAuth(); // ERROR!
 *   // MyComponent no está dentro de <AuthProvider>
 * }
 *
 * SOLUCIÓN:
 *
 * function App() {
 *   return (
 *     <AuthProvider>  ← Envolver aquí
 *       <MyComponent />  ← Ahora puede usar useAuth()
 *     </AuthProvider>
 *   );
 * }
 *
 * @returns AuthContextType con estado y funciones
 * @throws Error si se usa fuera de AuthProvider
 */
export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);

  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }

  return context;
}

// ============================================================================
// EXPORTS
// ============================================================================

/**
 * Exports:
 * - AuthProvider: Component que envuelve la app
 * - useAuth: Hook para consumir el context
 *
 * EJEMPLO DE USO COMPLETO:
 *
 * // En main.tsx o App.tsx
 * import { AuthProvider } from './contexts/AuthContext';
 *
 * root.render(
 *   <AuthProvider>
 *     <App />
 *   </AuthProvider>
 * );
 *
 * // En cualquier componente
 * import { useAuth } from './contexts/AuthContext';
 *
 * function LoginPage() {
 *   const { login, isLoading, error } = useAuth();
 *
 *   const handleSubmit = async (data) => {
 *     try {
 *       await login(data);
 *       navigate('/dashboard');
 *     } catch (err) {
 *       // Error ya está en context.error
 *     }
 *   };
 *
 *   return (
 *     <form onSubmit={handleSubmit}>
 *       {error && <Alert>{error}</Alert>}
 *       <input name="email" />
 *       <input name="password" type="password" />
 *       <button disabled={isLoading}>
 *         {isLoading ? 'Loading...' : 'Login'}
 *       </button>
 *     </form>
 *   );
 * }
 *
 * // En Navbar
 * function Navbar() {
 *   const { user, isAuthenticated, logout } = useAuth();
 *
 *   return (
 *     <nav>
 *       {isAuthenticated ? (
 *         <>
 *           <span>Welcome, {user.email}</span>
 *           <button onClick={logout}>Logout</button>
 *         </>
 *       ) : (
 *         <Link to="/login">Login</Link>
 *       )}
 *     </nav>
 *   );
 * }
 *
 * // Protected Route
 * function ProtectedRoute({ children }) {
 *   const { isAuthenticated, isLoading } = useAuth();
 *
 *   if (isLoading) {
 *     return <Spinner />;
 *   }
 *
 *   if (!isAuthenticated) {
 *     return <Navigate to="/login" />;
 *   }
 *
 *   return children;
 * }
 */
export default AuthContext;
