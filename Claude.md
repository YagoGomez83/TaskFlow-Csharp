# TaskFlow - Development Tracking

## Project Overview

TaskFlow is a task management system built with Clean Architecture principles, implementing CQRS with MediatR, JWT authentication, and comprehensive security features.

**Technology Stack:**
- .NET 9
- Entity Framework Core
- PostgreSQL
- Redis (distributed cache)
- JWT Authentication
- MediatR (CQRS Pattern)
- FluentValidation
- AutoMapper
- BCrypt (password hashing)

---

## Development Progress

### PHASE 1: Initial Setup ✅

**Date:** 2025-11-10

1. **Git Repository Initialization** ✅
   - Initialized local git repository
   - Created initial commit with project structure
   - Connected to remote: https://github.com/YagoGomez83/TaskFlow-Csharp.git
   - Pushed initial commit to master branch

2. **Project Structure** ✅
   - TaskManagement.Domain - Domain entities, enums, value objects
   - TaskManagement.Application - Use cases, DTOs, validators, behaviors
   - TaskManagement.Infrastructure - Database context, services
   - TaskManagement.API - Controllers, middleware
   - TaskManagement.UnitTests - Unit test project
   - TaskManagement.IntegrationTests - Integration test project

---

### PHASE 2: Compilation Errors Fixed ✅

**Date:** 2025-11-10

#### Issues Found and Resolved:

1. **Result Pattern Method Signatures** ✅
   - **Problem:** `Result.Success(value)` and `Result.Failure<T>(error)` were being called incorrectly
   - **Solution:** Changed to `Result<T>.Success(value)` and `Result<T>.Failure(error)` throughout all handlers
   - **Files Modified:**
     - RegisterCommandHandler.cs
     - LoginCommandHandler.cs
     - RefreshTokenCommandHandler.cs
     - CreateTaskCommandHandler.cs
     - UpdateTaskCommandHandler.cs
     - GetTaskByIdQueryHandler.cs
     - GetTasksQueryHandler.cs

2. **ICacheService Implementation Mismatch** ✅
   - **Problem:** `SetAsync` signature mismatch between interface and implementation
   - **Interface:** `Task SetAsync<T>(string key, T value, TimeSpan expiration)`
   - **Implementation:** Had `TimeSpan?` with default value
   - **Solution:** Updated CacheService.cs to match interface signature

3. **JWT Bearer Header Append Method** ✅
   - **Problem:** `context.Response.Headers.Append("Token-Expired", "true")` incompatible with .NET 9
   - **Solution:** Changed to `context.Response.Headers["Token-Expired"] = "true"`
   - **File:** DependencyInjection.cs (Infrastructure layer)

4. **Namespace Import Issues** ✅
   - **Problem:** Controllers importing from nested namespaces that don't exist
   - **Solution:** Simplified using statements in AuthController.cs and TasksController.cs
   - Added using alias for TaskStatus to resolve ambiguity with System.Threading.Tasks.TaskStatus

5. **Missing AddApplication Extension Method** ✅
   - **Problem:** Program.cs calling `AddApplication()` which didn't exist
   - **Solution:** Created `DependencyInjection.cs` in Application layer
   - **Registers:**
     - MediatR with all handlers
     - Pipeline Behaviors (LoggingBehavior, ValidationBehavior)
     - AutoMapper
     - FluentValidation validators

6. **Command/Query Constructor Issues** ✅
   - **Problem:** Controllers using constructor syntax but commands/queries only had properties
   - **Solution:** Added constructors to all commands and queries:
     - RegisterCommand(email, password, confirmPassword)
     - LoginCommand(email, password)
     - RefreshTokenCommand(refreshToken)
     - GetTasksQuery(page, pageSize, status, priority)
     - GetTaskByIdQuery(taskId)
     - CreateTaskCommand(title, description, dueDate, priority)
     - UpdateTaskCommand(taskId, title, description, dueDate, priority, status)
     - DeleteTaskCommand(taskId)

#### Build Status:
- **Errors:** 0 ✅
- **Warnings:** 31 (XML documentation formatting only)
- **Status:** BUILD SUCCESSFUL ✅

---

### PHASE 3: Frontend Development ✅

**Date:** 2025-11-11

#### Summary

Frontend completo implementado con React 19.2.0 + TypeScript 5.9.3 + Vite 7.2.2. Incluye autenticación JWT, gestión de estado global con Context API, 8 componentes reutilizables, servicios API, y validación de formularios.

---

#### 1. React + TypeScript Setup ✅

**Vite Configuration:**
- React plugin para JSX/TSX support
- Fast HMR (Hot Module Replacement)
- Optimized production builds
- Dev server en `http://localhost:5173`

**TypeScript Configuration:**
- **Target:** ES2022 (modern JavaScript)
- **Module System:** ESNext con bundler resolution
- **JSX:** react-jsx (automatic runtime - sin import React)
- **Strict Mode:** Habilitado para máxima seguridad de tipos

**Strict Checks Enabled:**
```typescript
{
  "strict": true,
  "noUnusedLocals": true,
  "noUnusedParameters": true,
  "noFallthroughCasesInSwitch": true,
  "noUncheckedSideEffectImports": true
}
```

**Entry Point:**
- React 18+ `createRoot()` API en `src/main.tsx`
- Strict mode habilitado
- Root element montado en `#root`

**Packages Installed:**
- react: 19.2.0
- react-dom: 19.2.0
- typescript: 5.9.3
- vite: 7.2.2
- @vitejs/plugin-react: 4.3.4
- axios: 1.7.9
- jwt-decode: 4.0.0
- @tanstack/react-query: 6.1.0 (instalado, no configurado aún)

---

#### 2. Frontend Folder Structure ✅

