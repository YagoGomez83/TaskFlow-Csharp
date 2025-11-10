# Sprint 3 Plan - DevOps, Frontend & Quality Assurance

**Duración:** 5 días (1 semana)
**Capacity:** 42 Story Points
**Fechas:** Semana 3

---

## Sprint Goal

> "Automatizar completamente el ciclo de vida del software con CI/CD, desarrollar frontend React funcional integrado con la API, y alcanzar production-ready state con suite de tests completa y quality gates."

Al finalizar este sprint, tendremos un sistema completo end-to-end con pipeline automatizado, frontend funcional, y cobertura de tests >80%.

---

## User Stories Incluidas

| ID | User Story | Story Points | Prioridad |
|----|-----------|--------------|-----------|
| US-17 | CI/CD Pipeline GitHub Actions | 13 SP | Media |
| US-18 | Frontend React TypeScript | 13 SP | Media |
| US-19 | Tests Unitarios e Integración | 13 SP | Alta |
| **Refinamiento & Bug Fixes** | | 3 SP | |
| **TOTAL** | | **42 SP** | |

---

## Objetivos Técnicos

### CI/CD Pipeline
- [ ] GitHub Actions workflow completo
- [ ] Build & Test automatizados
- [ ] SAST con SonarQube
- [ ] Dependency scanning (Snyk/Trivy)
- [ ] Docker build & push to registry
- [ ] Deploy automático a staging
- [ ] Notifications (Slack/Email)

### Frontend Development
- [ ] Proyecto React 18 + TypeScript + Vite
- [ ] AuthContext para gestión de estado
- [ ] Componentes de autenticación (Login, Register)
- [ ] Componentes de tareas (TaskList, TaskForm, TaskFilters)
- [ ] Integración con API vía Axios
- [ ] Interceptors para token refresh automático
- [ ] React Router con rutas protegidas
- [ ] Tailwind CSS para styling
- [ ] Responsive design

### Testing Excellence
- [ ] Tests unitarios para Domain (100% coverage)
- [ ] Tests unitarios para Application handlers (>90% coverage)
- [ ] Tests de integración para API endpoints (>85% coverage)
- [ ] Tests frontend con Jest + React Testing Library
- [ ] Coverage total >80%
- [ ] Tests de seguridad incluidos

---

## Daily Tasks Breakdown

### Día 1 - CI/CD Pipeline Parte 1 (8 SP)
**Objetivo:** Pipeline básico con build y tests

- [ ] Crear `.github/workflows/ci-cd.yml`
- [ ] Configurar triggers: push, PR, schedule
- [ ] Configurar steps: checkout, setup .NET, restore, build
- [ ] Configurar step: run tests
- [ ] Configurar caching de NuGet packages
- [ ] Configurar test results reporting
- [ ] Validar pipeline con commit de prueba
- [ ] **Deliverable:** Pipeline build & test funcionando

### Día 2 - CI/CD Pipeline Parte 2 & SAST (5 SP)
**Objetivo:** Security scanning y Docker build

- [ ] Integrar SonarQube para SAST
- [ ] Configurar quality gates
- [ ] Integrar Snyk o Trivy para dependency scanning
- [ ] Configurar Docker build step
- [ ] Configurar Docker push a registry (DockerHub/GitHub Registry)
- [ ] Configurar secrets en GitHub
- [ ] Tests de pipeline completo
- [ ] **Deliverable:** Pipeline completo con security scanning

### Día 3 - Frontend React Parte 1 (8 SP)
**Objetivo:** Setup y autenticación

- [ ] Crear proyecto con Vite + React + TypeScript
- [ ] Configurar estructura de carpetas
- [ ] Configurar Tailwind CSS
- [ ] Implementar AuthContext
- [ ] Crear servicio API con Axios
- [ ] Configurar interceptors para token refresh
- [ ] Implementar componentes: Login, Register
- [ ] Implementar ProtectedRoute
- [ ] Configurar React Router
- [ ] **Deliverable:** Auth flow funcionando en frontend

### Día 4 - Frontend React Parte 2 & Testing Backend (10 SP)
**Objetivo:** Componentes de tareas y tests backend

**Frontend:**
- [ ] Implementar TaskList con paginación
- [ ] Implementar TaskForm (create/edit)
- [ ] Implementar TaskFilters
- [ ] Implementar TaskItem
- [ ] Integrar con API endpoints
- [ ] Styling con Tailwind
- [ ] Responsive design
- [ ] **Deliverable Frontend:** CRUD de tareas funcionando

**Backend Testing:**
- [ ] Completar tests unitarios de Domain
- [ ] Completar tests unitarios de Application
- [ ] Completar tests de integración de API
- [ ] Verificar coverage >80%
- [ ] **Deliverable Backend:** Tests completos

