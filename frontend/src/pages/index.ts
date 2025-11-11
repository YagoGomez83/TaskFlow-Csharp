/**
 * Pages Index - Centralized exports
 *
 * EXPLICACIÓN:
 *
 * Este archivo exporta todos los componentes de página para facilitar imports:
 *
 * // Antes:
 * import LoginPage from './pages/LoginPage';
 * import RegisterPage from './pages/RegisterPage';
 * import DashboardPage from './pages/DashboardPage';
 *
 * // Ahora:
 * import { LoginPage, RegisterPage, DashboardPage } from './pages';
 *
 * VENTAJAS:
 * ✅ Imports más limpios
 * ✅ Single source of truth
 * ✅ Fácil refactoring (cambiar nombres en un lugar)
 * ✅ Tree shaking automático (Webpack/Vite eliminan código no usado)
 */

export { default as LoginPage } from './LoginPage';
export { default as RegisterPage } from './RegisterPage';
export { default as DashboardPage } from './DashboardPage';
export { default as TaskListPage } from './TaskListPage';
export { default as CreateTaskPage } from './CreateTaskPage';
export { default as EditTaskPage } from './EditTaskPage';
