/**
 * Card Component - Reusable container card
 *
 * EXPLICACIÓN DE COMPONENTES CARD:
 *
 * Los Cards son contenedores visuales que agrupan contenido relacionado.
 * Son fundamentales en UI moderno (Material Design, Bootstrap, Tailwind).
 *
 * USOS COMUNES:
 * ✅ Task items en una lista
 * ✅ User profiles
 * ✅ Product listings
 * ✅ Dashboard widgets
 * ✅ Forms sections
 *
 * CARACTERÍSTICAS:
 * - Fondo blanco
 * - Shadow para elevar del fondo
 * - Border radius para esquinas suaves
 * - Padding interno para contenido
 * - Opcional header y footer sections
 */

import React from 'react';
import './Card.css';

/**
 * Card variants (intensidad de shadow).
 *
 * EXPLICACIÓN:
 *
 * Diferentes shadows comunican diferentes niveles de "elevación":
 * - flat: Sin shadow, solo border (menos prominente)
 * - elevated: Shadow suave (uso general)
 * - floating: Shadow más pronunciado (destacar elementos importantes)
 */
export type CardVariant = 'flat' | 'elevated' | 'floating';

/**
 * Props del Card component.
 *
 * EXPLICACIÓN:
 *
 * Props propios:
 * - variant: Intensidad de shadow
 * - header: Contenido del header (opcional)
 * - footer: Contenido del footer (opcional)
 * - children: Contenido principal del card (body)
 * - padding: Tamaño del padding interno
 * - hoverable: Si debe tener efecto hover (útil para cards clickeables)
 * - onClick: Handler para click (hace al card interactivo)
 *
 * Extends HTMLAttributes:
 * - Permite pasar props HTML como className, data-*, etc.
 */
export interface CardProps extends Omit<React.HTMLAttributes<HTMLDivElement>, 'onClick'> {
  /**
   * Estilo visual del card (intensidad de shadow).
   * @default 'elevated'
   */
  variant?: CardVariant;

  /**
   * Contenido del header (opcional).
   * Típicamente: título, iconos, acciones.
   * @example <h3>Task Details</h3>
   */
  header?: React.ReactNode;

  /**
   * Contenido del footer (opcional).
   * Típicamente: acciones, metadata.
   * @example <div>Created: 2024-01-10</div>
   */
  footer?: React.ReactNode;

  /**
   * Contenido principal del card.
   */
  children: React.ReactNode;

  /**
   * Tamaño del padding interno.
   * @default 'md'
   */
  padding?: 'none' | 'sm' | 'md' | 'lg';

  /**
   * Si debe tener efecto hover (scale, shadow).
   * Útil para cards clickeables.
   * @default false
   */
  hoverable?: boolean;

  /**
   * Handler para click en el card.
   * Cuando está presente, el card es interactivo (cursor pointer).
   */
  onClick?: () => void;
}

/**
 * Card component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Un Card con tres secciones opcionales:
 * 1. Header → Título, iconos, acciones principales
 * 2. Body → Contenido principal (children)
 * 3. Footer → Metadata, acciones secundarias
 *
 * ESTRUCTURA:
 * .card
 *   ├── .card__header (opcional)
 *   ├── .card__body (children)
 *   └── .card__footer (opcional)
 *
 * INTERACTIVIDAD:
 *
 * - hoverable: Agrega efecto hover (scale, shadow)
 * - onClick: Hace al card clickeable
 * - role="button": Para accesibilidad si es clickeable
 * - tabIndex={0}: Permite navegar con teclado
 * - onKeyDown: Permite activar con Enter/Space
 *
 * PADDING:
 *
 * - 'none': Sin padding (útil para imágenes full-width)
 * - 'sm': Padding pequeño (espacios reducidos)
 * - 'md': Padding medio (uso general)
 * - 'lg': Padding grande (contenido importante)
 *
 * @example
 * // Basic card
 * <Card>
 *   <p>This is a simple card</p>
 * </Card>
 *
 * @example
 * // Card with header and footer
 * <Card
 *   header={<h3>Task Title</h3>}
 *   footer={<span>Due: 2024-12-31</span>}
 * >
 *   <p>Task description goes here</p>
 * </Card>
 *
 * @example
 * // Clickeable card with hover
 * <Card
 *   hoverable
 *   onClick={() => navigate('/task/123')}
 * >
 *   <p>Click to view details</p>
 * </Card>
 *
 * @example
 * // Card with no padding (for images)
 * <Card padding="none">
 *   <img src="/cover.jpg" alt="Cover" />
 *   <div style={{ padding: '1rem' }}>
 *     <h3>Title</h3>
 *     <p>Content</p>
 *   </div>
 * </Card>
 */
export function Card({
  variant = 'elevated',
  header,
  footer,
  children,
  padding = 'md',
  hoverable = false,
  onClick,
  className = '',
  ...rest
}: CardProps) {
  /**
   * Construir className dinámicamente.
   *
   * EXPLICACIÓN:
   * - 'card': Clase base
   * - `card--${variant}`: Variant class (card--elevated, etc.)
   * - `card--padding-${padding}`: Padding class
   * - 'card--hoverable': Si tiene efecto hover
   * - 'card--clickable': Si es clickeable
   * - className: Clases adicionales pasadas por props
   */
  const cardClassName = [
    'card',
    `card--${variant}`,
    `card--padding-${padding}`,
    hoverable && 'card--hoverable',
    onClick && 'card--clickable',
    className,
  ]
    .filter(Boolean)
    .join(' ');

  /**
   * Handler para keyboard events (accesibilidad).
   *
   * EXPLICACIÓN:
   * - Enter o Space activan onClick
   * - Permite usar el card con teclado
   * - Previene scroll al presionar Space
   */
  const handleKeyDown = (event: React.KeyboardEvent<HTMLDivElement>) => {
    if (onClick && (event.key === 'Enter' || event.key === ' ')) {
      event.preventDefault();
      onClick();
    }
  };

  return (
    <div
      className={cardClassName}
      onClick={onClick}
      onKeyDown={onClick ? handleKeyDown : undefined}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      {...rest}
    >
      {/* Header section (opcional) */}
      {header && <div className="card__header">{header}</div>}

      {/* Body section (children) */}
      <div className="card__body">{children}</div>

      {/* Footer section (opcional) */}
      {footer && <div className="card__footer">{footer}</div>}
    </div>
  );
}

export default Card;
