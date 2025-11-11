/**
 * DashboardPage Component - Main dashboard/home page
 *
 * EXPLICACIÓN DEL DASHBOARD:
 *
 * Esta es la página principal después del login.
 * Muestra un resumen de las tareas del usuario.
 *
 * CARACTERÍSTICAS:
 * ✅ Saludo personalizado con nombre de usuario
 * ✅ Estadísticas de tareas (total, pendientes, en progreso, completadas)
 * ✅ Vista rápida de tareas recientes
 * ✅ Links de navegación rápida
 * ✅ Botón para crear nueva tarea
 *
 * DATOS MOSTRADOS:
 *
 * 1. Header:
 *    - Saludo: "Hola, {user.email}"
 *    - Botón "Nueva Tarea"
 *
 * 2. Estadísticas (Cards):
 *    - Total de tareas
 *    - Tareas pendientes
 *    - Tareas en progreso
 *    - Tareas completadas
 *
 * 3. Tareas Recientes:
 *    - Últimas 5 tareas del usuario
 *    - Link "Ver todas" → /tasks
 *
 * 4. Quick Actions:
 *    - Ver todas las tareas
 *    - Crear nueva tarea
 *    - Filtrar por prioridad
 *
 * NOTA SOBRE DATOS:
 *
 * Esta versión muestra datos dummy (mock data).
 * En producción, se obtienen del backend con React Query:
 *
 * const { data: stats } = useQuery('task-stats', fetchTaskStats);
 * const { data: recentTasks } = useQuery('recent-tasks', fetchRecentTasks);
 */

import { useAuth } from '../contexts/AuthContext';
import Button from '../components/common/Button';
import Card from '../components/common/Card';
import './DashboardPage.css';

/**
 * DashboardPage component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Este es un "dashboard component":
 * - Muestra resumen/overview de la app
 * - Combina múltiples widgets/cards
 * - Provee navegación rápida
 * - Punto de entrada principal después de login
 *
 * ESTRUCTURA:
 * - Header con saludo
 * - Stats cards (4 tarjetas con números)
 * - Sección de tareas recientes
 * - Quick actions
 *
 * @example
 * // En App.tsx con React Router:
 * <Route path="/dashboard" element={<DashboardPage />} />
 */
