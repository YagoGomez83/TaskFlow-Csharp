# Sprint 1 Plan - Core Authentication & CRUD

**Duración:** 5 días (1 semana)
**Capacity:** 55 Story Points
**Fechas:** Semana 1

---

## Sprint Goal

> "Establecer fundamentos de arquitectura, autenticación JWT con refresh token rotation, y operaciones CRUD completas de tareas con logging y secrets management."

Al finalizar este sprint, un usuario debe poder registrarse, autenticar, crear tareas, listar, actualizar y eliminar con soft delete, todo con seguridad básica y logs estructurados.

---

## User Stories Incluidas

| ID | User Story | Story Points | Prioridad |
|----|-----------|--------------|-----------|
| US-01 | Registro de Usuario | 5 SP | Alta |
| US-02 | Login con JWT | 8 SP | Alta |
| US-03 | Refresh Token Rotation | 13 SP | Alta |
| US-04 | Crear Tarea | 5 SP | Alta |
| US-05 | Listar Tareas con Paginación | 8 SP | Alta |
| US-07 | Actualizar Tarea | 5 SP | Alta |
| US-08 | Eliminar Tarea (Soft Delete) | 3 SP | Media |
| US-11 | Logging con Serilog | 5 SP | Alta |
| US-14 | Secrets Management | 3 SP | Alta |
| **TOTAL** | | **55 SP** | |

---

## Objetivos Técnicos

### Arquitectura
- [ ] Crear solution (.sln) con 4 proyectos: Domain, Application, Infrastructure, API
- [ ] Configurar Clean Architecture con dependencias correctas
- [ ] Implementar SOLID principles en diseño

### Domain Layer
- [ ] Entity `User` con validaciones y lógica de account lockout
- [ ] Entity `TaskItem` con estados y prioridades
- [ ] Entity `RefreshToken` para token rotation
- [ ] Value Objects: `Email`
- [ ] Enums: `UserRole`, `TaskStatus`, `TaskPriority`
- [ ] Domain Exceptions: `DomainException`, `EntityNotFoundException`

### Application Layer
- [ ] MediatR configurado para CQRS
- [ ] Commands: `RegisterCommand`, `LoginCommand`, `RefreshTokenCommand`, `CreateTaskCommand`, `UpdateTaskCommand`, `DeleteTaskCommand`
- [ ] Queries: `GetTasksQuery`, `GetTaskByIdQuery`
- [ ] Handlers para todos los commands/queries
- [ ] DTOs: `AuthResponse`, `TaskDto`, request models
- [ ] FluentValidation para todos los requests
- [ ] AutoMapper profiles
- [ ] Behaviors: `ValidationBehavior`, `LoggingBehavior`

### Infrastructure Layer
- [ ] Entity Framework Core configurado con PostgreSQL
- [ ] DbContext con Fluent API configurations
- [ ] Redis configurado para refresh tokens
- [ ] `TokenService` para JWT generation/validation
- [ ] `PasswordHasher` con BCrypt (work factor 12)
- [ ] `CurrentUserService` para extraer claims
- [ ] Migrations iniciales

### API Layer
- [ ] `AuthController`: Register, Login, RefreshToken endpoints
- [ ] `TasksController`: CRUD completo
- [ ] `HealthController`: Health check endpoint
- [ ] JWT authentication configurado
- [ ] Middleware: `ExceptionHandlingMiddleware`, `RequestLoggingMiddleware`
- [ ] Serilog configurado (Console, File, Seq)
- [ ] Swagger configurado solo para Development
- [ ] User-secrets configurado

### Testing
- [ ] Proyecto de Unit Tests creado
- [ ] Proyecto de Integration Tests creado
- [ ] Tests para Domain entities
- [ ] Tests para Application handlers
- [ ] Tests para API endpoints
- [ ] Coverage > 80%

---

## Daily Tasks Breakdown

### Día 1 - Setup & Domain (11 SP)
**Objetivo:** Estructura de proyectos y Domain layer completo

- [ ] Crear solution y proyectos
- [ ] Configurar referencias entre proyectos
- [ ] Implementar Domain entities: User, TaskItem, RefreshToken
- [ ] Implementar Value Objects y Enums
- [ ] Escribir tests unitarios para Domain
- [ ] **Deliverable:** Domain layer completo y testeado

