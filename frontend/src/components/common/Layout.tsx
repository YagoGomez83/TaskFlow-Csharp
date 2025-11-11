/**
 * Layout Component - Wrapper for protected pages
 *
 * EXPLICACIÓN DEL LAYOUT:
 *
 * El Layout es un componente wrapper que proporciona estructura consistente
 * a todas las páginas protegidas.
 *
 * ESTRUCTURA:
 *
 * <Layout>
 *   <Navbar />           ← Barra de navegación superior
 *   <main>
 *     {children}         ← Contenido de la página
 *   </main>
 *   <Footer /> (optional) ← Pie de página (si se necesita)
 * </Layout>
 *
 * VENTAJAS:
 *
 * ✅ DRY (Don't Repeat Yourself):
 *    - Navbar definido una vez
 *    - No repetir en cada página
 *
 * ✅ Consistencia:
 *    - Todas las páginas tienen mismo layout
 *    - Spacing, padding consistentes
 *
 * ✅ Mantenibilidad:
 *    - Cambiar layout en un lugar
 *    - Actualiza todas las páginas
 *
 * ✅ Separación de concerns:
 *    - Layout maneja estructura
 *    - Páginas solo se preocupan por contenido
 *
 * USO:
 *
 * En App.tsx con React Router:
 *
 * <Route
 *   path="/dashboard"
 *   element={
 *     <PrivateRoute>
 *       <Layout>
 *         <DashboardPage />
 *       </Layout>
 *     </PrivateRoute>
 *   }
 * />
 *
 * PATRÓN COMÚN:
 *
 * Muchas apps tienen múltiples layouts:
 * - PublicLayout (sin navbar, para login/register)
 * - PrivateLayout (con navbar, para páginas protegidas)
 * - AdminLayout (con sidebar, para admin panel)
 *
 * Este proyecto usa:
 * - Sin layout → Login/Register (full screen)
 * - Con Layout → Dashboard/Tasks (con navbar)
 */

import type { ReactNode } from 'react';
import Navbar from './Navbar';
import './Layout.css';

/**
 * Props del Layout component.
 */
interface LayoutProps {
  /**
   * Contenido de la página a renderizar.
   */
  children: ReactNode;
}

/**
 * Layout component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Este es un "layout component":
 * - Proporciona estructura común
 * - Wrapper para páginas
 * - Incluye navegación global
 *
 * COMPOSICIÓN:
 * - Navbar (sticky top)
 * - Main content area (scrollable)
 * - Footer (opcional, comentado)
 *
 * RESPONSABILIDADES:
 * - Renderizar Navbar
 * - Proporcionar main content area
 * - Manejar spacing/padding global
 *
 * @example
 * // En App.tsx:
 * <Route
 *   path="/dashboard"
 *   element={
 *     <PrivateRoute>
 *       <Layout>
 *         <DashboardPage />
 *       </Layout>
 *     </PrivateRoute>
 *   }
 * />
 */
export function Layout({ children }: LayoutProps) {
  return (
    <div className="layout">
      {/* Navigation bar */}
      <Navbar />

      {/* Main content area */}
      <main className="layout__content">{children}</main>

      {/* Footer (opcional) */}
      {/* Descomentar si necesitas footer:
      <footer className="layout__footer">
        <p>&copy; 2025 TaskFlow. Todos los derechos reservados.</p>
      </footer>
      */}
    </div>
  );
}

export default Layout;
