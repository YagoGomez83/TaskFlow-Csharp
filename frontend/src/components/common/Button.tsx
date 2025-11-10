/**
 * Button Component - Reusable button with variants and states
 *
 * EXPLICACIÓN DE COMPONENTES REUTILIZABLES:
 *
 * Componentes reutilizables son bloques de UI que se pueden usar en múltiples lugares.
 *
 * VENTAJAS:
 * ✅ Consistencia - Mismo estilo en toda la app
 * ✅ Mantenibilidad - Cambiar en un lugar actualiza todos los usos
 * ✅ Productividad - No reescribir botones cada vez
 * ✅ Testing - Testear una vez, usar en todas partes
 *
 * CARACTERÍSTICAS DE UN BUEN COMPONENTE:
 * 1. Flexible - Props para customizar
 * 2. Accesible - ARIA labels, keyboard support
 * 3. Type-safe - TypeScript para prevenir errores
 * 4. Documentado - Explicar cómo usar
 */

import React from 'react';
import './Button.css';

/**
 * Button variants (estilos visuales).
 *
 * EXPLICACIÓN:
 *
 * Variants definen diferentes estilos del botón:
 * - primary: Acción principal (azul, destacado)
 * - secondary: Acción secundaria (gris)
 * - danger: Acción destructiva (rojo, ej: eliminar)
 * - ghost: Sin fondo, solo texto
 */
export type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost';

/**
 * Button sizes.
 *
 * EXPLICACIÓN:
 *
 * Sizes para diferentes contextos:
 * - sm: Small, para espacios reducidos
 * - md: Medium, tamaño por defecto
 * - lg: Large, para CTAs importantes
 */
export type ButtonSize = 'sm' | 'md' | 'lg';

/**
 * Props del Button component.
 *
 * EXPLICACIÓN DE EXTENDS:
 *
 * Extends React.ButtonHTMLAttributes<HTMLButtonElement>:
 * - Hereda TODOS los props nativos de <button>
 * - onClick, disabled, type, aria-*, etc.
 * - No necesitamos redefinirlos
 * - TypeScript los autocompleta
 *
 * Omit<..., 'className'>:
 * - Removemos className de los props heredados
 * - Porque manejamos className internamente
 * - Previene que usuarios sobrescriban estilos
 *
 * Props propios:
 * - variant: Estilo visual
 * - size: Tamaño
 * - isLoading: Mostrar spinner
 * - fullWidth: Ocupar 100% del ancho
 */
export interface ButtonProps
  extends Omit<React.ButtonHTMLAttributes<HTMLButtonElement>, 'className'> {
  /**
   * Estilo visual del botón.
   * @default 'primary'
   */
  variant?: ButtonVariant;

  /**
   * Tamaño del botón.
   * @default 'md'
   */
  size?: ButtonSize;

  /**
   * Mostrar spinner y deshabilitar.
   * Útil durante operaciones async (ej: submit form).
   * @default false
   */
  isLoading?: boolean;

  /**
   * Ocupar 100% del ancho del contenedor.
   * @default false
   */
  fullWidth?: boolean;

  /**
   * Contenido del botón (texto, iconos, etc).
   */
  children: React.ReactNode;
}

/**
 * Button component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Este componente envuelve <button> nativo y agrega:
 * 1. Estilos consistentes (variants, sizes)
 * 2. Loading state con spinner
 * 3. Disabled automático cuando isLoading
 * 4. Accesibilidad (aria-disabled, aria-busy)
 * 5. TypeScript para prevenir errores
 *
 * PROPS FORWARDING:
 *
 * {...rest} pasa todos los props no destructurados al <button>:
 * - onClick, onMouseEnter, aria-label, etc.
 * - Permite usar Button como <button> nativo
 * - Máxima flexibilidad
 *
 * DISABLED STATE:
 *
 * disabled = isLoading || rest.disabled:
 * - Auto-deshabilita durante loading
 * - También deshabilita si prop disabled está seteado
 * - Previene múltiples clicks
 *
 * ARIA ATTRIBUTES:
 *
 * aria-disabled:
 * - Screenreaders saben que está deshabilitado
 * - Mejor que solo disabled (HTML)
 *
 * aria-busy:
 * - Indica operación en progreso
 * - Screenreader anuncia "busy" durante loading
 *
 * @example
 * // Primary button
 * <Button onClick={handleClick}>
 *   Click me
 * </Button>
 *
 * @example
 * // Loading state
 * <Button isLoading={isSubmitting}>
 *   {isSubmitting ? 'Submitting...' : 'Submit'}
 * </Button>
 *
 * @example
 * // Danger button
 * <Button variant="danger" onClick={handleDelete}>
 *   Delete
 * </Button>
 *
 * @example
 * // Full width button
 * <Button fullWidth size="lg">
 *   Continue
 * </Button>
 */
export function Button({
  variant = 'primary',
  size = 'md',
  isLoading = false,
  fullWidth = false,
  children,
  ...rest
}: ButtonProps) {
  /**
   * Construir className dinámicamente.
   *
   * EXPLICACIÓN:
   *
   * Combinamos múltiples clases CSS:
   * - 'button': Clase base con estilos comunes
   * - `button--${variant}`: Clase de variant (button--primary, etc.)
   * - `button--${size}`: Clase de size (button--md, etc.)
   * - 'button--full-width': Condicional si fullWidth
   * - 'button--loading': Condicional si isLoading
   *
   * Template literals para interpolar valores:
   * `button--${variant}` → "button--primary"
   *
   * Ternary para condicionales:
   * fullWidth ? 'button--full-width' : ''
   * Si fullWidth es true, agrega la clase, si no, string vacío.
   *
   * .filter(Boolean):
   * - Remueve strings vacíos del array
   * - ['button', '', 'button--md'] → ['button', 'button--md']
   *
   * .join(' '):
   * - Une array en string con espacios
   * - ['button', 'button--md'] → "button button--md"
   */
  const className = [
    'button',
    `button--${variant}`,
    `button--${size}`,
    fullWidth && 'button--full-width',
    isLoading && 'button--loading',
  ]
    .filter(Boolean)
    .join(' ');

  /**
   * Deshabilitar si está loading O si prop disabled está seteado.
   *
   * EXPLICACIÓN:
   *
   * || es OR lógico:
   * - Si isLoading es true → disabled
   * - O si rest.disabled es true → disabled
   * - Si ambos false → no disabled
   */
  const disabled = isLoading || rest.disabled;

  return (
    <button
      {...rest}
      className={className}
      disabled={disabled}
      aria-disabled={disabled}
      aria-busy={isLoading}
    >
      {isLoading && (
        <span className="button__spinner" aria-hidden="true">
          ⏳
        </span>
      )}
      <span className={isLoading ? 'button__text--loading' : ''}>
        {children}
      </span>
    </button>
  );
}

export default Button;