```
frontend/
├── src/
│   ├── components/common/          # 8 componentes reutilizables
│   │   ├── Alert.tsx               # Notificación (4 variants)
│   │   ├── Alert.css
│   │   ├── Button.tsx              # Botón (4 variants, 3 sizes)
│   │   ├── Button.css
│   │   ├── Card.tsx                # Container card
│   │   ├── Card.css
│   │   ├── Input.tsx               # Form input con validación
│   │   ├── Input.css
│   │   ├── Spinner.tsx             # Loading indicator
│   │   ├── Spinner.css
│   │   ├── TaskCard.tsx            # Display individual task ✨ NEW
│   │   ├── TaskCard.css
│   │   ├── TaskList.tsx            # Lista de tareas con paginación ✨ NEW
│   │   ├── TaskList.css
│   │   ├── TaskForm.tsx            # Formulario create/edit ✨ NEW
│   │   └── TaskForm.css
│   │
│   ├── contexts/
│   │   └── AuthContext.tsx         # Global auth state
│   │
│   ├── services/
│   │   ├── api.ts                  # Axios con JWT interceptors
│   │   ├── authService.ts          # Login, register, logout
│   │   └── taskService.ts          # Task CRUD operations
│   │
│   ├── types/
│   │   └── index.ts                # TypeScript interfaces y enums
│   │
│   ├── hooks/                      # Custom hooks (directorio listo)
│   ├── pages/                      # Page components (directorio listo)
│   ├── utils/                      # Utilities (directorio listo)
│   │
│   ├── App.tsx                     # Root component
│   ├── main.tsx                    # Entry point
│   └── index.css                   # Global styles
│
├── public/                         # Static assets
├── package.json                    # Dependencies
├── tsconfig.json                   # TypeScript config
├── vite.config.ts                  # Vite config
└── index.html                      # HTML entry
```

**Explicación de Estructura:**

- **components/common:** Componentes reutilizables en toda la app
- **contexts:** React Context para estado global
- **services:** Lógica de comunicación con API
- **types:** Definiciones TypeScript centralizadas
- **hooks:** Custom hooks (useAuth, etc.)
- **pages:** Componentes de página (LoginPage, DashboardPage, etc.)
- **utils:** Funciones auxiliares

---

#### 3. Axios JWT Interceptors ✅

**Implementación Completa de Autenticación:**

**Archivo:** `src/services/api.ts`

**Flujo de Autenticación:**

```
1. Usuario Login
   ↓
POST /api/auth/login
   ↓
Backend retorna: accessToken (15 min) + refreshToken (7 days)
   ↓
setTokens() guarda en localStorage
   ↓
JWT decodificado para extraer user info
   ↓
Usuario autenticado

2. Cada Request
   ↓
Request Interceptor ejecuta
   ↓
Agrega: Authorization: Bearer {accessToken}
   ↓
Request enviado
   ↓
Response recibida

3. Token Expiración (401)
   ↓
Response Interceptor detecta 401
   ↓
POST /api/auth/refresh con refreshToken
   ↓
Backend retorna nuevos tokens (token rotation)
   ↓
Request original se reintenta con nuevo token
   ↓
Response fresco retornado

4. Refresh Token Expirado
   ↓
Refresh request falla con 401
   ↓
Tokens eliminados de localStorage
   ↓
Usuario redirigido a /login
```

**Token Storage (localStorage):**

```typescript
const TOKEN_KEYS = {
  ACCESS_TOKEN: 'taskflow_access_token',
  REFRESH_TOKEN: 'taskflow_refresh_token'
}

// Functions
getAccessToken() → string | null
getRefreshToken() → string | null
setTokens(authResponse) → void
clearTokens() → void
```

**Request Interceptor:**
- Ejecuta ANTES de cada request
- Agrega automáticamente `Authorization: Bearer {token}` header
- Sin manejo manual de headers en componentes

**Response Interceptor:**
- Detecta 401 Unauthorized
- Intenta refresh automático con refreshToken
- Implementa token rotation (nuevo refresh token en cada refresh)
- Maneja concurrencia (múltiples requests simultáneos)
- Cola de requests fallidos para reintentar después del refresh

**Manejo de Concurrencia:**

```typescript
// Problema: 5 requests expiran simultáneamente
// Solución:
isRefreshing = true  (solo primer request hace refresh)
failedQueue = []     (otros requests en cola)
// Todos esperan refresh
// Después del refresh, todos reintentan con nuevo token
```

---

#### 4. Componentes Reutilizables ✅

Todos los componentes están completamente tipados, documentados en español, y con soporte de accesibilidad.

##### **4.1. Button Component**

**Archivo:** `src/components/common/Button.tsx`

**Props:**
```typescript
variant?: 'primary' | 'secondary' | 'danger' | 'ghost'
size?: 'sm' | 'md' | 'lg'
isLoading?: boolean
fullWidth?: boolean
children: ReactNode
```

**Features:**
- ✅ Loading state con spinner
- ✅ Auto-disable durante loading
- ✅ ARIA attributes (aria-disabled, aria-busy)
- ✅ Flexible prop forwarding (onClick, onMouseEnter, etc.)
- ✅ 4 variants de color
- ✅ 3 tamaños

**Uso:**
```tsx
<Button variant="primary" isLoading={isSubmitting}>
  Submit
</Button>
```

---

##### **4.2. Input Component**

**Archivo:** `src/components/common/Input.tsx`

**Props:**
```typescript
label?: string
error?: string
helperText?: string
fullWidth?: boolean
type?: 'text' | 'email' | 'password' | 'number' | 'date' | 'tel' | 'url'
required?: boolean
disabled?: boolean
```

**Features:**
- ✅ Auto-generated unique IDs
- ✅ Label con required indicator (*)
- ✅ Error state con red styling
- ✅ ARIA attributes (aria-invalid, aria-describedby)
- ✅ Helper text below input
- ✅ Múltiples tipos de input

**Uso:**
```tsx
<Input
  label="Email"
  type="email"
  error={errors.email}
  helperText="Ingresa tu email"
  required
/>
```

---

##### **4.3. Alert Component**

**Archivo:** `src/components/common/Alert.tsx`

