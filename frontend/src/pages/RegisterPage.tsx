/**
 * RegisterPage Component - User registration page
 *
 * EXPLICACIÓN DE LA PÁGINA DE REGISTRO:
 *
 * Esta página permite a nuevos usuarios crear una cuenta.
 *
 * CARACTERÍSTICAS:
 * ✅ Formulario de registro (email + password + confirmPassword)
 * ✅ Validación de contraseña fuerte
 * ✅ Confirmación de contraseña
 * ✅ Integración con AuthContext
 * ✅ Auto-login después de registro exitoso
 * ✅ Manejo de errores del backend
 * ✅ Loading states
 * ✅ Link a página de login
 *
 * FLUJO DE REGISTRO:
 *
 * 1. Usuario ingresa email, password, confirmPassword
 * 2. Submit → handleRegister() ejecuta
 * 3. Validación frontend:
 *    - Email válido
 *    - Password fuerte (8+ chars, mayúscula, minúscula, número, especial)
 *    - Password y confirmPassword coinciden
 * 4. Si válido → authContext.register(userData)
 * 5. AuthContext llama authService.register() → POST /api/auth/register
 * 6. Backend:
 *    - Valida datos con FluentValidation
 *    - Hashea password con BCrypt
 *    - Crea usuario en DB
 *    - Retorna tokens (auto-login)
 * 7. Tokens guardados en localStorage
 * 8. User state actualizado en AuthContext
 * 9. Navegación a /dashboard
 *
 * VALIDACIÓN DE PASSWORD:
 *
 * Requisitos (sincronizados con backend):
 * - Mínimo 8 caracteres
 * - Al menos 1 mayúscula (A-Z)
 * - Al menos 1 minúscula (a-z)
 * - Al menos 1 dígito (0-9)
 * - Al menos 1 carácter especial (!@#$%^&*...)
 */

import { useState } from 'react';
import type { FormEvent, ChangeEvent } from 'react';
import { useAuth } from '../contexts/AuthContext';
import Input from '../components/common/Input';
import Button from '../components/common/Button';
import Card from '../components/common/Card';
import Alert from '../components/common/Alert';
import './RegisterPage.css';

/**
 * Estado del formulario de registro.
 */
interface RegisterFormData {
  email: string;
  password: string;
  confirmPassword: string;
}

/**
 * Errores de validación del formulario.
 */
interface RegisterFormErrors {
  email?: string;
  password?: string;
  confirmPassword?: string;
}

/**
 * RegisterPage component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Similar a LoginPage pero con validación adicional:
 * - Password strength (fuerza de contraseña)
 * - Password confirmation (confirmación)
 *
 * DIFERENCIAS CON LOGIN:
 * - Más campos (confirmPassword)
 * - Validación más compleja (password strength)
 * - Muestra requisitos de password al usuario
 *
 * @example
 * // En App.tsx con React Router:
 * <Route path="/register" element={<RegisterPage />} />
 */
