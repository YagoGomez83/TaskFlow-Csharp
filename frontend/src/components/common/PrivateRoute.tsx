/**
 * PrivateRoute Component - Protected route wrapper
 *
 * EXPLICACIÓN DE RUTAS PROTEGIDAS:
 *
 * Las rutas protegidas son páginas que requieren autenticación.
 * Si el usuario NO está autenticado → Redirigir a /login
 * Si el usuario SÍ está autenticado → Mostrar la página
 *
 * CASOS DE USO:
 * ✅ Dashboard (requiere login)
 * ✅ Task management (requiere login)
 * ✅ Profile settings (requiere login)
 *
 * RUTAS PÚBLICAS (NO necesitan PrivateRoute):
 * - /login
 * - /register
 * - Landing page (si existe)
 *
 * IMPLEMENTACIÓN:
 *
 * Este componente usa:
 * 1. useAuth() para verificar autenticación
 * 2. Navigate de React Router para redireccionar
 * 3. Spinner durante verificación inicial
 *
 * FLUJO:
 *
 * Usuario intenta acceder a /dashboard:
 * 1. PrivateRoute verifica isAuthenticated
 * 2. Si isLoading → Mostrar Spinner (evita flash)
 * 3. Si !isAuthenticated → <Navigate to="/login" />
 * 4. Si isAuthenticated → Renderizar children (página)
 *
 * PREVENIR FLASH DE CONTENIDO:
 *
 * Durante session restoration (al recargar página):
 * - AuthContext verifica localStorage por token
 * - isLoading = true mientras verifica
 * - PrivateRoute espera (muestra Spinner)
 * - Cuando termina verificación → Renderiza o redirige
 *
 * Sin esto, usuario vería:
 * Dashboard → Flash → Login (mal UX)
 *
 * Con esto, usuario ve:
 * Spinner → Dashboard (si autenticado)
 * Spinner → Login (si no autenticado)
 */

import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import Spinner from './Spinner';

/**
 * Props del PrivateRoute component.
 */
interface PrivateRouteProps {
  /**
   * Componente/página a renderizar si está autenticado.
   */
  children: ReactNode;
}

/**
 * PrivateRoute component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Este es un "guard component":
 * - Verifica condiciones antes de renderizar
 * - Similar a guards en Angular
 * - Patrón común en React Router
 *
 * ALTERNATIVAS:
 *
 * Antes de React Router v6, usábamos:
 * <Route
 *   path="/dashboard"
 *   element={<PrivateRoute component={Dashboard} />}
 * />
 *
 * Con React Router v6, usamos children:
 * <Route
 *   path="/dashboard"
 *   element={<PrivateRoute><Dashboard /></PrivateRoute>}
 * />
 *
 * @example
 * // En App.tsx:
 * <Route
 *   path="/dashboard"
 *   element={
 *     <PrivateRoute>
 *       <DashboardPage />
 *     </PrivateRoute>
 *   }
 * />
 */
export function PrivateRoute({ children }: PrivateRouteProps) {
  /**
   * Context de autenticación.
   */
  const { isAuthenticated, isLoading } = useAuth();

  /**
   * Estado: Loading (verificando autenticación).
   *
   * EXPLICACIÓN:
   *
   * Mientras AuthContext verifica token en localStorage:
   * - Mostrar Spinner overlay
   * - Prevenir flash de contenido no autenticado
   * - Mejor UX
   */
  if (isLoading) {
    return <Spinner overlay label="Verificando sesión..." />;
  }

  /**
   * Estado: No autenticado.
   *
   * EXPLICACIÓN:
   *
   * Navigate component de React Router:
   * - Hace redirect programático
   * - replace={true} → Reemplaza entrada en historial
   * - Usuario no puede volver con botón "atrás"
   *
   * Ejemplo:
   * Usuario intenta /dashboard sin login:
   * 1. Redirige a /login
   * 2. Usuario hace login
   * 3. Navega a /dashboard (success)
   * 4. Botón "atrás" NO vuelve a /login (porque replace=true)
   */
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  /**
   * Estado: Autenticado.
   *
   * Renderizar children (la página protegida).
   */
  return <>{children}</>;
}

export default PrivateRoute;
