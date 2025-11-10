# Product Backlog - TaskManagement API

**Proyecto:** TaskManagement API
**Product Owner:** Senior Full-Stack Developer & DevSecOps Engineer
**Última actualización:** 2025-01-09
**Total Story Points:** 140 SP

---

## Épicas del Proyecto

1. **Authentication & Authorization** (34 SP)
2. **Task Management CRUD** (29 SP)
3. **Security Hardening** (22 SP)
4. **DevOps & Infrastructure** (29 SP)
5. **Frontend & Testing** (26 SP)

---

## Backlog Priorizado

### ÉPICA 1: Authentication & Authorization (34 SP)

#### US-01: Registro de Usuario
**Prioridad:** Alta | **Story Points:** 5 | **Sprint:** 1

**Como** visitante del sistema
**Quiero** registrarme con email y contraseña
**Para** poder acceder a la gestión de mis tareas

**Criterios de Aceptación:**
- La contraseña debe tener mínimo 8 caracteres, 1 mayúscula, 1 número y 1 carácter especial
- El email debe ser único en el sistema (validación en DB)
- La contraseña se hashea con BCrypt (cost factor 12)
- Se valida el formato del email con regex
- Se retorna token JWT tras registro exitoso
- Se implementa rate limiting (5 intentos por minuto por IP)
- Se loguea el evento de registro con Serilog

**Tareas Técnicas:**
- [ ] Crear entity `User` en Domain con validaciones
- [ ] Crear `RegisterCommand` y `RegisterCommandHandler` en Application
- [ ] Implementar `RegisterRequestValidator` con FluentValidation
- [ ] Crear `AuthController.Register` endpoint
- [ ] Implementar `IPasswordHasher` con BCrypt en Infrastructure
- [ ] Configurar rate limiting para endpoint /api/auth/register
- [ ] Escribir tests unitarios para validaciones
- [ ] Escribir tests de integración para endpoint completo

**Definition of Done:**
- [ ] Código revisado y aprobado por par
- [ ] Tests unitarios con >80% coverage
- [ ] Tests de integración pasando
- [ ] Documentación Swagger actualizada
- [ ] Validación manual realizada
- [ ] Logging verificado en desarrollo

---

#### US-02: Login con JWT
**Prioridad:** Alta | **Story Points:** 8 | **Sprint:** 1

**Como** usuario registrado
**Quiero** autenticarme con email y contraseña
**Para** obtener un token de acceso y poder usar la API

**Criterios de Aceptación:**
- Se genera access token (15 min expiración) y refresh token (7 días)
- Los tokens contienen claims: userId, email, role
- Se implementa account lockout tras 5 intentos fallidos (15 minutos)
- Cada intento de login se registra en logs con timestamp e IP
- Timing-safe comparison para prevenir user enumeration
- Se retorna 401 Unauthorized con mensaje genérico en credenciales inválidas
- Se retorna 403 Forbidden si la cuenta está bloqueada

**Tareas Técnicas:**
- [ ] Crear `LoginCommand` y `LoginCommandHandler`
- [ ] Implementar `ITokenService` en Application (interface)
- [ ] Implementar `TokenService` en Infrastructure con JWT generation
- [ ] Agregar propiedades `FailedLoginAttempts`, `IsLockedOut` a User entity
- [ ] Implementar método `User.RecordFailedLogin()` con lógica de lockout
- [ ] Crear `AuthController.Login` endpoint
- [ ] Configurar JWT authentication en Program.cs
- [ ] Implementar `ISecurityAuditLogger` para eventos de login
- [ ] Tests de seguridad: timing attacks, account lockout

**Definition of Done:**
- [ ] JWT generation funcionando correctamente
- [ ] Account lockout activándose tras 5 intentos
- [ ] Logs de seguridad registrándose
- [ ] Tests de integración con múltiples escenarios
- [ ] Documentación de claims en Swagger
- [ ] Validación de security requirements

---

#### US-03: Refresh Token Rotation
**Prioridad:** Alta | **Story Points:** 13 | **Sprint:** 1