export function DashboardPage() {
  /**
   * Usuario autenticado del context.
   */
  const { user } = useAuth();

  /**
   * Datos dummy para estadísticas.
   *
   * EXPLICACIÓN:
   *
   * En producción, estos datos vendrían del backend:
   *
   * GET /api/tasks/stats
   * Response:
   * {
   *   total: 25,
   *   pending: 10,
   *   inProgress: 8,
   *   completed: 7
   * }
   */
  const stats = {
    total: 0,
    pending: 0,
    inProgress: 0,
    completed: 0,
  };

  /**
   * Navegar a página (dummy - será reemplazado por React Router).
   */
  const navigate = (path: string) => {
    // TODO: Implementar con React Router
    console.log('Navegar a:', path);
    window.location.href = path;
  };

  return (
    <div className="dashboard-page">
      {/* Header */}
      <div className="dashboard-page__header">
        <div>
          <h1 className="dashboard-page__title">
            Hola, {user?.email.split('@')[0]} 👋
          </h1>
          <p className="dashboard-page__subtitle">
            Aquí está el resumen de tus tareas
          </p>
        </div>
        <Button
          variant="primary"
          size="lg"
          onClick={() => navigate('/tasks/create')}
        >
          + Nueva Tarea
        </Button>
      </div>

      {/* Stats Cards */}
      <div className="dashboard-page__stats">
        {/* Total tareas */}
        <Card variant="elevated" padding="md">
          <div className="dashboard-page__stat-card">
            <div className="dashboard-page__stat-icon dashboard-page__stat-icon--total">
              📊
            </div>
            <div>
              <p className="dashboard-page__stat-label">Total de Tareas</p>
              <p className="dashboard-page__stat-value">{stats.total}</p>
            </div>
          </div>
        </Card>

        {/* Pendientes */}
        <Card variant="elevated" padding="md">
          <div className="dashboard-page__stat-card">
            <div className="dashboard-page__stat-icon dashboard-page__stat-icon--pending">
              ⏳
            </div>
            <div>
              <p className="dashboard-page__stat-label">Pendientes</p>
              <p className="dashboard-page__stat-value">{stats.pending}</p>
            </div>
          </div>
        </Card>

        {/* En progreso */}
        <Card variant="elevated" padding="md">
          <div className="dashboard-page__stat-card">
            <div className="dashboard-page__stat-icon dashboard-page__stat-icon--progress">
              🔄
            </div>
            <div>
              <p className="dashboard-page__stat-label">En Progreso</p>
              <p className="dashboard-page__stat-value">{stats.inProgress}</p>
            </div>
          </div>
        </Card>

        {/* Completadas */}
        <Card variant="elevated" padding="md">
          <div className="dashboard-page__stat-card">
            <div className="dashboard-page__stat-icon dashboard-page__stat-icon--completed">
              ✅
            </div>
            <div>
              <p className="dashboard-page__stat-label">Completadas</p>
              <p className="dashboard-page__stat-value">{stats.completed}</p>
            </div>
          </div>
        </Card>
      </div>

      {/* Contenido principal */}
      <div className="dashboard-page__content">
        {/* Sección: Tareas recientes */}
        <Card variant="elevated" padding="lg">
          <div className="dashboard-page__section">
            <div className="dashboard-page__section-header">
              <h2 className="dashboard-page__section-title">
                Tareas Recientes
              </h2>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => navigate('/tasks')}
              >
                Ver todas →
              </Button>
            </div>

            {stats.total === 0 ? (
              <div className="dashboard-page__empty">
                <div className="dashboard-page__empty-icon">📝</div>
                <p className="dashboard-page__empty-text">
                  No tienes tareas aún
                </p>
                <Button
                  variant="primary"
                  onClick={() => navigate('/tasks/create')}
                >
                  Crear tu primera tarea
                </Button>
              </div>
            ) : (
              <div className="dashboard-page__tasks">
                <p className="dashboard-page__info">
                  Tus tareas aparecerán aquí. Haz clic en "Ver todas" para
                  gestionar tus tareas.
                </p>
              </div>
            )}
          </div>
        </Card>

        {/* Sección: Quick Actions */}
        <Card variant="elevated" padding="lg">
          <div className="dashboard-page__section">
            <h2 className="dashboard-page__section-title">Acciones Rápidas</h2>
            <div className="dashboard-page__quick-actions">
              <button
                className="dashboard-page__quick-action"
                onClick={() => navigate('/tasks')}
              >
                <span className="dashboard-page__quick-action-icon">📋</span>
                <span className="dashboard-page__quick-action-label">
                  Ver todas las tareas
                </span>
              </button>

              <button
                className="dashboard-page__quick-action"
                onClick={() => navigate('/tasks/create')}
              >
                <span className="dashboard-page__quick-action-icon">➕</span>
                <span className="dashboard-page__quick-action-label">
                  Crear nueva tarea
                </span>
              </button>

              <button
                className="dashboard-page__quick-action"
                onClick={() => navigate('/tasks?priority=high')}
              >
                <span className="dashboard-page__quick-action-icon">🔥</span>
                <span className="dashboard-page__quick-action-label">
                  Ver alta prioridad
                </span>
              </button>

              <button
                className="dashboard-page__quick-action"
                onClick={() => navigate('/tasks?status=pending')}
              >
                <span className="dashboard-page__quick-action-icon">⏰</span>
                <span className="dashboard-page__quick-action-label">
                  Ver pendientes
                </span>
              </button>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
}

export default DashboardPage;
