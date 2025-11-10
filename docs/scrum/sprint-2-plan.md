# Sprint 2 Plan - Security Hardening & Infrastructure

**Duración:** 5 días (1 semana)
**Capacity:** 43 Story Points
**Fechas:** Semana 2

---

## Sprint Goal

> "Fortificar la seguridad de la API con OWASP Top 10 mitigations, implementar control de acceso robusto (RBAC), containerizar la aplicación con Docker, y preparar infraestructura production-ready."

Al finalizar este sprint, la API estará protegida contra vulnerabilidades comunes, con control de acceso por roles, completamente containerizada y lista para deploy.

---

## User Stories Incluidas

| ID | User Story | Story Points | Prioridad |
|----|-----------|--------------|-----------|
| US-06 | Filtros Avanzados de Tareas | 8 SP | Media |
| US-09 | RBAC - Roles y Permisos | 8 SP | Alta |
| US-10 | Rate Limiting | 5 SP | Media |
| US-12 | Headers de Seguridad | 3 SP | Alta |
| US-13 | CORS Restrictivo | 3 SP | Alta |
| US-15 | Dockerización Multi-Stage | 5 SP | Media |
| US-16 | Docker Compose Completo | 8 SP | Media |
| US-20 | Swagger Documentation | 3 SP | Media |
| **TOTAL** | | **43 SP** | |

---

## Objetivos Técnicos

### Security Hardening
- [ ] Implementar RBAC con roles Admin y User
- [ ] Configurar Rate Limiting con políticas diferenciadas
- [ ] Implementar Security Headers Middleware
- [ ] Configurar CORS restrictivo con whitelist
- [ ] Validar seguridad con herramientas externas (securityheaders.com)

### Advanced Features
- [ ] Filtros avanzados: status, priority, dates, searchTerm
- [ ] Búsqueda full-text en tareas
- [ ] Optimización de queries con índices

### Infrastructure
- [ ] Dockerfile multi-stage optimizado
- [ ] Docker Compose con 4 servicios: API, PostgreSQL, Redis, Nginx
- [ ] Health checks para todos los servicios
- [ ] Volúmenes para persistencia
- [ ] Networks aisladas

### Documentation
- [ ] Swagger con autenticación JWT configurada
- [ ] Ejemplos de requests/responses
- [ ] Documentación de roles y permisos

---

## Daily Tasks Breakdown

### Día 1 - RBAC & Authorization (8 SP)
**Objetivo:** Control de acceso por roles

- [ ] Implementar enum `UserRole` en Domain
- [ ] Agregar claim de role en JWT
- [ ] Implementar `ICurrentUserService` con role
- [ ] Agregar verificación de ownership en handlers
- [ ] Crear endpoints admin (GET /api/admin/tasks)
- [ ] Configurar `[Authorize(Roles = "Admin")]`
- [ ] Tests de autorización por roles
- [ ] **Deliverable:** RBAC funcionando end-to-end

### Día 2 - Advanced Filters & Security Headers (11 SP)
**Objetivo:** Filtros avanzados y headers de seguridad

- [ ] Extender `GetTasksQuery` con parámetros de filtro
- [ ] Implementar filtros dinámicos en handler
- [ ] Implementar búsqueda full-text con LINQ
- [ ] Tests de SQL injection prevention
- [ ] Implementar `SecurityHeadersMiddleware`
- [ ] Configurar todos los security headers
- [ ] Validar con securityheaders.com
- [ ] **Deliverable:** Filtros funcionando, Security headers con score A+

### Día 3 - Rate Limiting & CORS (8 SP)
**Objetivo:** Protección contra abuso y CORS seguro

- [ ] Instalar y configurar `AspNetCoreRateLimit`
- [ ] Crear políticas: general (100 req/min), auth (10 req/min)
- [ ] Implementar whitelist de IPs
- [ ] Configurar CORS policy con whitelist de orígenes
- [ ] Mover orígenes permitidos a `appsettings.json`
- [ ] Tests de rate limiting (exceder límite)
- [ ] Tests de CORS (orígenes bloqueados)
- [ ] **Deliverable:** Rate limiting y CORS operativos

### Día 4 - Docker & Docker Compose (13 SP)
**Objetivo:** Containerización completa

- [ ] Crear Dockerfile multi-stage en `docker/api.Dockerfile`
- [ ] Optimizar para tamaño < 200MB
- [ ] Configurar usuario no root
- [ ] Crear `docker-compose.yml` con 4 servicios
- [ ] Configurar PostgreSQL con volumen persistente
- [ ] Configurar Redis con volumen persistente
- [ ] Configurar Nginx como reverse proxy
- [ ] Configurar health checks
- [ ] Crear `.env.example`
- [ ] Validar que `docker-compose up` funciona
- [ ] **Deliverable:** Stack completo levantando con Docker Compose