**Como** usuario autenticado
**Quiero** renovar mi token cuando expire
**Para** mantener mi sesión activa sin volver a loguearme

**Criterios de Aceptación:**
- Se invalida el refresh token anterior tras uso (rotation)
- Se genera nuevo par de tokens (access + refresh)
- Se detecta reuso de refresh token → posible ataque → revocar todos los tokens del usuario
- Refresh tokens se almacenan en Redis con TTL de 7 días
- Se implementa "token family" para rastrear jerarquía de tokens
- Endpoint `/api/auth/refresh` protegido con rate limiting estricto

**Tareas Técnicas:**
- [ ] Crear entity `RefreshToken` en Domain (Token, ExpiresAt, IsUsed, IsRevoked, ParentTokenId)
- [ ] Crear `RefreshTokenCommand` y `RefreshTokenCommandHandler`
- [ ] Implementar lógica de detección de reuso
- [ ] Implementar token family tracking
- [ ] Configurar Redis para almacenamiento de refresh tokens
- [ ] Crear endpoint `AuthController.RefreshToken`
- [ ] Implementar alerta de seguridad al detectar reuso
- [ ] Tests de seguridad: token reuse, expiration, revocation

**Definition of Done:**
- [ ] Token rotation funcionando end-to-end
- [ ] Detección de reuso verificada
- [ ] Redis integrado y configurado
- [ ] Alertas de seguridad logueándose
- [ ] Tests de seguridad pasando
- [ ] Documentación de flujo de refresh en docs/

---

#### US-09: Roles y Permisos (RBAC)
**Prioridad:** Alta | **Story Points:** 8 | **Sprint:** 2

**Como** administrador del sistema
**Quiero** gestionar roles de usuarios
**Para** controlar accesos según permisos

**Criterios de Aceptación:**
- Roles implementados: `Admin`, `User`
- Admin puede ver todas las tareas de todos los usuarios
- User solo puede ver/editar/eliminar sus propias tareas
- Se implementa `[Authorize(Roles = "Admin")]` en endpoints administrativos
- Se retorna `403 Forbidden` si el usuario no tiene permisos
- Se valida ownership a nivel de handler (no solo controller)

**Tareas Técnicas:**
- [ ] Crear enum `UserRole` en Domain
- [ ] Agregar propiedad `Role` a User entity
- [ ] Implementar `ICurrentUserService` para obtener userId y role desde claims
- [ ] Agregar claim de role en `TokenService.GenerateAccessToken`
- [ ] Implementar verificación de ownership en handlers de tasks
- [ ] Crear endpoint admin `GET /api/admin/tasks` (todas las tareas)
- [ ] Configurar autorización por roles en Program.cs
- [ ] Tests de autorización por roles

**Definition of Done:**
- [ ] RBAC funcionando correctamente
- [ ] Tests de autorización para ambos roles
- [ ] Documentación de permisos por endpoint
- [ ] Validación de edge cases (usuario sin rol, rol inválido)

---

### ÉPICA 2: Task Management CRUD (29 SP)

#### US-04: Crear Tarea
**Prioridad:** Alta | **Story Points:** 5 | **Sprint:** 1

**Como** usuario autenticado
**Quiero** crear una nueva tarea
**Para** organizar mis pendientes

**Criterios de Aceptación:**
- Campos: `Title` (requerido, max 200 chars), `Description` (opcional, max 2000 chars), `DueDate` (opcional, futuro), `Priority` (Low/Medium/High), `Status` (Pending por defecto)
- Se valida con FluentValidation antes de llegar al handler
- Se asocia automáticamente al `UserId` del usuario autenticado
- Se previene XSS sanitizando inputs (sin HTML tags)
- Se retorna `201 Created` con Location header y TaskDto