### Día 5 - Frontend Testing & Polish (11 SP)
**Objetivo:** Tests frontend, refinamiento y preparación para producción

- [ ] Configurar Jest + React Testing Library
- [ ] Escribir tests para componentes de Auth
- [ ] Escribir tests para componentes de Tasks
- [ ] Escribir tests para servicios API
- [ ] Tests de integración frontend
- [ ] Bug fixes identificados durante testing
- [ ] Refinamiento de UX/UI
- [ ] Documentación de componentes
- [ ] Preparar documentación de deployment
- [ ] Sprint Review preparation
- [ ] **Deliverable:** Sistema completo production-ready

---

## Definition of Done

Una User Story se considera DONE cuando:

- [ ] Código implementado según criterios de aceptación
- [ ] Tests unitarios y de integración pasando
- [ ] Coverage >80%
- [ ] CI/CD pipeline ejecutándose exitosamente
- [ ] Code review aprobado
- [ ] Documentación actualizada (técnica y usuario)
- [ ] Frontend integrado con backend
- [ ] Responsive en móvil/tablet/desktop
- [ ] Sin vulnerabilidades críticas o altas
- [ ] Deploy a staging exitoso
- [ ] Aprobado por Product Owner

---

## CI/CD Pipeline Validation Checklist

- [ ] **Build:**
  - [ ] Compila sin errores
  - [ ] Sin warnings críticos
  - [ ] Build time < 5 minutos

- [ ] **Tests:**
  - [ ] Todos los tests pasando
  - [ ] Coverage >80%
  - [ ] Test results reportados

- [ ] **SAST:**
  - [ ] SonarQube quality gate PASSED
  - [ ] Sin vulnerabilidades críticas o altas
  - [ ] Code smells < 50

- [ ] **Dependency Scanning:**
  - [ ] Sin vulnerabilidades críticas en dependencias
  - [ ] Reporte de vulnerabilidades generado

- [ ] **Docker:**
  - [ ] Imagen construida exitosamente
  - [ ] Imagen escaneada (Trivy)
  - [ ] Imagen pushed a registry
  - [ ] Tag versionado correctamente

- [ ] **Deploy:**
  - [ ] Deploy a staging automático
  - [ ] Health check exitoso post-deploy
  - [ ] Rollback automático si falla

---

## Frontend Validation Checklist

- [ ] **Functionality:**
  - [ ] Login funcional
  - [ ] Register funcional
  - [ ] CRUD de tareas funcional
  - [ ] Filtros funcionando
  - [ ] Paginación funcionando
  - [ ] Logout funcional

- [ ] **Integration:**
  - [ ] API calls exitosos
  - [ ] Token refresh automático
  - [ ] Error handling correcto
  - [ ] Loading states implementados

- [ ] **UX/UI:**
  - [ ] Diseño consistente
  - [ ] Responsive (móvil, tablet, desktop)
  - [ ] Accesibilidad básica (WCAG 2.1 Level A)
  - [ ] Mensajes de error claros

- [ ] **Performance:**
  - [ ] Carga inicial < 3 segundos
  - [ ] No memory leaks
  - [ ] Optimización de re-renders

- [ ] **Testing:**
  - [ ] Tests unitarios para componentes
  - [ ] Tests de integración
  - [ ] Coverage >70% frontend

---

## Testing Validation Checklist

### Backend Testing

- [ ] **Domain Layer (Target: 100%):**
  - [ ] Entity validation tests
  - [ ] Business logic tests
  - [ ] Value object tests

- [ ] **Application Layer (Target: >90%):**
  - [ ] Command handler tests
  - [ ] Query handler tests
  - [ ] Validator tests
  - [ ] Behavior tests

- [ ] **API Layer (Target: >85%):**
  - [ ] Controller tests
  - [ ] Middleware tests
  - [ ] Integration tests end-to-end
  - [ ] Authentication tests
  - [ ] Authorization tests

### Frontend Testing

- [ ] **Components (Target: >70%):**
  - [ ] Unit tests con Jest
  - [ ] Integration tests con RTL
  - [ ] User interaction tests

- [ ] **Services (Target: >80%):**
  - [ ] API service tests
  - [ ] Auth service tests
  - [ ] Mock de Axios

---

## Riesgos Identificados

| Riesgo | Impacto | Probabilidad | Mitigación |
|--------|---------|--------------|------------|
| Pipeline complejo toma más tiempo | Alto | Media | Comenzar temprano en el sprint, usar templates |
| Integración frontend-backend con issues | Alto | Media | Testing continuo, mock API para desarrollo paralelo |
| Coverage <80% difícil de alcanzar | Medio | Media | Priorizar tests de lógica crítica, TDD cuando sea posible |
| Deploy a staging falla | Medio | Baja | Probar deploy localmente primero, documentación detallada |