export function RegisterPage() {
  /**
   * Estado del formulario.
   */
  const [formData, setFormData] = useState<RegisterFormData>({
    email: '',
    password: '',
    confirmPassword: '',
  });

  /**
   * Errores de validación local.
   */
  const [errors, setErrors] = useState<RegisterFormErrors>({});

  /**
   * Context de autenticación.
   */
  const { register, isLoading, error, clearError } = useAuth();

  /**
   * Validar email con regex.
   */
  const isValidEmail = (email: string): boolean => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  };

  /**
   * Validar fuerza de password.
   *
   * EXPLICACIÓN:
   *
   * Regex para cada requisito:
   * - (?=.*[a-z]) → Lookahead: debe contener minúscula
   * - (?=.*[A-Z]) → Lookahead: debe contener mayúscula
   * - (?=.*\d) → Lookahead: debe contener dígito
   * - (?=.*[@$!%*?&#]) → Lookahead: debe contener especial
   * - .{8,} → Al menos 8 caracteres
   *
   * LOOKAHEADS:
   *
   * (?=...) es un lookahead assertion:
   * - Verifica que existe en el string
   * - No consume caracteres
   * - Permite múltiples condiciones
   *
   * Ejemplo:
   * "Pass123!" cumple todos:
   * - (?=.*[a-z]) → "a" ✓
   * - (?=.*[A-Z]) → "P" ✓
   * - (?=.*\d) → "1", "2", "3" ✓
   * - (?=.*[@$!%*?&#]) → "!" ✓
   * - .{8,} → 8 chars ✓
   */
  const isStrongPassword = (password: string): boolean => {
    const strongPasswordRegex =
      /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{8,}$/;
    return strongPasswordRegex.test(password);
  };

  /**
   * Validar formulario completo.
   */
  const validateForm = (): RegisterFormErrors => {
    const newErrors: RegisterFormErrors = {};

    // Validar email
    if (!formData.email.trim()) {
      newErrors.email = 'El email es requerido';
    } else if (!isValidEmail(formData.email)) {
      newErrors.email = 'Formato de email inválido';
    }

    // Validar password
    if (!formData.password) {
      newErrors.password = 'La contraseña es requerida';
    } else if (!isStrongPassword(formData.password)) {
      newErrors.password =
        'La contraseña debe tener mínimo 8 caracteres, incluir mayúscula, minúscula, número y carácter especial';
    }

    // Validar confirmPassword
    if (!formData.confirmPassword) {
      newErrors.confirmPassword = 'Confirma tu contraseña';
    } else if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = 'Las contraseñas no coinciden';
    }

    return newErrors;
  };

  /**
   * Handler para cambios en inputs.
   */
  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;

    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));

    // Limpiar error del campo
    if (errors[name as keyof RegisterFormErrors]) {
      setErrors((prev) => ({
        ...prev,
        [name]: undefined,
      }));
    }

    // Limpiar error del backend
    if (error) {
      clearError();
    }
  };

  /**
   * Handler para submit del formulario.
   */
  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    // Validar
    const validationErrors = validateForm();

    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    // Intentar registro
    try {
      await register({
        email: formData.email.trim(),
        password: formData.password,
        confirmPassword: formData.confirmPassword,
      });

      // Si llegamos aquí, registro fue exitoso
      // AuthContext ya manejó auto-login y navegación
    } catch (err) {
      // Error ya está en context.error
    }
  };

  return (
    <div className="register-page">
      <div className="register-page__container">
        {/* Logo o título */}
        <div className="register-page__header">
          <h1 className="register-page__title">TaskFlow</h1>
          <p className="register-page__subtitle">
            Crea tu cuenta y comienza a organizar tus tareas
          </p>
        </div>

        {/* Card con formulario */}
        <Card variant="elevated" padding="lg">
          <form onSubmit={handleSubmit} className="register-page__form">
            <h2 className="register-page__form-title">Crear Cuenta</h2>

            {/* Error del backend */}
            {error && (
              <Alert variant="error" title="Error al crear cuenta" dismissible onDismiss={clearError}>
                {error}
              </Alert>
            )}

            {/* Campo: Email */}
            <Input
              label="Email"
              name="email"
              type="email"
              value={formData.email}
              onChange={handleChange}
              error={errors.email}
              placeholder="tu@email.com"
              required
              fullWidth
              disabled={isLoading}
              autoComplete="email"
            />

            {/* Campo: Password */}
            <Input
              label="Contraseña"
              name="password"
              type="password"
              value={formData.password}
              onChange={handleChange}
              error={errors.password}
              helperText="Mínimo 8 caracteres, incluye mayúscula, minúscula, número y carácter especial"
              placeholder="••••••••"
              required
              fullWidth
              disabled={isLoading}
              autoComplete="new-password"
            />

            {/* Campo: Confirm Password */}
            <Input
              label="Confirmar Contraseña"
              name="confirmPassword"
              type="password"
              value={formData.confirmPassword}
              onChange={handleChange}
              error={errors.confirmPassword}
              placeholder="••••••••"
              required
              fullWidth
              disabled={isLoading}
              autoComplete="new-password"
            />

            {/* Botón submit */}
            <Button
              type="submit"
              variant="primary"
              size="lg"
              fullWidth
              isLoading={isLoading}
            >
              {isLoading ? 'Creando cuenta...' : 'Crear Cuenta'}
            </Button>

            {/* Link a login */}
            <div className="register-page__footer">
              <p className="register-page__footer-text">
                ¿Ya tienes una cuenta?{' '}
                <a href="/login" className="register-page__link">
                  Inicia sesión aquí
                </a>
              </p>
            </div>
          </form>
        </Card>
      </div>
    </div>
  );
}

export default RegisterPage;