**Tareas Técnicas:**
- [ ] Crear entity `TaskItem` en Domain
- [ ] Crear enums `TaskStatus`, `TaskPriority` en Domain
- [ ] Crear `CreateTaskCommand` y `CreateTaskCommandHandler`
- [ ] Crear `CreateTaskRequestValidator` con reglas de validación
- [ ] Crear `TaskDto` y mapping con AutoMapper
- [ ] Crear endpoint `TasksController.CreateTask`
- [ ] Implementar sanitización de inputs contra XSS
- [ ] Tests unitarios para validaciones
- [ ] Tests de integración end-to-end

**Definition of Done:**
- [ ] Endpoint funcionando y retornando 201
- [ ] Validaciones aplicándose correctamente
- [ ] XSS prevention verificado
- [ ] Tests con >80% coverage
- [ ] Swagger documentado

---

#### US-05: Listar Tareas con Paginación
**Prioridad:** Alta | **Story Points:** 8 | **Sprint:** 1

**Como** usuario autenticado
**Quiero** ver mis tareas con paginación
**Para** navegar eficientemente por mis pendientes

**Criterios de Aceptación:**
- Parámetros: `pageNumber` (default 1), `pageSize` (default 10, max 100)
- Metadata retornada: `totalItems`, `totalPages`, `currentPage`, `hasNext`, `hasPrevious`
- Solo se retornan tareas del usuario autenticado (excepto Admin)
- Resultados cacheados en Redis con TTL de 5 minutos
- Performance: respuesta < 200ms para 10,000 registros

**Tareas Técnicas:**
- [ ] Crear clase `PaginatedList<T>` en Application/Common
- [ ] Crear `GetTasksQuery` y `GetTasksQueryHandler`
- [ ] Implementar paginación en query con LINQ
- [ ] Integrar Redis cache para resultados
- [ ] Crear endpoint `TasksController.GetTasks`
- [ ] Implementar cache invalidation al crear/editar/eliminar
- [ ] Tests de performance con dataset grande
- [ ] Tests de cache hit/miss

**Definition of Done:**
- [ ] Paginación funcionando correctamente
- [ ] Cache en Redis operativo
- [ ] Performance validado (<200ms)
- [ ] Metadata retornándose correctamente
- [ ] Tests de integración pasando

---

#### US-06: Filtros Avanzados de Tareas
**Prioridad:** Media | **Story Points:** 8 | **Sprint:** 2

**Como** usuario autenticado
**Quiero** filtrar tareas por estado, prioridad y fechas
**Para** encontrar rápidamente lo que necesito

**Criterios de Aceptación:**
- Filtros: `status` (Pending/InProgress/Completed), `priority` (Low/Medium/High), `dueDateBefore`, `dueDateAfter`, `searchTerm` (full-text en título/descripción)
- Se pueden combinar múltiples filtros simultáneamente
- Búsqueda case-insensitive
- Cache de Redis se invalida al aplicar filtros
- SQL injection prevenido usando LINQ/EF Core

**Tareas Técnicas:**
- [ ] Extender `GetTasksQuery` con parámetros de filtro
- [ ] Implementar filtros dinámicos en handler con LINQ
- [ ] Implementar búsqueda full-text con `EF.Functions.Like`
- [ ] Ajustar cache key para incluir filtros
- [ ] Tests de seguridad para SQL injection
- [ ] Tests de combinación de filtros

**Definition of Done:**
- [ ] Filtros funcionando individualmente y combinados
- [ ] SQL injection tests pasando
- [ ] Performance aceptable con filtros
- [ ] Documentación de query parameters en Swagger

---

#### US-07: Actualizar Tarea
**Prioridad:** Alta | **Story Points:** 5 | **Sprint:** 1

**Como** usuario autenticado
**Quiero** modificar una tarea existente
**Para** mantener actualizada mi información

**Criterios de Aceptación:**
- Solo el propietario puede actualizar su tarea (o Admin)
- Se validan todos los campos con FluentValidation
- Se retorna `404 Not Found` si la tarea no existe o no pertenece al usuario
- Se invalida cache de Redis para esa tarea y listados
- Se registra auditoría (quién, cuándo, qué cambió) en logs

