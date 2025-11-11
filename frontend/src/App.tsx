/**
 * App Component - Main application router
 *
 * EXPLICACIÓN DE LA APLICACIÓN:
 *
 * Este es el componente raíz que configura React Router.
 * Define todas las rutas de la aplicación.
 *
 * ESTRUCTURA DE RUTAS:
 *
 * RUTAS PÚBLICAS (sin autenticación):
 * - / → Redirect a /login
 * - /login → LoginPage
 * - /register → RegisterPage
 *
 * RUTAS PROTEGIDAS (requieren autenticación):
 * - /dashboard → DashboardPage (home después de login)
 * - /tasks → TaskListPage (lista de tareas)
 * - /tasks/create → CreateTaskPage (crear nueva tarea)
 * - /tasks/:taskId/edit → EditTaskPage (editar tarea)
 *
 * RUTA 404:
 * - * → NotFoundPage (cualquier ruta no definida)
 *
 * LAYOUT:
 *
 * - Login/Register → Sin layout (full screen)
 * - Dashboard/Tasks → Con Layout (navbar + content)
 *
 * PROTECCIÓN:
 *
 * Rutas protegidas envueltas en <PrivateRoute>:
 * - Verifica autenticación
 * - Redirige a /login si no autenticado
 * - Muestra página si autenticado
 *
 * NAVEGACIÓN:
 *
 * React Router provee:
 * - <Link to="/path"> → Navegación sin reload
 * - useNavigate() → Navegación programática
 * - <Navigate to="/path" /> → Redirect component
 *
 * REACT ROUTER v6:
 *
 * Esta implementación usa React Router v6:
 * - <BrowserRouter> → Router wrapper
 * - <Routes> → Container de rutas
 * - <Route> → Definición de ruta individual
 * - element prop → Componente a renderizar
 */

import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import PrivateRoute from './components/common/PrivateRoute';
import Layout from './components/common/Layout';
import {
  LoginPage,
  RegisterPage,
  DashboardPage,
  TaskListPage,
  CreateTaskPage,
  EditTaskPage,
} from './pages';

/**
 * NotFoundPage - Simple 404 page.
 *
 * EXPLICACIÓN:
 *
 * Esta es una página simple para rutas no encontradas.
 * En producción, podrías crear un componente más elaborado.
 */
function NotFoundPage() {
  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '2rem',
        textAlign: 'center',
      }}
    >
      <h1 style={{ fontSize: '4rem', margin: 0 }}>404</h1>
      <p style={{ fontSize: '1.5rem', color: '#6b7280', marginBottom: '2rem' }}>
        Página no encontrada
      </p>
      <a
        href="/dashboard"
        style={{
          padding: '0.75rem 1.5rem',
          backgroundColor: '#3b82f6',
          color: 'white',
          textDecoration: 'none',
          borderRadius: '0.5rem',
          fontWeight: 600,
        }}
      >
        Volver al Dashboard
      </a>
    </div>
  );
}

/**
 * App component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Este es el componente raíz de la aplicación:
 * - Envuelve todo en BrowserRouter (para routing)
 * - Envuelve todo en AuthProvider (para autenticación global)
 * - Define todas las rutas de la app
 *
 * ORDEN DE WRAPPERS:
 *
 * BrowserRouter (externo):
 * - Debe envolver AuthProvider
 * - Porque AuthProvider usa useNavigate internamente
 * - useNavigate requiere estar dentro de Router
 *
 * AuthProvider:
 * - Provee contexto de autenticación
 * - Disponible para todos los componentes hijos
 * - PrivateRoute, Navbar, páginas lo usan
 *
 * RUTAS:
 *
 * "/" → Navigate to="/login":
 * - Root redirect a login
 * - Usuario autenticado ya estará en /dashboard
 *
 * "/login" y "/register":
 * - Rutas públicas
 * - Sin Layout (full screen)
 * - Sin PrivateRoute
 *
 * "/dashboard", "/tasks", etc:
 * - Rutas protegidas
 * - Con <PrivateRoute> (verifica auth)
 * - Con <Layout> (navbar + content)
 *
 * "*" (catch-all):
 * - Cualquier ruta no definida
 * - Renderiza NotFoundPage
 * - Siempre al final de Routes
 *
 * @example
 * // Navegación en componentes:
 * import { useNavigate } from 'react-router-dom';
 *
 * const navigate = useNavigate();
 * navigate('/tasks'); // Programmatic navigation
 *
 * <Link to="/dashboard">Dashboard</Link> // Declarative navigation
 */
function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          {/* Root redirect */}
          <Route path="/" element={<Navigate to="/login" replace />} />

          {/* Public routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          {/* Protected routes */}
          <Route
            path="/dashboard"
            element={
              <PrivateRoute>
                <Layout>
                  <DashboardPage />
                </Layout>
              </PrivateRoute>
            }
          />

          <Route
            path="/tasks"
            element={
              <PrivateRoute>
                <Layout>
                  <TaskListPage />
                </Layout>
              </PrivateRoute>
            }
          />

          <Route
            path="/tasks/create"
            element={
              <PrivateRoute>
                <Layout>
                  <CreateTaskPage />
                </Layout>
              </PrivateRoute>
            }
          />

          <Route
            path="/tasks/:taskId/edit"
            element={
              <PrivateRoute>
                <Layout>
                  <EditTaskPage />
                </Layout>
              </PrivateRoute>
            }
          />

          {/* 404 route (catch-all) */}
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