**Props:**
```typescript
variant?: 'success' | 'error' | 'warning' | 'info'
title?: string
children: ReactNode
dismissible?: boolean
onDismiss?: () => void
```

**Features:**
- ✅ Icon por variant (emoji-based)
- ✅ Título opcional en negrita
- ✅ Botón close cuando dismissible
- ✅ ARIA alerts (role="alert")
- ✅ Diferentes aria-live levels

**Uso:**
```tsx
<Alert variant="error" title="Error" dismissible>
  No se pudo guardar la tarea
</Alert>
```

---

##### **4.4. Card Component**

**Archivo:** `src/components/common/Card.tsx`

**Props:**
```typescript
variant?: 'flat' | 'elevated' | 'floating'
header?: ReactNode
footer?: ReactNode
children: ReactNode
padding?: 'none' | 'sm' | 'md' | 'lg'
hoverable?: boolean
onClick?: () => void
```

**Features:**
- ✅ Header, body, footer sections
- ✅ 3 shadow variants para elevación
- ✅ Padding customizable
- ✅ Clickable con hover effects
- ✅ Keyboard navigation (Enter/Space)
- ✅ role="button" cuando clickable

**Uso:**
```tsx
<Card
  variant="elevated"
  header={<h3>Card Title</h3>}
  footer={<Button>Action</Button>}
>
  Card content here
</Card>
```

---

##### **4.5. Spinner Component**

**Archivo:** `src/components/common/Spinner.tsx`

**Props:**
```typescript
size?: 'sm' | 'md' | 'lg' | 'xl'
label?: string
overlay?: boolean
color?: 'primary' | 'white' | 'gray'
```

**Features:**
- ✅ 4 tamaños
- ✅ Label opcional
- ✅ Full-page overlay mode
- ✅ CSS animation
- ✅ ARIA status indicator (role="status")

**Uso:**
```tsx
<Spinner overlay label="Cargando..." />
```

---

##### **4.6. TaskCard Component ✨ NEW**

**Archivo:** `src/components/common/TaskCard.tsx`

**Propósito:** Mostrar una tarea individual con todas sus propiedades y acciones.

**Props:**
```typescript
task: TaskDto
onEdit?: (task: TaskDto) => void
onDelete?: (taskId: string) => void
onStatusChange?: (taskId: string, newStatus: TaskStatus) => void
isDeleting?: boolean
```

**Features:**
- ✅ Muestra título, descripción, fechas
- ✅ Badges visuales para prioridad y estado
- ✅ Acciones: editar, eliminar, cambiar estado
- ✅ Quick actions (Iniciar, Completar)
- ✅ Formato de fechas legible
- ✅ Indicador de tareas vencidas
- ✅ Loading state en botón delete
- ✅ Responsive design

**Priority Badges:**
- High: Rojo (urgente)
- Medium: Amarillo (normal)
- Low: Azul (baja prioridad)

**Status Badges:**
- Pending: Gris (sin empezar)
- InProgress: Azul (trabajando)
- Completed: Verde (terminado)

**Quick Actions:**
- Pending → botón "Iniciar" (va a InProgress)
- InProgress → botón "Completar" (va a Completed)
- Completed → sin botón (ya terminado)

**Uso:**
```tsx
<TaskCard
  task={task}
  onEdit={(task) => navigate(`/tasks/${task.id}/edit`)}
  onDelete={(id) => deleteTaskMutation.mutate(id)}
  onStatusChange={(id, status) => updateStatusMutation.mutate({ id, status })}
  isDeleting={deleteTaskMutation.isLoading}
/>
```

---

##### **4.7. TaskList Component ✨ NEW**

**Archivo:** `src/components/common/TaskList.tsx`

**Propósito:** Contenedor que muestra múltiples tareas con paginación.

**Props:**
```typescript
tasks?: PaginatedList<TaskDto>
isLoading?: boolean
error?: string | null
onEdit?: (task: TaskDto) => void
onDelete?: (taskId: string) => void
onStatusChange?: (taskId: string, newStatus: TaskStatus) => void
onPageChange?: (pageNumber: number) => void
deletingTaskId?: string | null
```

**Features:**
- ✅ Grid responsive de TaskCards
- ✅ Paginación integrada (Anterior/Siguiente)
- ✅ Estados: loading, error, empty, success
- ✅ Contador de tareas (ej: "Mostrando 10 de 25 tareas")
- ✅ Propagación de callbacks a TaskCards
- ✅ Accesibilidad completa

**Estados del Componente:**

1. **Loading:** Muestra Spinner overlay
2. **Error:** Muestra Alert variant="error"
3. **Empty:** Muestra mensaje amigable + emoji
4. **Success:** Muestra grid + paginación

**Grid Responsive:**
- Desktop (>1024px): 3 columnas
- Tablet (768-1024px): 2 columnas
- Mobile (<768px): 1 columna

**Paginación:**
- Botón "Anterior" (disabled si !hasPreviousPage)
- Info: "Página X de Y"
- Botón "Siguiente" (disabled si !hasNextPage)

**Uso:**
```tsx
const { data, isLoading, error } = useQuery('tasks', fetchTasks);

<TaskList
  tasks={data}
  isLoading={isLoading}
  error={error}
  onEdit={(task) => navigate(`/tasks/${task.id}/edit`)}
  onDelete={(id) => deleteMutation.mutate(id)}
  onStatusChange={(id, status) => updateMutation.mutate({ id, status })}
  onPageChange={(page) => setPage(page)}
  deletingTaskId={deleteMutation.variables?.id}
/>
```

---

##### **4.8. TaskForm Component ✨ NEW**

**Archivo:** `src/components/common/TaskForm.tsx`

**Propósito:** Formulario reutilizable para crear o editar tareas.

**Props:**
```typescript
initialData?: TaskDto | null
onSubmit: (data: CreateTaskRequest | UpdateTaskRequest) => void
onCancel?: () => void
isSubmitting?: boolean
error?: string | null
```

