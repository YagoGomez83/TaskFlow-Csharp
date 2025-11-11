/**
 * LoginPage Component - User authentication page
 *
 * EXPLICACIÓN DE LA PÁGINA DE LOGIN:
 *
 * Esta página permite a usuarios existentes autenticarse en la aplicación.
 *
 * CARACTERÍSTICAS:
 * ✅ Formulario de login (email + password)
 * ✅ Validación en tiempo real
 * ✅ Integración con AuthContext
 * ✅ Manejo de errores del backend
 * ✅ Loading states
 * ✅ Link a página de registro
 * ✅ Redirección automática después de login exitoso
 *
 * FLUJO DE AUTENTICACIÓN:
 *
 * 1. Usuario ingresa email y password
 * 2. Submit → handleLogin() ejecuta
 * 3. Validación frontend (campos requeridos, formato email)
 * 4. Si válido → authContext.login(credentials)
 * 5. AuthContext llama authService.login() → POST /api/auth/login
 * 6. Backend valida credenciales
 * 7. Si correcto → retorna tokens (access + refresh)
 * 8. Tokens guardados en localStorage
 * 9. User state actualizado en AuthContext
 * 10. Navegación a /dashboard
 *
 * MANEJO DE ERRORES:
 *
 * - Email inválido → Mensaje local
 * - Campos vacíos → Mensajes locales
 * - Credenciales incorrectas → Error del backend
 * - Usuario bloqueado → Error del backend
 * - Network error → Mensaje genérico
 */

import { useState } from 'react';
import type { FormEvent, ChangeEvent } from 'react';
import { useAuth } from '../contexts/AuthContext';
import Input from '../components/common/Input';
import Button from '../components/common/Button';
import Card from '../components/common/Card';
import Alert from '../components/common/Alert';
import './LoginPage.css';

/**
 * Estado del formulario de login.
 */
interface LoginFormData {
  email: string;
  password: string;
}

/**
 * Errores de validación del formulario.
 */
interface LoginFormErrors {
  email?: string;
  password?: string;
}

/**
 * LoginPage component.
 *
 * EXPLICACIÓN DEL COMPONENTE:
 *
 * Esta es una "page component" (componente de página):
 * - Coordina múltiples componentes reutilizables
 * - Maneja lógica de negocio de la página
 * - Se conecta con servicios y contextos
 * - Maneja navegación
 *
 * PATRÓN CONTAINER/PRESENTATIONAL:
 * - LoginPage es un "container" (maneja lógica)
 * - Input, Button, Card son "presentational" (solo UI)
 *
 * CONTROLLED FORM:
 * - useState maneja estado del form
 * - Cada input tiene value={state} y onChange
 * - React es single source of truth
 *
 * @example
 * // En App.tsx con React Router:
 * <Route path="/login" element={<LoginPage />} />
 */
export function LoginPage() {
  /**
   * Estado del formulario.
   */
  const [formData, setFormData] = useState<LoginFormData>({
    email: '',
    password: '',
  });

  /**
   * Errores de validación local.
   */
  const [errors, setErrors] = useState<LoginFormErrors>({});

  /**
   * Context de autenticación.
   *
   * EXPLICACIÓN:
   *
   * useAuth() es un custom hook que consume AuthContext.
   * Provee:
   * - login(): Función para autenticar
   * - isLoading: Loading state durante autenticación
   * - error: Error message del backend
   * - clearError(): Limpiar error
   */
  const { login, isLoading, error, clearError } = useAuth();

  /**
   * Validar email con regex.
   *
   * EXPLICACIÓN:
   *
   * Regex simple para validar formato de email:
   * - Debe tener @ en el medio
   * - Dominio con punto
   *
   * Ejemplos válidos:
   * - user@example.com ✓
   * - test.user@domain.co ✓
   *
   * Ejemplos inválidos:
   * - user@example ✗ (sin .com)
   * - user.example.com ✗ (sin @)
   */
  const isValidEmail = (email: string): boolean => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  };

  /**
   * Validar formulario completo.
   *
   * EXPLICACIÓN:
   *
   * Retorna objeto con errores encontrados.
   * Si objeto vacío → form válido.
   */
  const validateForm = (): LoginFormErrors => {
    const newErrors: LoginFormErrors = {};

    // Validar email
    if (!formData.email.trim()) {
      newErrors.email = 'El email es requerido';
    } else if (!isValidEmail(formData.email)) {
      newErrors.email = 'Formato de email inválido';
    }

    // Validar password
    if (!formData.password) {
      newErrors.password = 'La contraseña es requerida';
    }

    return newErrors;
  };

  /**
   * Handler para cambios en inputs.
   *
   * EXPLICACIÓN:
   *
   * ChangeEvent<HTMLInputElement>:
   * - Evento de cambio de input
   *
   * e.target.name → 'email' o 'password'
   * e.target.value → valor nuevo
   *
   * Functional update:
   * - setFormData(prev => ...) recibe estado anterior
   * - Spread operator {...prev} copia estado
   * - [name]: value sobrescribe campo específico
   */
  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;

    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));

    // Limpiar error del campo cuando usuario escribe
    if (errors[name as keyof LoginFormErrors]) {
      setErrors((prev) => ({
        ...prev,
        [name]: undefined,
      }));
    }

    // Limpiar error del backend cuando usuario empieza a corregir
    if (error) {
      clearError();
    }
  };

  /**
   * Handler para submit del formulario.
   *
   * EXPLICACIÓN:
   *
   * e.preventDefault():
   * - Previene comportamiento default del form
   * - Default: Recargar página
   * - Queremos manejar submit con JavaScript
   *
   * FLUJO:
   * 1. Validar formulario
   * 2. Si hay errores → Mostrar y detener
   * 3. Si válido → Llamar login del context
   * 4. Context maneja la petición API
   * 5. Si success → Context navega a dashboard
   * 6. Si error → Context actualiza error state
   */
  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    // Validar
    const validationErrors = validateForm();

    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    // Intentar login
    try {
      await login({
        email: formData.email.trim(),
        password: formData.password,
      });

      // Si llegamos aquí, login fue exitoso
      // AuthContext ya manejó la navegación
    } catch (err) {
      // Error ya está en context.error
      // No necesitamos hacer nada aquí
    }
  };

  return (
    <div className="login-page">
      <div className="login-page__container">
        {/* Logo o título de la app */}
        <div className="login-page__header">
          <h1 className="login-page__title">TaskFlow</h1>
          <p className="login-page__subtitle">
            Gestiona tus tareas de forma eficiente
          </p>
        </div>

        {/* Card con formulario */}
        <Card variant="elevated" padding="lg">
          <form onSubmit={handleSubmit} className="login-page__form">
            <h2 className="login-page__form-title">Iniciar Sesión</h2>

            {/* Error del backend */}
            {error && (
              <Alert variant="error" title="Error al iniciar sesión" dismissible onDismiss={clearError}>
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
              placeholder="••••••••"
              required
              fullWidth
              disabled={isLoading}
              autoComplete="current-password"
            />

            {/* Botón submit */}
            <Button
              type="submit"
              variant="primary"
              size="lg"
              fullWidth
              isLoading={isLoading}
            >
              {isLoading ? 'Iniciando sesión...' : 'Iniciar Sesión'}
            </Button>

            {/* Link a registro */}
            <div className="login-page__footer">
              <p className="login-page__footer-text">
                ¿No tienes una cuenta?{' '}
                <a href="/register" className="login-page__link">
                  Regístrate aquí
                </a>
              </p>
            </div>
          </form>
        </Card>
      </div>
    </div>
  );
}

export default LoginPage;
