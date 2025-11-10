/**
 * Alert Component - Reusable notification/message component
 *
 * EXPLICACIÓN DE COMPONENTES ALERT:
 *
 * Los Alerts comunican mensajes importantes al usuario:
 * ✅ Success: Operación exitosa (verde)
 * ✅ Error: Algo salió mal (rojo)
 * ✅ Warning: Advertencia (amarillo/naranja)
 * ✅ Info: Información general (azul)
 *
 * USOS COMUNES:
 * - Feedback después de submit form
 * - Error messages de API
 * - Warnings de validación
 * - Info sobre el estado de la app
 *
 * CARACTERÍSTICAS:
 * - Color coding por tipo (success, error, warning, info)
 * - Icono opcional para reforzar el mensaje
 * - Botón de cierre (dismissible)
 * - Accesibilidad con role="alert"
 */

import React from 'react';
import './Alert.css';

/**
 * Alert variants (tipos de mensaje).
 *
 * EXPLICACIÓN:
 *
 * Cada variant tiene un color específico:
 * - success: Verde (✅ operación exitosa)
 * - error: Rojo (❌ error crítico)
 * - warning: Amarillo/Naranja (⚠️ advertencia)
 * - info: Azul (ℹ️ información)
 */
export type AlertVariant = 'success' | 'error' | 'warning' | 'info';

/**
 * Props del Alert component.
 *
 * EXPLICACIÓN:
 *
 * Props propios:
 * - variant: Tipo de mensaje (success, error, warning, info)
 * - title: Título del alert (opcional, en bold)
 * - children: Contenido del mensaje
 * - dismissible: Si se puede cerrar (muestra botón X)
 * - onDismiss: Callback cuando se cierra
 */
export interface AlertProps {
  /**
   * Tipo de mensaje.
   * @default 'info'
   */
  variant?: AlertVariant;

  /**
   * Título del alert (opcional).
   * Se muestra en bold arriba del mensaje.
   * @example "Success!"
   */
  title?: string;

  /**
   * Contenido del mensaje.
   * Puede ser texto o JSX.
   */
  children: React.ReactNode;

  /**
   * Si el alert se puede cerrar.
   * Muestra botón X y permite llamar onDismiss.
   * @default false
   */
  dismissible?: boolean;

  /**
   * Callback cuando el usuario cierra el alert.
   * Solo se llama si dismissible es true.
   */
  onDismiss?: () => void;
}

/**
 * Alert component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Un Alert con:
 * 1. Icono visual según el variant
 * 2. Título opcional (bold)
 * 3. Mensaje (children)
 * 4. Botón de cierre opcional
 *
 * ICONOS:
 *
 * Usamos emojis como iconos simples:
 * - ✅ Success
 * - ❌ Error
 * - ⚠️ Warning
 * - ℹ️ Info
 *
 * En producción, considerar usar una librería de iconos:
 * - Heroicons
 * - Feather Icons
 * - React Icons
 *
 * ACCESIBILIDAD:
 *
 * - role="alert" → Screenreader anuncia el mensaje automáticamente
 * - aria-live="assertive" → Alta prioridad para errors
 * - aria-live="polite" → Baja prioridad para info
 * - aria-label en botón close → "Close alert"
 *
 * DISMISSIBLE:
 *
 * - Si dismissible={true}, muestra botón X
 * - onClick llama onDismiss
 * - El padre maneja el estado (mostrar/ocultar)
 *
 * @example
 * // Success alert
 * <Alert variant="success">
 *   Task created successfully!
 * </Alert>
 *
 * @example
 * // Error alert with title
 * <Alert variant="error" title="Error">
 *   Failed to save task. Please try again.
 * </Alert>
 *
 * @example
 * // Dismissible alert
 * const [showAlert, setShowAlert] = useState(true);
 *
 * {showAlert && (
 *   <Alert
 *     variant="warning"
 *     dismissible
 *     onDismiss={() => setShowAlert(false)}
 *   >
 *     Your session will expire in 5 minutes.
 *   </Alert>
 * )}
 *
 * @example
 * // Info alert with custom content
 * <Alert variant="info" title="Pro Tip">
 *   Press <kbd>Ctrl+K</kbd> to open quick search.
 * </Alert>
 */
export function Alert({
  variant = 'info',
  title,
  children,
  dismissible = false,
  onDismiss,
}: AlertProps) {
  /**
   * Mapeo de variants a iconos.
   *
   * EXPLICACIÓN:
   * - Record<AlertVariant, string> → Objeto con keys de AlertVariant
   * - TypeScript asegura que todos los variants tienen icono
   */
  const icons: Record<AlertVariant, string> = {
    success: '✅',
    error: '❌',
    warning: '⚠️',
    info: 'ℹ️',
  };

  /**
   * Construir className dinámicamente.
   */
  const className = ['alert', `alert--${variant}`].filter(Boolean).join(' ');

  /**
   * aria-live determina la prioridad del anuncio.
   *
   * EXPLICACIÓN:
   * - assertive: Alta prioridad (interrumpe al screenreader)
   * - polite: Baja prioridad (espera a que termine de hablar)
   *
   * Errors son assertive, resto polite.
   */
  const ariaLive = variant === 'error' ? 'assertive' : 'polite';

  return (
    <div className={className} role="alert" aria-live={ariaLive}>
      {/* Icon */}
      <div className="alert__icon" aria-hidden="true">
        {icons[variant]}
      </div>

      {/* Content */}
      <div className="alert__content">
        {/* Title (opcional) */}
        {title && <div className="alert__title">{title}</div>}

        {/* Message */}
        <div className="alert__message">{children}</div>
      </div>

      {/* Close button (si dismissible) */}
      {dismissible && onDismiss && (
        <button
          type="button"
          className="alert__close"
          onClick={onDismiss}
          aria-label="Close alert"
        >
          ✕
        </button>
      )}
    </div>
  );
}

export default Alert;
