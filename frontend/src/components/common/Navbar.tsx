/**
 * Navbar Component - Global navigation bar
 *
 * EXPLICACIÓN DEL NAVBAR:
 *
 * El Navbar es un componente de navegación que aparece en todas las páginas protegidas.
 *
 * CARACTERÍSTICAS:
 * ✅ Logo/brand (TaskFlow)
 * ✅ Navigation links (Dashboard, Tasks)
 * ✅ User info (email)
 * ✅ Logout button
 * ✅ Active link highlighting
 * ✅ Responsive design (hamburger menu en mobile)
 *
 * POSICIONAMIENTO:
 *
 * Sticky navbar:
 * - position: sticky; top: 0;
 * - Se mantiene visible al hacer scroll
 * - z-index alto para estar sobre contenido
 *
 * NAVEGACIÓN:
 *
 * React Router Link component:
 * - Similar a <a> pero sin recargar página
 * - SPA navigation (Single Page Application)
 * - Más rápido que <a href>
 *
 * Active link styling:
 * - useLocation() hook obtiene ruta actual
 * - Comparar con link href
 * - Aplicar clase CSS si coincide
 *
 * RESPONSIVE:
 *
 * Desktop (>768px):
 * - Links horizontales
 * - Todos visibles
 *
 * Mobile (<768px):
 * - Hamburger menu (☰)
 * - Links en dropdown
 * - Toggle con useState
 */

import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import Button from './Button';
import './Navbar.css';

/**
 * Navbar component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Este es un "layout component":
 * - Aparece en múltiples páginas
 * - Maneja navegación global
 * - Conecta con AuthContext
 * - Responsive behavior
 *
 * ESTADO:
 * - isMenuOpen: Toggle del mobile menu
 *
 * HOOKS USADOS:
 * - useAuth(): Obtener user y logout
 * - useLocation(): Obtener ruta actual para active links
 * - useState(): Manejar mobile menu toggle
 *
 * @example
 * // En Layout component:
 * <Layout>
 *   <Navbar />
 *   <main>{children}</main>
 * </Layout>
 */
export function Navbar() {
  /**
   * Context de autenticación.
   */
  const { user, logout } = useAuth();

  /**
   * Ruta actual (para active links).
   *
   * EXPLICACIÓN:
   *
   * useLocation() hook de React Router:
   * - Retorna objeto con info de la ruta actual
   * - location.pathname → "/dashboard", "/tasks", etc.
   * - Re-renderiza cuando ruta cambia
   */
  const location = useLocation();

  /**
   * Estado del mobile menu (abierto/cerrado).
   */
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  /**
   * Verificar si link está activo.
   *
   * EXPLICACIÓN:
   *
   * Comparar pathname actual con href del link.
   * Si coincide → link activo → aplicar clase CSS.
   *
   * Casos especiales:
   * - href="/tasks" coincide con "/tasks/123/edit" (startsWith)
   * - href="/" solo coincide exacto (evitar que siempre esté activo)
   */
  const isActive = (path: string): boolean => {
    if (path === '/') {
      return location.pathname === path;
    }
    return location.pathname.startsWith(path);
  };

  /**
   * Handler para logout.
   *
   * EXPLICACIÓN:
   *
   * 1. Llamar logout del AuthContext
   * 2. Context limpia tokens de localStorage
   * 3. Context set user = null
   * 4. PrivateRoute detecta !isAuthenticated
   * 5. Redirect a /login
   */
  const handleLogout = () => {
    logout();
    setIsMenuOpen(false); // Cerrar menu mobile
  };

  /**
   * Toggle mobile menu.
   */
  const toggleMenu = () => {
    setIsMenuOpen((prev) => !prev);
  };

  /**
   * Cerrar menu mobile al hacer click en link.
   */
  const closeMenu = () => {
    setIsMenuOpen(false);
  };

  return (
    <nav className="navbar">
      <div className="navbar__container">
        {/* Logo/Brand */}
        <Link to="/dashboard" className="navbar__brand" onClick={closeMenu}>
          <span className="navbar__logo">✓</span>
          <span className="navbar__name">TaskFlow</span>
        </Link>

        {/* Hamburger button (mobile) */}
        <button
          className="navbar__hamburger"
          onClick={toggleMenu}
          aria-label="Toggle menu"
          aria-expanded={isMenuOpen}
        >
          <span className="navbar__hamburger-line"></span>
          <span className="navbar__hamburger-line"></span>
          <span className="navbar__hamburger-line"></span>
        </button>

        {/* Navigation links */}
        <div
          className={`navbar__menu ${
            isMenuOpen ? 'navbar__menu--open' : ''
          }`}
        >
          {/* Links */}
          <div className="navbar__links">
            <Link
              to="/dashboard"
              className={`navbar__link ${
                isActive('/dashboard') ? 'navbar__link--active' : ''
              }`}
              onClick={closeMenu}
            >
              Dashboard
            </Link>
            <Link
              to="/tasks"
              className={`navbar__link ${
                isActive('/tasks') ? 'navbar__link--active' : ''
              }`}
              onClick={closeMenu}
            >
              Mis Tareas
            </Link>
          </div>

          {/* User info & logout */}
          <div className="navbar__user">
            <span className="navbar__user-email">{user?.email}</span>
            <Button variant="secondary" size="sm" onClick={handleLogout}>
              Cerrar Sesión
            </Button>
          </div>
        </div>
      </div>
    </nav>
  );
}

export default Navbar;