**Tareas Técnicas:**
- [ ] Crear `UpdateTaskCommand` y `UpdateTaskCommandHandler`
- [ ] Verificar ownership en handler
- [ ] Crear `UpdateTaskRequestValidator`
- [ ] Actualizar `UpdatedAt` timestamp automáticamente
- [ ] Invalidar cache relevante
- [ ] Loguear cambios para auditoría
- [ ] Crear endpoint `TasksController.UpdateTask`
- [ ] Tests de autorización (user no owner)

**Definition of Done:**
- [ ] Solo owner puede actualizar
- [ ] Validaciones aplicándose
- [ ] Cache invalidándose
- [ ] Auditoría logueándose
- [ ] Tests de ownership pasando

---

#### US-08: Eliminar Tarea (Soft Delete)
**Prioridad:** Media | **Story Points:** 3 | **Sprint:** 1

**Como** usuario autenticado
**Quiero** eliminar una tarea
**Para** mantener limpia mi lista

**Criterios de Aceptación:**
- Se implementa soft delete (`IsDeleted` flag en entity)
- Solo el propietario puede eliminar (o Admin)
- Tareas eliminadas no aparecen en listados normales (query filter)
- Admin puede ver tareas eliminadas con parámetro `?includeDeleted=true`
- Se invalida cache de listados

**Tareas Técnicas:**
- [ ] Agregar propiedad `IsDeleted`, `DeletedAt` a TaskItem entity
- [ ] Configurar query filter global en DbContext
- [ ] Crear `DeleteTaskCommand` y `DeleteTaskCommandHandler`
- [ ] Implementar soft delete en handler
- [ ] Crear endpoint `TasksController.DeleteTask`
- [ ] Invalidar cache al eliminar
- [ ] Tests de soft delete y query filter

**Definition of Done:**
- [ ] Soft delete funcionando
- [ ] Query filter excluyendo eliminadas
- [ ] Admin puede ver eliminadas
- [ ] Cache invalidándose
- [ ] Tests pasando

---

### ÉPICA 3: Security Hardening (22 SP)

#### US-10: Rate Limiting
**Prioridad:** Media | **Story Points:** 5 | **Sprint:** 2

**Como** administrador del sistema
**Quiero** implementar rate limiting
**Para** prevenir abuso de la API

**Criterios de Aceptación:**
- Límites: 100 req/min para endpoints generales, 10 req/min para auth endpoints
- Librería: `AspNetCoreRateLimit` o built-in .NET 7+ RateLimiter
- Se retorna `429 Too Many Requests` con header `Retry-After`
- Se configura por IP y por usuario autenticado
- Whitelist de IPs para servicios internos

**Tareas Técnicas:**
- [ ] Instalar NuGet package `AspNetCoreRateLimit`
- [ ] Configurar rate limiting en Program.cs
- [ ] Crear políticas diferenciadas: general, auth
- [ ] Implementar whitelist de IPs en configuración
- [ ] Tests de rate limiting: exceder límite, verificar 429
- [ ] Documentar límites en README

**Definition of Done:**
- [ ] Rate limiting aplicándose correctamente
- [ ] 429 retornándose al exceder límite
- [ ] Tests verificando límites
- [ ] Documentación actualizada

---

#### US-11: Logging Estructurado con Serilog
**Prioridad:** Alta | **Story Points:** 5 | **Sprint:** 1

**Como** desarrollador
**Quiero** tener logs estructurados
**Para** debugging y auditoría

**Criterios de Aceptación:**
- Serilog configurado con sinks: Console (JSON), File (rolling daily), Seq (opcional)
- Niveles: Debug (desarrollo), Information (producción), Warning, Error
- Se loguean: requests HTTP, errores, cambios en datos, intentos de login, accesos no autorizados
- Se excluyen datos sensibles (passwords, tokens completos)
- Formato JSON estructurado con timestamps UTC

