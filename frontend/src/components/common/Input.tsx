/**
 * Input Component - Reusable input field with validation states
 *
 * EXPLICACIÓN DE COMPONENTES DE FORMULARIO:
 *
 * Los inputs son fundamentales en cualquier aplicación web.
 * Este componente encapsula un <input> nativo con:
 * ✅ Estilos consistentes
 * ✅ Estados de validación (error, success)
 * ✅ Label y helper text integrados
 * ✅ Accesibilidad (aria-*, for, id)
 * ✅ TypeScript para type safety
 *
 * ARQUITECTURA:
 * - Input wrapper → Contenedor del grupo completo
 * - Label → Etiqueta descriptiva
 * - Input → Campo de entrada
 * - Helper text → Texto de ayuda o error
 * - Error message → Mensaje de error específico
 */

import React from 'react';
import './Input.css';

/**
 * Input types (tipos de input HTML).
 *
 * EXPLICACIÓN:
 *
 * Restringimos a los tipos más comunes para mejor UX:
 * - text: Texto general
 * - email: Email con validación nativa del navegador
 * - password: Password oculto
 * - number: Solo números
 * - date: Selector de fecha
 * - tel: Teléfono
 * - url: URL con validación
 */
export type InputType = 'text' | 'email' | 'password' | 'number' | 'date' | 'tel' | 'url';

/**
 * Props del Input component.
 *
 * EXPLICACIÓN DE EXTENDS:
 *
 * Extends React.InputHTMLAttributes<HTMLInputElement>:
 * - Hereda TODOS los props nativos de <input>
 * - placeholder, maxLength, pattern, autoComplete, etc.
 * - onChange, onBlur, onFocus, etc.
 *
 * Omit<..., 'className' | 'id'>:
 * - Removemos className porque lo manejamos internamente
 * - Removemos id porque lo generamos automáticamente
 *
 * Props propios:
 * - label: Etiqueta descriptiva (ej: "Email")
 * - error: Mensaje de error de validación
 * - helperText: Texto de ayuda
 * - fullWidth: Ocupar 100% del ancho
 */
export interface InputProps
  extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'className' | 'id'> {
  /**
   * Etiqueta descriptiva del input.
   * Se muestra arriba del campo.
   * @example "Email Address"
   */
  label?: string;

  /**
   * Mensaje de error de validación.
   * Cuando está presente, el input muestra estado de error (rojo).
   * @example "Email is required"
   */
  error?: string;

  /**
   * Texto de ayuda que aparece debajo del input.
   * Útil para dar contexto adicional.
   * @example "We'll never share your email"
   */
  helperText?: string;

  /**
   * Ocupar 100% del ancho del contenedor.
   * @default false
   */
  fullWidth?: boolean;
}

/**
 * Input component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Este componente envuelve <input> nativo y agrega:
 * 1. Label asociado con htmlFor/id para accesibilidad
 * 2. Estados de error/success con estilos visuales
 * 3. Helper text para guiar al usuario
 * 4. Error message prominente cuando hay validación fallida
 * 5. IDs únicos autogenerados
 *
 * GENERACIÓN DE ID ÚNICO:
 *
 * usamos React.useId() (React 18+) para generar IDs únicos:
 * - Previene colisiones de IDs en la página
 * - Necesario para asociar label con input (accesibilidad)
 * - Funciona con Server Side Rendering (SSR)
 *
 * ACCESIBILIDAD:
 *
 * - htmlFor conecta label con input
 * - aria-invalid indica error al screenreader
 * - aria-describedby conecta input con helper/error text
 * - required prop agrega * visual al label
 *
 * ESTADOS:
 *
 * - Normal: Borde gris, sin decoración
 * - Focus: Borde azul, outline visible
 * - Error: Borde rojo, mensaje de error rojo
 * - Disabled: Opacity reducida, cursor not-allowed
 *
 * @example
 * // Basic input
 * <Input
 *   label="Email"
 *   type="email"
 *   placeholder="your@email.com"
 * />
 *
 * @example
 * // Input with error
 * <Input
 *   label="Password"
 *   type="password"
 *   error="Password must be at least 8 characters"
 * />
 *
 * @example
 * // Input with helper text
 * <Input
 *   label="Username"
 *   helperText="Only letters, numbers, and underscores"
 *   maxLength={20}
 * />
 *
 * @example
 * // Full width input
 * <Input
 *   label="Full Name"
 *   fullWidth
 *   required
 * />
 */
export function Input({
  label,
  error,
  helperText,
  fullWidth = false,
  type = 'text',
  required,
  disabled,
  ...rest
}: InputProps) {
  /**
   * Generar ID único para asociar label con input.
   *
   * EXPLICACIÓN:
   *
   * React.useId() genera un ID único por componente:
   * - ":r1:" (ejemplo)
   * - Único incluso si renderizas múltiples <Input> en la misma página
   * - Compatible con SSR (mismo ID en server y client)
   */
  const id = React.useId();

  /**
   * ID para helper/error text.
   * Usado en aria-describedby para accesibilidad.
   */
  const helperId = `${id}-helper`;

  /**
   * Construir className dinámicamente.
   *
   * EXPLICACIÓN:
   *
   * - 'input__field': Clase base del input
   * - 'input__field--error': Si hay error
   * - 'input__field--full-width': Si fullWidth es true
   */
  const inputClassName = [
    'input__field',
    error && 'input__field--error',
    fullWidth && 'input__field--full-width',
  ]
    .filter(Boolean)
    .join(' ');

  /**
   * Determinar si mostrar helper text o error.
   * Error tiene prioridad sobre helper text.
   */
  const showHelperText = error || helperText;

  return (
    <div className={`input ${fullWidth ? 'input--full-width' : ''}`}>
      {/* Label */}
      {label && (
        <label htmlFor={id} className="input__label">
          {label}
          {required && <span className="input__required" aria-label="required">*</span>}
        </label>
      )}

      {/* Input field */}
      <input
        id={id}
        type={type}
        className={inputClassName}
        disabled={disabled}
        required={required}
        aria-invalid={error ? 'true' : 'false'}
        aria-describedby={showHelperText ? helperId : undefined}
        {...rest}
      />

      {/* Helper text or error message */}
      {showHelperText && (
        <div
          id={helperId}
          className={`input__helper ${error ? 'input__helper--error' : ''}`}
        >
          {error || helperText}
        </div>
      )}
    </div>
  );
}

export default Input;