### Día 2 - Authentication (26 SP)
**Objetivo:** Auth completo con JWT y Refresh Token Rotation

- [ ] Configurar EF Core + PostgreSQL
- [ ] Crear migraciones iniciales
- [ ] Implementar commands: Register, Login, RefreshToken
- [ ] Implementar `TokenService` y `PasswordHasher`
- [ ] Configurar JWT authentication
- [ ] Implementar endpoints de Auth
- [ ] Tests de integración para auth
- [ ] **Deliverable:** Auth flow end-to-end funcionando

### Día 3 - Task CRUD Parte 1 (13 SP)
**Objetivo:** Crear y Listar tareas

- [ ] Implementar `CreateTaskCommand` y handler
- [ ] Configurar validaciones con FluentValidation
- [ ] Configurar AutoMapper
- [ ] Implementar `GetTasksQuery` con paginación
- [ ] Configurar Redis para cache
- [ ] Implementar endpoints Create y Get Tasks
- [ ] Tests para CRUD
- [ ] **Deliverable:** Crear y listar tareas funcionando

### Día 4 - Task CRUD Parte 2 & Logging (10 SP)
**Objetivo:** Completar CRUD, logging y secrets

- [ ] Implementar `UpdateTaskCommand` y handler
- [ ] Implementar `DeleteTaskCommand` con soft delete
- [ ] Configurar query filters para soft delete
- [ ] Configurar Serilog con múltiples sinks
- [ ] Implementar `RequestLoggingMiddleware`
- [ ] Configurar user-secrets
- [ ] Documentar secrets management
- [ ] **Deliverable:** CRUD completo, logging operativo

### Día 5 - Testing & Refinamiento (5 SP)
**Objetivo:** Tests completos y polish

- [ ] Completar suite de tests unitarios
- [ ] Completar suite de tests de integración
- [ ] Verificar coverage > 80%
- [ ] Fix de bugs encontrados
- [ ] Documentación Swagger completa
- [ ] Code review interno
- [ ] **Deliverable:** Sprint 1 completo y listo para demo

---

## Definition of Done

Una User Story se considera DONE cuando:

- [ ] Código implementado según criterios de aceptación
- [ ] Tests unitarios escritos y pasando (>80% coverage)
- [ ] Tests de integración escritos y pasando
- [ ] Code review aprobado por al menos un par
- [ ] Sin warnings de compilación
- [ ] Documentación técnica actualizada
- [ ] Swagger documentation actualizada
- [ ] Sin vulnerabilidades críticas (SAST)
- [ ] Logging implementado para eventos relevantes
- [ ] Funcionando en entorno de desarrollo local

---

## Sprint Retrospective (Al finalizar)

### Preguntas para Retrospectiva:

1. **What went well?** ¿Qué funcionó bien en este sprint?
2. **What didn't go well?** ¿Qué no funcionó bien?
3. **What can we improve?** ¿Qué podemos mejorar para el próximo sprint?
4. **Action items** Acciones concretas para Sprint 2

---

## Dependencias Externas

- PostgreSQL 15+ instalado o Docker disponible
- Redis 7+ instalado o Docker disponible
- .NET 8 SDK instalado
- Visual Studio 2022 / VS Code / Rider

---

## Riesgos Identificados

| Riesgo | Impacto | Probabilidad | Mitigación |
|--------|---------|--------------|------------|
| Complejidad de Refresh Token Rotation (US-03) | Alto | Media | Dedicar más tiempo, pair programming |
| Setup de EF Core + PostgreSQL | Medio | Baja | Docker Compose para facilitar setup |
| Coverage < 80% | Medio | Media | Priorizar tests desde día 1 |

---

## Notas Adicionales

- **Pair Programming:** Recomendado para US-03 (Refresh Token Rotation) por complejidad
- **Code Reviews:** Diarios al finalizar cada día
- **Stand-ups:** Diarios a las 9:00 AM (15 min máximo)
- **Sprint Review:** Viernes final del sprint
- **Sprint Retrospective:** Viernes después de review

---

**Preparado por:** Senior Full-Stack Developer & DevSecOps Engineer
**Última actualización:** 2025-01-09