**Tareas Técnicas:**
- [ ] Instalar NuGet packages: Serilog, Serilog.AspNetCore, Serilog.Sinks.Console, Serilog.Sinks.File, Serilog.Sinks.Seq
- [ ] Configurar Serilog en Program.cs
- [ ] Crear RequestLoggingMiddleware
- [ ] Implementar `ISecurityAuditLogger`
- [ ] Configurar enrichers: MachineName, EnvironmentName
- [ ] Tests verificando que datos sensibles no se loguean

**Definition of Done:**
- [ ] Serilog configurado con múltiples sinks
- [ ] Logs estructurados en JSON
- [ ] Datos sensibles protegidos
- [ ] Tests de logging pasando

---

#### US-12: Headers de Seguridad
**Prioridad:** Alta | **Story Points:** 3 | **Sprint:** 2

**Como** DevSecOps Engineer
**Quiero** implementar headers de seguridad
**Para** proteger contra ataques comunes

**Criterios de Aceptación:**
- HSTS: `Strict-Transport-Security: max-age=31536000; includeSubDomains; preload`
- CSP: Content-Security-Policy restrictivo
- `X-Frame-Options: DENY`
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- Validación externa con https://securityheaders.com score A+

**Tareas Técnicas:**
- [ ] Crear `SecurityHeadersMiddleware`
- [ ] Configurar todos los headers de seguridad
- [ ] Registrar middleware en pipeline
- [ ] Validar headers con securityheaders.com
- [ ] Tests verificando presencia de headers

**Definition of Done:**
- [ ] Middleware implementado
- [ ] Score A+ en securityheaders.com
- [ ] Tests de headers pasando

---

#### US-13: CORS Configuración Restrictiva
**Prioridad:** Alta | **Story Points:** 3 | **Sprint:** 2

**Como** DevSecOps Engineer
**Quiero** configurar CORS de forma segura
**Para** permitir solo orígenes autorizados

**Criterios de Aceptación:**
- Lista blanca de orígenes desde `appsettings.json`
- Métodos permitidos: GET, POST, PUT, DELETE
- Headers permitidos: Content-Type, Authorization
- `AllowCredentials: true` para cookies HttpOnly
- Se rechaza con `403 Forbidden` orígenes no autorizados

**Tareas Técnicas:**
- [ ] Configurar CORS policy en Program.cs
- [ ] Crear sección `CorsSettings:AllowedOrigins` en appsettings
- [ ] Tests verificando orígenes bloqueados
- [ ] Documentar configuración CORS en docs/

**Definition of Done:**
- [ ] CORS configurado restrictivamente
- [ ] Tests de orígenes bloqueados pasando
- [ ] Documentación actualizada

---

#### US-14: Secrets Management
**Prioridad:** Alta | **Story Points:** 3 | **Sprint:** 1

**Como** DevSecOps Engineer
**Quiero** gestionar secrets de forma segura
**Para** evitar exposición de credenciales

**Criterios de Aceptación:**
- Desarrollo: `dotnet user-secrets`
- Producción: Variables de entorno
- Archivo `appsettings.json` NO contiene secrets
- `.gitignore` excluye archivos con secrets
- Validación: No hay secrets en código fuente (SAST)

**Tareas Técnicas:**
- [ ] Configurar user-secrets en todos los proyectos
- [ ] Crear `.env.example` con estructura
- [ ] Actualizar `.gitignore` para excluir secrets
- [ ] Documentar secrets management en docs/setup/
- [ ] Agregar verificación de secrets en CI/CD

**Definition of Done:**
- [ ] User-secrets configurado
- [ ] `.env.example` creado
- [ ] Documentación completada
- [ ] SAST no encuentra secrets hardcoded

---

### ÉPICA 4: DevOps & Infrastructure (29 SP)

#### US-15: Dockerización Multi-Stage
**Prioridad:** Media | **Story Points:** 5 | **Sprint:** 2

**Como** DevOps Engineer
**Quiero** crear Dockerfile optimizado
**Para** reducir tamaño de imagen y mejorar seguridad