**Features:**
- ✅ Modo Create o Edit (basado en initialData)
- ✅ Validación de campos en tiempo real
- ✅ Manejo de fechas (Date picker, formato ISO)
- ✅ Dropdowns para prioridad y estado
- ✅ Mensajes de error detallados
- ✅ Loading state durante submit
- ✅ Controlled components pattern
- ✅ Accesibilidad completa

**Modos:**

1. **CREATE MODE** (initialData = null):
   - Campos vacíos
   - Priority = Medium (default)
   - Status = Pending (auto-asignado)
   - Botón "Crear Tarea"

2. **EDIT MODE** (initialData = TaskDto):
   - Campos pre-poblados
   - Permite cambiar status
   - Botón "Actualizar Tarea"

**Validación:**

**Frontend (UX):**
- title: Required, max 200 chars
- dueDate: Optional, debe ser futura si se especifica
- priority: Required (dropdown)
- status: Required en edit mode

**Backend (Security):**
- FluentValidation re-valida todo
- Defense in depth

**Campos del Form:**

1. **Título** (required):
   - Input text
   - Max 200 caracteres
   - Error si vacío

2. **Descripción** (optional):
   - Textarea
   - 4 rows
   - Sin límite de caracteres

3. **Fecha de vencimiento** (optional):
   - Input date
   - Debe ser futura
   - Formato: YYYY-MM-DD

4. **Prioridad** (required):
   - Select dropdown
   - Opciones: Baja, Media, Alta
   - Default: Media

5. **Estado** (required en edit mode):
   - Select dropdown
   - Opciones: Pendiente, En Progreso, Completada
   - Solo visible en edit mode

**Manejo de Fechas:**

```typescript
// Backend → Frontend
"2025-11-10T14:30:00Z" → "2025-11-10" (para input[type="date"])

// Frontend → Backend
"2025-11-10" → "2025-11-10T00:00:00Z" (ISO 8601 UTC)
```

**Uso:**
```tsx
// Create mode
<TaskForm
  onSubmit={(data) => createMutation.mutate(data)}
  onCancel={() => navigate('/tasks')}
  isSubmitting={createMutation.isLoading}
  error={createMutation.error?.message}
/>

// Edit mode
<TaskForm
  initialData={task}
  onSubmit={(data) => updateMutation.mutate({ id: task.id, ...data })}
  onCancel={() => navigate('/tasks')}
  isSubmitting={updateMutation.isLoading}
  error={updateMutation.error?.message}
/>
```

---

#### 5. Global State - Context API ✅

**Archivo:** `src/contexts/AuthContext.tsx`

**Arquitectura:** Provider Pattern + Custom Hook

**AuthContext State:**

```typescript
interface AuthContextType {
  // State
  user: User | null
  isLoading: boolean
  error: string | null

  // Computed
  isAuthenticated: boolean

  // Actions
  login(credentials: LoginRequest): Promise<void>
  register(userData: RegisterRequest): Promise<void>
  logout(): void
  clearError(): void
}
```

**Características:**

1. **Automatic Session Restoration:**
   - Al montar app, verifica localStorage por token válido
   - Restaura sesión de usuario sin login adicional
   - `isLoading` previene flash de contenido no autenticado

2. **Login Flow:**
   - Llama `authService.login()` → POST /api/auth/login
   - Backend retorna tokens
   - Tokens guardados en localStorage
   - JWT decodificado para extraer user info
   - Context actualiza: `user != null`, `isAuthenticated = true`
   - Componentes re-renderizan
   - Router navega a dashboard

3. **Register Flow:**
   - Similar a login
   - Backend auto-loguea usuario (retorna tokens)
   - Usuario autenticado inmediatamente después de registro

4. **Logout Flow:**
   - Limpia tokens de localStorage
   - Set `user = null`
   - Limpia error message
   - Componentes re-renderizan mostrando login

5. **Error Handling:**
   - Errores de login/register almacenados en `error` state
   - `clearError()` resetea mensaje
   - Componentes muestran Alert al usuario

**Setup (main.tsx):**

```tsx
import { AuthProvider } from './contexts/AuthContext';

root.render(
  <AuthProvider>
    <App />
  </AuthProvider>
);
```

**Uso en Componentes:**

```tsx
import { useAuth } from './contexts/AuthContext';

function LoginPage() {
  const { login, isLoading, error } = useAuth();

  const handleSubmit = async (data) => {
    try {
      await login(data);
      navigate('/dashboard');
    } catch (err) {
      // Error ya en context.error
    }
  };
}

function Navbar() {
  const { user, isAuthenticated, logout } = useAuth();

  return isAuthenticated ? (
    <>
      <span>Bienvenido, {user?.email}</span>
      <button onClick={logout}>Cerrar Sesión</button>
    </>
  ) : (
    <Link to="/login">Iniciar Sesión</Link>
  );
}
```

---

#### 6. Form Validation ✅

**Estrategia Multi-Layer:**

**Layer 1: Browser Native**
- Email input type valida formato
- `required` attribute previene submit vacío
- `maxLength` limita caracteres

**Layer 2: Frontend Validation (UX)**
- Fast feedback al usuario
- Sin round-trip al servidor
- Validación antes de submit
- Mensajes de error en español

**Layer 3: Backend Validation (Security)**
- Server-side FluentValidation
- Defense in depth - nunca confiar en cliente
- Mensajes detallados retornados

**TypeScript Types:**

**Archivo:** `src/types/index.ts`

**Auth Types:**
```typescript
interface LoginRequest {
  email: string
  password: string
}

interface RegisterRequest {
  email: string
  password: string
  confirmPassword: string
}

interface RefreshTokenRequest {
  refreshToken: string
}

interface User {
  id: string
  email: string
  role: UserRole
}

enum UserRole {
  User = 'User',
  Admin = 'Admin'
}
```

