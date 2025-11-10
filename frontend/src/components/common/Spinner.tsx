/**
 * Spinner Component - Loading indicator
 *
 * EXPLICACIÓN DE COMPONENTES SPINNER:
 *
 * Los Spinners comunican que algo está cargando:
 * ✅ Operaciones async (fetch API)
 * ✅ Submit forms
 * ✅ Carga inicial de página
 * ✅ Lazy loading de componentes
 *
 * TIPOS DE SPINNERS:
 * - Spinner: Solo icono girando
 * - Overlay: Cubre toda la pantalla con backdrop
 * - Inline: Dentro de un componente específico
 *
 * ACCESIBILIDAD:
 * - role="status" → Indica loading state
 * - aria-live="polite" → Anuncia cambios
 * - aria-label → Describe qué está cargando
 */

import React from 'react';
import './Spinner.css';

/**
 * Spinner sizes.
 *
 * EXPLICACIÓN:
 *
 * Tamaños para diferentes contextos:
 * - sm: Pequeño (dentro de botones, inline text)
 * - md: Mediano (uso general)
 * - lg: Grande (full page loading)
 * - xl: Extra grande (splash screens)
 */
export type SpinnerSize = 'sm' | 'md' | 'lg' | 'xl';

/**
 * Props del Spinner component.
 *
 * EXPLICACIÓN:
 *
 * Props propios:
 * - size: Tamaño del spinner
 * - label: Texto descriptivo debajo del spinner
 * - overlay: Si debe cubrir toda la pantalla con backdrop
 * - color: Color del spinner (por defecto: primary)
 */
export interface SpinnerProps {
  /**
   * Tamaño del spinner.
   * @default 'md'
   */
  size?: SpinnerSize;

  /**
   * Texto descriptivo debajo del spinner.
   * @example "Loading tasks..."
   */
  label?: string;

  /**
   * Si debe cubrir toda la pantalla con backdrop oscuro.
   * Útil para loading states que bloquean la UI.
   * @default false
   */
  overlay?: boolean;

  /**
   * Color del spinner.
   * @default 'primary'
   */
  color?: 'primary' | 'white' | 'gray';
}

/**
 * Spinner component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Un spinner animado con:
 * 1. Círculo girando (CSS animation)
 * 2. Label opcional debajo
 * 3. Overlay opcional para full page loading
 *
 * IMPLEMENTACIÓN:
 *
 * El spinner es un <div> con border y border-top de color diferente.
 * La animación 'spin' rota el elemento 360 grados continuamente.
 *
 * OVERLAY MODE:
 *
 * Cuando overlay={true}:
 * - Cubre toda la pantalla (fixed position)
 * - Backdrop oscuro semi-transparente
 * - Spinner centrado
 * - z-index alto para estar sobre todo
 *
 * ACCESIBILIDAD:
 *
 * - role="status" → Screenreader sabe que es loading state
 * - aria-live="polite" → Anuncia cambios sin interrumpir
 * - aria-label → Describe qué está cargando
 *
 * @example
 * // Basic spinner
 * <Spinner />
 *
 * @example
 * // Spinner with label
 * <Spinner label="Loading tasks..." />
 *
 * @example
 * // Full page loading
 * <Spinner
 *   overlay
 *   size="lg"
 *   label="Loading application..."
 * />
 *
 * @example
 * // Small inline spinner
 * <div>
 *   Processing <Spinner size="sm" />
 * </div>
 *
 * @example
 * // White spinner (para fondos oscuros)
 * <div style={{ background: '#333' }}>
 *   <Spinner color="white" />
 * </div>
 */
export function Spinner({
  size = 'md',
  label,
  overlay = false,
  color = 'primary',
}: SpinnerProps) {
  /**
   * Construir className del spinner.
   */
  const spinnerClassName = [
    'spinner',
    `spinner--${size}`,
    `spinner--${color}`,
  ]
    .filter(Boolean)
    .join(' ');

  /**
   * Spinner element.
   */
  const spinnerElement = (
    <div
      className="spinner-container"
      role="status"
      aria-live="polite"
      aria-label={label || 'Loading'}
    >
      {/* Spinning circle */}
      <div className={spinnerClassName} />

      {/* Label (opcional) */}
      {label && <div className="spinner__label">{label}</div>}
    </div>
  );

  /**
   * Si overlay={true}, envolver en backdrop full page.
   */
  if (overlay) {
    return <div className="spinner-overlay">{spinnerElement}</div>;
  }

  return spinnerElement;
}

export default Spinner;