**Criterios de Aceptación:**
- Dockerfile multi-stage: `build` stage y `runtime` stage
- Imagen base: `mcr.microsoft.com/dotnet/aspnet:8.0-alpine`
- Usuario no root en runtime
- Tamaño de imagen < 200MB
- Health check endpoint `/health` configurado

**Tareas Técnicas:**
- [ ] Crear Dockerfile multi-stage en `docker/api.Dockerfile`
- [ ] Configurar usuario no root
- [ ] Crear health check endpoint
- [ ] Optimizar capas para cache
- [ ] Build y validar tamaño de imagen
- [ ] Documentar Dockerfile en docs/setup/

**Definition of Done:**
- [ ] Dockerfile construyendo exitosamente
- [ ] Imagen < 200MB
- [ ] Health check funcionando
- [ ] Documentación creada

---

#### US-16: Docker Compose Completo
**Prioridad:** Media | **Story Points:** 8 | **Sprint:** 2

**Como** DevOps Engineer
**Quiero** orquestar todos los servicios
**Para** levantar el stack completo con un comando

**Criterios de Aceptación:**
- Servicios: API, PostgreSQL, Redis, Nginx (reverse proxy)
- Health checks configurados para todos
- Volúmenes: `postgres-data`, `redis-data`
- Networks: `backend-network`, `frontend-network`
- Variables de entorno desde `.env` file

**Tareas Técnicas:**
- [ ] Crear `docker-compose.yml` principal
- [ ] Crear `docker-compose.override.yml` para desarrollo
- [ ] Configurar servicios con health checks
- [ ] Configurar volúmenes para persistencia
- [ ] Configurar networks aisladas
- [ ] Crear archivo `.env.example`
- [ ] Documentar uso en docs/setup/docker-setup.md

**Definition of Done:**
- [ ] `docker-compose up` levanta stack completo
- [ ] Health checks funcionando
- [ ] Persistencia de datos validada
- [ ] Documentación completada

---

#### US-17: CI/CD Pipeline con GitHub Actions
**Prioridad:** Media | **Story Points:** 13 | **Sprint:** 3

**Como** DevOps Engineer
**Quiero** automatizar build, test y deploy
**Para** acelerar releases y mantener calidad

**Criterios de Aceptación:**
- Triggers: push a `main`, pull requests
- Steps: restore, build, test, SAST (SonarQube), build Docker image, push to registry
- Deploy automático a staging tras merge a `main`
- Notificaciones: Slack/Email en caso de fallo
- Caching de dependencias NuGet para velocidad

**Tareas Técnicas:**
- [ ] Crear `.github/workflows/ci-cd.yml`
- [ ] Configurar build y test steps
- [ ] Integrar SonarQube para SAST
- [ ] Configurar Docker build y push
- [ ] Configurar deploy a staging
- [ ] Configurar notificaciones
- [ ] Documentar pipeline en docs/

**Definition of Done:**
- [ ] Pipeline ejecutándose en cada push
- [ ] Tests automáticos pasando
- [ ] Deploy a staging funcionando
- [ ] Notificaciones configuradas

---

### ÉPICA 5: Frontend & Testing (26 SP)

#### US-18: Frontend React con TypeScript
**Prioridad:** Media | **Story Points:** 13 | **Sprint:** 3

**Como** usuario final
**Quiero** una interfaz web intuitiva
**Para** gestionar mis tareas fácilmente

**Criterios de Aceptación:**
- Componentes: Login, Register, TaskList, TaskItem, TaskForm, TaskFilters, Pagination
- State management: Context API o Zustand
- API integration con Axios e interceptors
- Tokens en `localStorage` con manejo de expiración
- Rutas protegidas con React Router
- UI: Tailwind CSS
- Responsive design

**Tareas Técnicas:**
- [ ] Configurar proyecto React con Vite + TypeScript
- [ ] Crear estructura de carpetas
- [ ] Implementar AuthContext
- [ ] Crear servicio API con Axios
- [ ] Implementar componentes de Auth
- [ ] Implementar componentes de Tasks
- [ ] Configurar React Router
- [ ] Implementar refresh token en interceptor
- [ ] Styling con Tailwind CSS