**Task Types:**
```typescript
interface TaskDto {
  id: string
  title: string
  description: string | null
  dueDate: string | null  // ISO 8601
  priority: TaskPriority
  status: TaskStatus
  userId: string
  createdAt: string  // ISO 8601
  updatedAt: string  // ISO 8601
}

interface CreateTaskRequest {
  title: string
  description?: string
  dueDate?: string  // ISO 8601
  priority: TaskPriority
}

interface UpdateTaskRequest {
  title: string
  description?: string
  dueDate?: string  // ISO 8601
  priority: TaskPriority
  status: TaskStatus
}

enum TaskStatus {
  Pending = 'Pending',
  InProgress = 'InProgress',
  Completed = 'Completed'
}

enum TaskPriority {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High'
}
```

**Pagination Types:**
```typescript
interface PaginatedList<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

interface PaginationParams {
  page: number
  pageSize: number
}

interface TaskQueryParams extends PaginationParams {
  status?: TaskStatus
  priority?: TaskPriority
}
```

**Error Types:**
```typescript
interface ProblemDetails {
  type: string
  title: string
  status: number
  detail?: string
  errors?: Record<string, string[]>
  traceId?: string
}

interface ApiError {
  message: string
  status: number
  details?: ProblemDetails
}
```

**Password Requirements (Backend FluentValidation):**
- Minimum 8 characters
- Al menos 1 mayúscula (A-Z)
- Al menos 1 minúscula (a-z)
- Al menos 1 dígito (0-9)
- Al menos 1 carácter especial (!@#$%^&*...)

---

#### 7. Services Architecture ✅

##### **7.1. API Service**

**Archivo:** `src/services/api.ts`

**Responsabilidades:**
- Axios instance configurado
- Base URL: `http://localhost:5000/api`
- JWT interceptors (request & response)
- Token management functions
- Error handling

**Functions:**
```typescript
getAccessToken(): string | null
getRefreshToken(): string | null
setTokens(authResponse: AuthResponse): void
clearTokens(): void
```

---

##### **7.2. Auth Service**

**Archivo:** `src/services/authService.ts`

**Functions:**

1. **login(credentials: LoginRequest): Promise<AuthResponse>**
   - POST /api/auth/login
   - Retorna tokens

2. **register(userData: RegisterRequest): Promise<AuthResponse>**
   - POST /api/auth/register
   - Auto-loguea usuario

3. **logout(): void**
   - Limpia tokens de localStorage

4. **getCurrentUser(): User | null**
   - Extrae user info del JWT
   - Decodifica accessToken

5. **isAuthenticated(): boolean**
   - Verifica si hay token válido

6. **isTokenExpired(token: string): boolean**
   - Verifica expiración del JWT

---

##### **7.3. Task Service**

**Archivo:** `src/services/taskService.ts`

**CRUD Operations:**

1. **getTasks(params: TaskQueryParams): Promise<PaginatedList<TaskDto>>**
   - GET /api/tasks
   - Query params: page, pageSize, status, priority
   - Retorna lista paginada

2. **getTaskById(id: string): Promise<TaskDto>**
   - GET /api/tasks/{id}
   - Retorna tarea específica

3. **createTask(taskData: CreateTaskRequest): Promise<TaskDto>**
   - POST /api/tasks
   - Retorna tarea creada

4. **updateTask(id: string, taskData: UpdateTaskRequest): Promise<TaskDto>**
   - PUT /api/tasks/{id}
   - Retorna tarea actualizada

5. **deleteTask(id: string): Promise<void>**
   - DELETE /api/tasks/{id}
   - Soft delete

**Helper Functions:**

```typescript
formatDateForApi(date: Date): string
// Date → "2025-11-10T00:00:00Z"

parseDateFromApi(isoString: string): Date
// "2025-11-10T14:30:00Z" → Date object
```

---

#### 8. Implementation Status

**✅ COMPLETED (100%):**

1. **React + TypeScript Setup** ✅
   - Vite configuration
   - Strict TypeScript mode
   - React 19 with hooks
   - JSX/TSX support

2. **Frontend Folder Structure** ✅
   - components/common (8 componentes)
   - services (api, auth, tasks)
   - contexts (AuthContext)
   - types (centralized)
   - Directorios listos: hooks, pages, utils

3. **Axios JWT Interceptors** ✅
   - Request interceptor (Authorization header)
   - Response interceptor (token refresh)
   - Token rotation automático
   - Manejo de concurrencia
   - Error handling

4. **Reusable Components** ✅
   - Button ✅
   - Input ✅
   - Alert ✅
   - Card ✅
   - Spinner ✅
   - TaskCard ✅
   - TaskList ✅
   - TaskForm ✅

5. **Global State - Context API** ✅
   - AuthProvider component
   - useAuth() custom hook
   - Session restoration
   - Login/Register/Logout
   - Error handling

6. **Form Validation** ✅
   - TypeScript types para todos los requests
   - Frontend validation patterns
   - Error display support
   - Backend validation (FluentValidation)

7. **Services** ✅
   - authService (JWT operations)
   - taskService (CRUD)
   - api client (interceptors)

---

#### 9. What's Still Needed (Future Work)

**Page Components:**
- LoginPage, RegisterPage
- DashboardPage
- TaskListPage, TaskDetailPage
- CreateTaskPage, EditTaskPage

**Routing:**
- React Router configuration
- Route definitions
- Protected routes (PrivateRoute component)
- Navigation structure

**Advanced Features:**
- React Query integration (ya instalado)
- Optimistic updates
- Real-time sync
- WebSockets

**Testing:**
- Unit tests (React Testing Library)
- Component tests
- Integration tests

**Styling:**
- Global theme/variables
- Dark mode support
- Responsive improvements

---

#### 10. Architecture Benefits

**Type Safety:**
- Compile-time error detection
- Full IDE support (IntelliSense)
- Refactoring seguro

**Clean Code:**
- Separation of concerns (services, context, components)
- Single Responsibility Principle
- DRY (Don't Repeat Yourself)

**Scalability:**
- Fácil agregar nuevas features
- Componentes reutilizables
- Folder structure clara

**Reusability:**
- 8 componentes reutilizables
- Services compartidos
- Custom hooks

**Security:**
- JWT interceptors
- Token rotation
- Backend validation
- HTTPS enforcement (production)

**User Experience:**
- Automatic token refresh (sin interrupciones)
- Loading states claros
- Error messages informativos
- Responsive design

---

#### 11. File Locations (Frontend)

**Core Files:**
- `frontend/src/main.tsx` - Entry point
- `frontend/src/App.tsx` - Root component
- `frontend/src/contexts/AuthContext.tsx` - Global auth state
- `frontend/src/types/index.ts` - TypeScript types

**Services:**
- `frontend/src/services/api.ts` - Axios config
- `frontend/src/services/authService.ts` - Auth operations
- `frontend/src/services/taskService.ts` - Task operations

**Components:**
- `frontend/src/components/common/Button.tsx`
- `frontend/src/components/common/Input.tsx`
- `frontend/src/components/common/Alert.tsx`
- `frontend/src/components/common/Card.tsx`
- `frontend/src/components/common/Spinner.tsx`
- `frontend/src/components/common/TaskCard.tsx`
- `frontend/src/components/common/TaskList.tsx`
- `frontend/src/components/common/TaskForm.tsx`

**Configuration:**
- `frontend/package.json` - Dependencies
- `frontend/tsconfig.json` - TypeScript config
- `frontend/vite.config.ts` - Vite config

---

## Architecture Overview

### Clean Architecture Layers

```
┌─────────────────────────────────────────┐
│          Presentation Layer             │
│         (TaskManagement.API)            │
│  - Controllers                          │
│  - Middleware                           │
│  - Program.cs (Startup)                 │
└─────────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│         Application Layer               │
│    (TaskManagement.Application)         │
│  - Use Cases (Commands/Queries)         │
│  - Handlers                             │
│  - DTOs                                 │
│  - Validators                           │
│  - Behaviors (Logging, Validation)      │
└─────────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│           Domain Layer                  │
│      (TaskManagement.Domain)            │
│  - Entities (User, TaskItem)            │
│  - Value Objects (Email)                │
│  - Enums (TaskStatus, TaskPriority)     │
│  - Exceptions                           │
└─────────────────────────────────────────┘
                  ↑
┌─────────────────────────────────────────┐
│        Infrastructure Layer             │
│   (TaskManagement.Infrastructure)       │
│  - ApplicationDbContext                 │
│  - Services (Token, Password, Cache)    │
│  - External integrations                │
└─────────────────────────────────────────┘
```

### Domain Entities

#### 1. User Entity

**Properties:**
- `Id` (Guid) - Primary key
- `Email` (Email Value Object) - User's email address
- `PasswordHash` (string) - BCrypt hashed password
- `Role` (UserRole enum) - User, Admin
- `FailedLoginAttempts` (int) - Login security tracking
- `LockedOutUntil` (DateTime?) - Account lockout timestamp
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime?)

**Methods:**
- `Create()` - Factory method
- `RecordFailedLogin()` - Increments failed attempts, locks account after 5 attempts
- `ResetLoginAttempts()` - Clears failed attempts on successful login
- `CanLogin()` - Checks if account is not locked

#### 2. TaskItem Entity

**Properties:**
- `Id` (Guid) - Primary key
- `Title` (string) - Task title
- `Description` (string?) - Optional description
- `Status` (TaskStatus enum) - Pending, InProgress, Completed
- `Priority` (TaskPriority enum) - Low, Medium, High
- `DueDate` (DateTime?) - Optional due date
- `UserId` (Guid) - Foreign key to User
- `IsDeleted` (bool) - Soft delete flag
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime?)
- `DeletedAt` (DateTime?)