---

## Dependencias con Sprints Anteriores

Este sprint depende de:
- **Sprint 1:** API completa y funcional → Requerido para integración frontend
- **Sprint 2:** Docker Compose funcionando → Requerido para deploy pipeline
- **Sprint 2:** Swagger documentation → Facilita desarrollo frontend

---

## Tools & Resources

### CI/CD
- GitHub Actions
- SonarQube Cloud
- Snyk / Trivy
- Docker Hub o GitHub Container Registry
- Staging environment (puede ser cloud o VPS)

### Frontend Development
- Vite
- React 18
- TypeScript
- Tailwind CSS
- Axios
- React Router
- Zustand (opcional, si Context API no es suficiente)

### Testing
- xUnit (backend)
- Moq (backend mocking)
- Jest (frontend)
- React Testing Library
- Coverlet (coverage backend)
- Istanbul (coverage frontend)

---

## Acceptance Criteria para Sprint Goal

El Sprint 3 se considera EXITOSO cuando:

1. **CI/CD Pipeline:**
   - [ ] Pipeline ejecuta automáticamente en cada push/PR
   - [ ] Build, test, SAST, Docker build funcionan
   - [ ] Deploy a staging exitoso

2. **Frontend:**
   - [ ] Usuario puede registrarse, login, y gestionar tareas
   - [ ] Integración con API funcional
   - [ ] Responsive en 3 breakpoints mínimo
   - [ ] Sin bugs críticos

3. **Testing:**
   - [ ] Coverage backend >80%
   - [ ] Coverage frontend >70%
   - [ ] Todos los tests pasando
   - [ ] Tests integrados en CI/CD

4. **Production Ready:**
   - [ ] Sin vulnerabilidades críticas
   - [ ] Documentación completa
   - [ ] Sistema funcionando end-to-end
   - [ ] Listo para deploy a producción

---

## Sprint Review Preparation

### Demo Script

1. **Mostrar CI/CD Pipeline** (5 min)
   - Trigger pipeline con commit
   - Mostrar ejecución de tests
   - Mostrar SAST results
   - Mostrar deploy a staging

2. **Demo Frontend** (10 min)
   - Register de nuevo usuario
   - Login
   - Crear tareas
   - Filtrar tareas
   - Editar tarea
   - Eliminar tarea
   - Logout
   - Mostrar responsive en móvil

3. **Mostrar Tests & Coverage** (5 min)
   - Coverage reports
   - Tests ejecutándose
   - Quality gates

4. **Q&A** (10 min)

---

## Sprint Retrospective (Al finalizar)

### Preguntas para Retrospectiva:

1. **Overall project:** ¿Logramos el objetivo del proyecto completo?
2. **Technical debt:** ¿Acumulamos deuda técnica? ¿Cómo la abordaremos?
3. **CI/CD experience:** ¿El pipeline agrega valor? ¿Qué mejorar?
4. **Frontend quality:** ¿El frontend cumple estándares de calidad?
5. **Testing strategy:** ¿La estrategia de testing fue efectiva?
6. **What's next?** ¿Qué features agregaríamos en v1.1?
7. **Lessons learned:** ¿Qué aprendimos como equipo?

---

## Post-Sprint Activities

Una vez completado Sprint 3:

1. **Deploy to Production:**
   - [ ] Validar staging exhaustivamente
   - [ ] Configurar dominio y certificados SSL
   - [ ] Deploy a producción
   - [ ] Smoke tests en producción
   - [ ] Monitoreo activo

2. **Documentation:**
   - [ ] Actualizar README con instrucciones producción
   - [ ] Crear user guide
   - [ ] Documentar troubleshooting común

3. **Handover:**
   - [ ] Knowledge transfer si aplica
   - [ ] Credenciales y accesos documentados
   - [ ] Plan de mantenimiento

---

## Success Metrics

Al finalizar este sprint, mediremos éxito con:

- ✅ **Functionality:** Sistema completo end-to-end funcionando
- ✅ **Quality:** Coverage >80%, sin bugs críticos
- ✅ **Security:** Sin vulnerabilidades críticas o altas
- ✅ **Automation:** CI/CD ejecutándose automáticamente
- ✅ **Performance:** API <200ms, Frontend carga <3s
- ✅ **UX:** Frontend responsive e intuitivo
- ✅ **Documentation:** Docs completas para desarrollo y deploy

---

**Preparado por:** Senior Full-Stack Developer & DevSecOps Engineer
**Última actualización:** 2025-01-09