**Definition of Done:**
- [ ] Frontend funcional end-to-end
- [ ] Integración con API exitosa
- [ ] Responsive en móvil/tablet/desktop
- [ ] Documentación de componentes

---

#### US-19: Tests Unitarios e Integración
**Prioridad:** Alta | **Story Points:** 13 | **Sprint:** 3

**Como** desarrollador
**Quiero** suite de tests completa
**Para** garantizar calidad y prevenir regresiones

**Criterios de Aceptación:**
- Coverage mínimo: 80%
- Backend: xUnit para tests unitarios, WebApplicationFactory para integración
- Frontend: Jest + React Testing Library
- Mocks con Moq para repositorios
- Tests de seguridad incluidos

**Tareas Técnicas:**
- [ ] Configurar proyectos de tests
- [ ] Escribir tests unitarios para Domain
- [ ] Escribir tests unitarios para Application handlers
- [ ] Escribir tests de integración para API endpoints
- [ ] Escribir tests frontend con Jest
- [ ] Configurar coverage reporting
- [ ] Integrar tests en CI/CD

**Definition of Done:**
- [ ] Coverage ≥ 80%
- [ ] Todos los tests pasando
- [ ] Tests integrados en CI/CD
- [ ] Coverage report generándose

---

#### US-20: Documentación API con Swagger
**Prioridad:** Media | **Story Points:** 3 | **Sprint:** 2

**Como** desarrollador consumer de la API
**Quiero** documentación interactiva
**Para** entender y probar endpoints

**Criterios de Aceptación:**
- Swagger UI habilitado solo en Development
- Documentación de schemas, responses, errores
- Ejemplos de requests y responses
- JWT authentication configurada en Swagger UI
- Descripciones claras de cada endpoint

**Tareas Técnicas:**
- [ ] Configurar Swashbuckle.AspNetCore
- [ ] Agregar XML comments a controllers
- [ ] Configurar autenticación JWT en Swagger
- [ ] Agregar ejemplos con atributos
- [ ] Deshabilitar Swagger en Production

**Definition of Done:**
- [ ] Swagger UI funcionando en desarrollo
- [ ] Documentación completa y clara
- [ ] JWT auth funcionando en UI
- [ ] Deshabilitado en producción

---

## Métricas del Proyecto

### Distribución de Story Points

| Épica | Story Points | Porcentaje |
|-------|--------------|------------|
| Authentication & Authorization | 34 SP | 24% |
| Task Management CRUD | 29 SP | 21% |
| Security Hardening | 22 SP | 16% |
| DevOps & Infrastructure | 29 SP | 21% |
| Frontend & Testing | 26 SP | 18% |
| **TOTAL** | **140 SP** | **100%** |

### Prioridades

- **Alta:** 13 User Stories (94 SP) - 67%
- **Media:** 7 User Stories (46 SP) - 33%

### Distribución por Sprint

- **Sprint 1:** 55 SP (39%)
- **Sprint 2:** 43 SP (31%)
- **Sprint 3:** 42 SP (30%)

---

## Notas y Dependencias

### Dependencias Críticas

```
US-01 (Registro) → US-02 (Login) → US-03 (Refresh Token)
                         ↓
                    US-09 (RBAC)
                         ↓
    US-04, US-07, US-08 (CRUD) → US-05 (Listar) → US-06 (Filtros)
                         ↓
                US-15, US-16 (Docker)
                         ↓
                  US-17 (CI/CD)
                         ↓
                  US-18 (Frontend)
```

### Riesgos Identificados

1. **Complejidad de Refresh Token Rotation (US-03):** 13 SP - Puede requerir más tiempo
2. **Integración CI/CD (US-17):** Depende de infraestructura externa (GitHub, Docker registry)
3. **Performance con 10k registros (US-05):** Puede requerir optimización adicional

---

**Este Product Backlog es un documento vivo y se actualizará continuamente durante el proyecto.**