**Methods:**
- `Create()` - Factory method
- `UpdateTitle()`, `UpdateDescription()`, `UpdateDueDate()`, `UpdatePriority()`, `UpdateStatus()` - Domain methods
- `MarkAsDeleted()` - Soft delete

### CQRS Pattern with MediatR

**Commands** (Write Operations):
- `RegisterCommand` - Register new user
- `LoginCommand` - Authenticate user
- `RefreshTokenCommand` - Refresh access token
- `CreateTaskCommand` - Create new task
- `UpdateTaskCommand` - Update existing task
- `DeleteTaskCommand` - Delete task (soft delete)

**Queries** (Read Operations):
- `GetTasksQuery` - Get paginated list of tasks with filters
- `GetTaskByIdQuery` - Get single task by ID

**Pipeline Behaviors:**
1. **LoggingBehavior** - Logs all requests/responses
2. **ValidationBehavior** - Validates commands/queries using FluentValidation

### Data Access Pattern: No Repository Pattern - Direct DbContext

**Architectural Decision:**

This project **does NOT use the Repository Pattern**. Instead, handlers access data directly through `IApplicationDbContext`.

**Why No Repository Pattern?**

1. **CQRS Already Provides Abstraction**
   - Each handler is already a focused, single-purpose component
   - Handlers act like repository methods (GetTaskById, CreateTask, etc.)
   - Adding repositories would create redundant abstraction layers

2. **IApplicationDbContext IS the Abstraction**
   - Provides interface for testing (can mock)
   - Decouples Application from Infrastructure
   - Satisfies Clean Architecture dependency rules
   - Application layer depends on interface, not concrete implementation

3. **EF Core is Already Unit of Work + Repository**
   - DbContext implements Unit of Work pattern
   - DbSet<T> acts like a repository
   - LINQ provides query abstraction
   - Change tracking handles updates automatically

4. **Reduced Complexity**
   - Fewer classes to maintain
   - Less boilerplate code
   - Direct, transparent data access
   - Easier to optimize queries (no hidden abstractions)

**Code Example:**