### Día 5 - Documentation & Testing (3 SP)
**Objetivo:** Documentación Swagger y tests de seguridad

- [ ] Configurar Swashbuckle con XML comments
- [ ] Agregar ejemplos de requests/responses
- [ ] Configurar JWT auth en Swagger UI
- [ ] Deshabilitar Swagger en Production
- [ ] Completar tests de seguridad
- [ ] Penetration testing básico manual
- [ ] Documentar setup de Docker en `docs/setup/docker-setup.md`
- [ ] Code review y refinamiento
- [ ] **Deliverable:** Swagger documentado, tests de seguridad pasando

---

## Definition of Done

Una User Story se considera DONE cuando:

- [ ] Código implementado según criterios de aceptación
- [ ] Security validations pasando
- [ ] Tests de seguridad específicos escritos y pasando
- [ ] Code review aprobado con enfoque en seguridad
- [ ] Documentación de seguridad actualizada
- [ ] SAST ejecutado sin vulnerabilidades críticas
- [ ] Funcionando en Docker containers
- [ ] Swagger documentation actualizada
- [ ] Performance validado (donde aplique)

---

## Security Validation Checklist

- [ ] **OWASP A01 (Broken Access Control):**
  - [ ] RBAC implementado
  - [ ] Ownership validation en handlers
  - [ ] Tests de autorización pasando

- [ ] **OWASP A03 (Injection):**
  - [ ] Filtros usando LINQ/EF Core (no SQL raw)
  - [ ] Input validation con FluentValidation
  - [ ] Tests de SQL injection intentados y bloqueados

- [ ] **OWASP A05 (Security Misconfiguration):**
  - [ ] Security headers implementados (HSTS, CSP, X-Frame-Options, etc.)
  - [ ] CORS restrictivo
  - [ ] Error messages no revelando información sensible
  - [ ] Score A+ en securityheaders.com

- [ ] **OWASP A07 (Identification Failures):**
  - [ ] RBAC validado
  - [ ] Account lockout funcionando
  - [ ] Refresh token rotation validado

- [ ] **Rate Limiting:**
  - [ ] Límites aplicándose correctamente
  - [ ] 429 Too Many Requests retornándose

---

## Docker Validation Checklist

- [ ] `docker build` exitoso
- [ ] Imagen < 200MB
- [ ] Container corre con usuario no root
- [ ] Health check endpoint respondiendo
- [ ] `docker-compose up` levanta todos los servicios
- [ ] Servicios pueden comunicarse entre sí
- [ ] Datos persisten tras restart de containers
- [ ] Logs accesibles con `docker-compose logs`

---

## Riesgos Identificados

| Riesgo | Impacto | Probabilidad | Mitigación |
|--------|---------|--------------|------------|
| Configuración compleja de Nginx | Medio | Media | Usar configuración estándar probada |
| Performance de filtros complejos | Medio | Media | Índices de DB, profiling de queries |
| Docker networking issues | Alto | Baja | Documentación clara, troubleshooting guide |

---

## Dependencias con Sprint 1

Este sprint depende de:
- US-01, US-02, US-03 (Auth completo) → Requerido para US-09 (RBAC)
- US-04, US-05 (CRUD básico) → Requerido para US-06 (Filtros avanzados)
- Configuración de secrets → Requerida para variables de entorno en Docker

---

## Tools & Resources

### Security Validation
- [securityheaders.com](https://securityheaders.com) - Validar headers
- [OWASP ZAP](https://www.zaproxy.org/) - Penetration testing
- Postman - Tests manuales de rate limiting y CORS

### Docker
- Docker Desktop
- Docker Hub account (para push de imágenes)
- [dive](https://github.com/wagoodman/dive) - Analizar capas de Docker image

### Documentation
- Swagger UI - Documentación interactiva
- Markdown editors - Documentación técnica

---

## Sprint Retrospective (Al finalizar)

### Preguntas para Retrospectiva:

1. **Security posture:** ¿Nos sentimos seguros con las mitigaciones implementadas?
2. **Docker experience:** ¿Fue fácil trabajar con Docker? ¿Qué podemos mejorar?
3. **Code quality:** ¿El código está limpio y mantenible?
4. **Blockers:** ¿Hubo impedimentos significativos?
5. **Action items** Acciones concretas para Sprint 3

---

## Notas Adicionales

- **Security Focus:** Este sprint tiene énfasis en seguridad. Todas las decisiones deben considerar implicaciones de seguridad.
- **Testing Manual:** Dedicar tiempo a testing manual de seguridad con herramientas como OWASP ZAP.
- **Documentation:** Docker setup debe estar perfectamente documentado para facilitar onboarding.
- **Performance:** Monitorear performance de queries con filtros complejos.

---

**Preparado por:** Senior DevSecOps Engineer
**Última actualización:** 2025-01-09