```csharp
// Handler accesses DbContext directly
public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, Result<PaginatedList<TaskDto>>>
{
    private readonly IApplicationDbContext _context; // Interface abstraction

    public async Task<Result<PaginatedList<TaskDto>>> Handle(...)
    {
        var query = _context.Tasks // Direct DbSet access
            .Where(t => t.UserId == _currentUser.UserId)
            .Where(t => !t.IsDeleted);

        return await query
            .ProjectTo<TaskDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.Page, request.PageSize);
    }
}
```

**Benefits:**
- ✅ Clean Architecture compliance (depends on interface)
- ✅ Testable (mock IApplicationDbContext)
- ✅ Optimized queries (direct LINQ, ProjectTo, no N+1)
- ✅ Less code duplication
- ✅ Transparent data access

**When Repository Pattern IS Useful:**
- Multiple data sources (SQL + NoSQL + API)
- Complex query logic shared across multiple handlers
- Need to hide data access technology from Application layer
- Legacy codebase without CQRS

**When to Avoid Repository Pattern:**
- Using CQRS with MediatR (like this project) ✅
- Single data source (just EF Core) ✅
- Handlers are already single-purpose ✅
- Modern .NET projects with IQueryable ✅

### Result Pattern

**Purpose:** Replace exceptions for business logic errors

**Implementation:**
- `Result` - For operations without return value (delete, update without return)
- `Result<T>` - For operations that return value (get, create)

**Usage:**
```csharp
var result = await _mediator.Send(command);
if (result.IsFailure)
    return BadRequest(new { error = result.Error });
return Ok(result.Value);
```

**Benefits:**
- Explicit error handling
- No exception overhead
- Type-safe
- Forces handling of both success/failure cases

### Security Features

#### 1. JWT Authentication
- **Access Token:** 15 minutes expiration
- **Refresh Token:** 7 days expiration, stored in database
- **Token Rotation:** New refresh token on each refresh
- **Reuse Detection:** Revokes token family if reused refresh token detected

#### 2. Password Security
- BCrypt hashing with salt
- Minimum 8 characters
- Must contain: uppercase, lowercase, digit, special character

#### 3. Account Lockout
- 5 failed login attempts → 15 minute lockout
- Automatic unlock after lockout period
- Reset attempts on successful login

#### 4. Security Headers
- CORS configured
- HTTPS enforcement (production)
- Rate limiting (planned)

### Validation Strategy

**FluentValidation** for all commands/queries:

**Auth Validators:**
- `LoginCommandValidator` - Email format, password not empty
- `RegisterCommandValidator` - Email, password strength, confirm password match
- `RefreshTokenCommandValidator` - Token not empty

**Task Validators:**
- `CreateTaskCommandValidator` - Title required (max 200 chars), priority valid
- `UpdateTaskCommandValidator` - Same as create + status valid
- `GetTasksQueryValidator` - Page ≥ 1, PageSize 1-100

**Validation Pipeline:** Executed automatically before handler via `ValidationBehavior`

### Caching Strategy

**Redis Distributed Cache:**
- Cache-Aside pattern (Lazy Loading)
- TTL: 5-30 minutes depending on data
- Keys format: `entity:id:attribute` (e.g., `tasks:user:123:page:1`)
- Invalidation: Manual on write operations

**Benefits:**
- Reduces database load
- 10-50x faster than DB queries
- Shared across multiple API instances
- Persistent across app restarts

---

## API Endpoints

### Authentication Endpoints

**POST /api/auth/register**
- Register new user
- Returns: JWT tokens (auto-login)

**POST /api/auth/login**
- Authenticate user
- Returns: Access token + Refresh token

**POST /api/auth/refresh**
- Refresh access token
- Requires: Refresh token
- Returns: New access token + New refresh token

### Task Endpoints (Requires Authentication)

**GET /api/tasks**
- Get paginated list of user's tasks
- Query params: page, pageSize, status, priority
- Returns: PaginatedList<TaskDto>

**GET /api/tasks/{id}**
- Get single task by ID
- Returns: TaskDto

**POST /api/tasks**
- Create new task
- Body: CreateTaskRequest
- Returns: TaskDto (201 Created)

**PUT /api/tasks/{id}**
- Update existing task
- Body: UpdateTaskRequest
- Returns: TaskDto

**DELETE /api/tasks/{id}**
- Delete task (soft delete)
- Returns: 204 No Content

---

## Database Schema

### EF Core Migrations ✅

**Status:** Initial migration created successfully!

**Migration:** `20251110184433_InitialCreate`

**Database Tables:**

#### 1. Users Table
```sql
CREATE TABLE "Users" (
    "Id" uuid PRIMARY KEY,
    "Email" varchar(254) NOT NULL UNIQUE,
    "PasswordHash" varchar(60) NOT NULL,
    "Role" text NOT NULL,
    "FailedLoginAttempts" integer NOT NULL,
    "IsLockedOut" boolean NOT NULL,
    "LockedOutUntil" timestamptz NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamptz NULL
);

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users"("Email");
```

**Explanation:**
- **Email**: Value Object mapped to string column with unique constraint
- **PasswordHash**: BCrypt hash (60 chars fixed length)
- **Role**: Enum stored as string ("User" or "Admin")
- **Account Lockout**: FailedLoginAttempts, IsLockedOut, LockedOutUntil
- **Soft Delete**: IsDeleted flag + DeletedAt timestamp

#### 2. Tasks Table
```sql
CREATE TABLE "Tasks" (
    "Id" uuid PRIMARY KEY,
    "Title" varchar(200) NOT NULL,
    "Description" varchar(2000) NULL,
    "DueDate" timestamptz NULL,
    "Priority" text NOT NULL,
    "Status" text NOT NULL,
    "UserId" uuid NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamptz NULL
);

CREATE INDEX "IX_Tasks_UserId" ON "Tasks"("UserId");
CREATE INDEX "IX_Tasks_Status" ON "Tasks"("Status");
CREATE INDEX "IX_Tasks_CreatedAt" ON "Tasks"("CreatedAt");
```

**Explanation:**
- **Title**: Required, max 200 characters
- **Description**: Optional, max 2000 characters
- **Priority & Status**: Enums stored as strings
- **UserId**: Foreign key to Users (indexed for performance)
- **Indexes**: UserId, Status, CreatedAt for fast queries
- **Soft Delete**: Global query filter applied (`!IsDeleted`)

#### 3. RefreshTokens Table
```sql
CREATE TABLE "RefreshTokens" (
    "Id" uuid PRIMARY KEY,
    "Token" varchar(200) NOT NULL UNIQUE,
    "UserId" uuid NOT NULL,
    "ExpiresAt" timestamptz NOT NULL,
    "IsUsed" boolean NOT NULL,
    "IsRevoked" boolean NOT NULL,
    "ParentTokenId" uuid NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamptz NULL
);

CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON "RefreshTokens"("Token");
CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens"("UserId");
CREATE INDEX "IX_RefreshTokens_ExpiresAt" ON "RefreshTokens"("ExpiresAt");
```

**Explanation:**
- **Token**: Unique refresh token string
- **Rotation**: ParentTokenId tracks token families
- **Security**: IsUsed, IsRevoked for reuse detection
- **Indexes**: Token (unique), UserId, ExpiresAt for cleanup

**Database Provider:** PostgreSQL (configured for production)

**Apply Migration:**
```bash
dotnet ef database update --project src/TaskManagement.Infrastructure --startup-project src/TaskManagement.API
```

---

## Implementation Status Summary

### ✅ Completed

1. **Domain Layer** ✅
   - User entity with rich domain model
   - TaskItem entity with business logic
   - Email Value Object
   - Enums (TaskStatus, TaskPriority, UserRole)
   - Domain exceptions
   - All entities fully documented in Spanish

2. **Application Layer** ✅
   - CQRS Commands & Queries
   - MediatR handlers
   - FluentValidation validators
   - AutoMapper profiles
   - Pipeline behaviors (Logging, Validation)
   - Result pattern for error handling
   - DTOs for all operations
   - DependencyInjection configuration

3. **Infrastructure Layer** ✅
   - ApplicationDbContext with EF Core
   - Entity configurations (Value Objects, Enums)
   - PasswordHasher service (BCrypt)
   - TokenService (JWT generation)
   - CacheService (Redis, Cache-Aside pattern)
   - CurrentUserService (claims extraction)
   - DependencyInjection configuration

4. **API Layer** ✅
   - AuthController (register, login, refresh)
   - TasksController (CRUD operations)
   - RESTful conventions
   - Global exception handler middleware
   - Swagger/OpenAPI documentation
   - CORS configuration
   - JWT authentication middleware

5. **Database** ✅
   - EF Core migrations configured
   - Initial migration created
   - PostgreSQL schema defined
   - Proper indexes for performance
   - Soft delete support

6. **Security** ✅
   - JWT access tokens (15 min)
   - Refresh tokens with rotation (7 days)
   - BCrypt password hashing
   - Account lockout (5 attempts → 15 min)
   - Role-based authorization
   - Reuse detection for refresh tokens

---

## Next Steps

### Pending Implementation

1. **Database Setup** 🔲
   - Install PostgreSQL locally or use Docker
   - Update appsettings.json with connection string
   - Run `dotnet ef database update` to create schema
   - Optionally seed test data

2. **Integration Tests** 🔲
   - API endpoint tests
   - Database integration tests
   - Authentication flow tests

3. **Unit Tests** 🔲
   - Handler tests
   - Validator tests
   - Domain entity tests

4. **Docker Configuration** 🔲
   - Dockerfile for API
   - Docker Compose (API + PostgreSQL + Redis)
   - Development environment setup

5. **CI/CD Pipeline** 🔲
   - GitHub Actions
   - Automated testing
   - Build and deploy

6. **Documentation** 🔲
   - API documentation (Swagger)
   - Setup guide
   - Architecture documentation

---

## Commit History

### Commit 1: Initial project setup ✅
- Created solution structure with Clean Architecture layers
- Configured project dependencies
- Added configuration files (.gitignore, .editorconfig, etc.)
- **Date:** 2025-11-10
- **Hash:** 5229dec

### Commit 2: Fixed compilation errors ✅
- Fixed Result pattern usage in all handlers
- Fixed CacheService implementation
- Added constructors to commands/queries
- Created AddApplication extension method
- Fixed namespace imports
- Created Claude.md for development tracking
- **Status:** BUILD SUCCESSFUL (0 errors)
- **Date:** 2025-11-10
- **Hash:** 7b3d70b

### Commit 3: Database configuration and migrations ✅
- Fixed ApplicationDbContext Email Value Object configuration
- Added EF Core Design package
- Created initial migration (20251110184433_InitialCreate)
- Updated Claude.md with comprehensive documentation
- Added Repository Pattern explanation
- **Date:** 2025-11-10
- **Hash:** c55cde0
- **Status:** COMMITTED

### Commit 4: Frontend development complete ✅
- React 19.2.0 + TypeScript 5.9.3 + Vite 7.2.2 setup
- 8 reusable components (Button, Input, Alert, Card, Spinner, TaskCard, TaskList, TaskForm)
- Axios JWT interceptors with token rotation
- AuthContext for global state management
- Services layer (api, auth, tasks)
- TypeScript types for all DTOs
- Form validation (frontend + backend)
- Comprehensive Spanish documentation
- **Date:** 2025-11-11
- **Hash:** b1dcc69
- **Status:** COMMITTED

---

## Notes

- All explanations are in Spanish (as per project documentation style)
- Extensive inline documentation for educational purposes
- Following Microsoft C# coding standards
- Security-first approach throughout implementation
- React components follow composition and controlled component patterns
- Clean Architecture principles applied to both backend and frontend

---

**Last Updated:** 2025-11-11
**Build Status:** ✅ SUCCESS (Backend: 0 errors, 31 warnings | Frontend: No build errors)
**Next Phase:** Page Components & Routing (React Router)
