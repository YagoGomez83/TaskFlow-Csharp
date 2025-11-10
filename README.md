# TaskManagement API - Clean Architecture

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-336791?logo=postgresql)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7.0-DC382D?logo=redis)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Enabled-2496ED?logo=docker)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Descripción

Sistema completo de gestión de tareas personales construido con **Clean Architecture**, siguiendo los principios **SOLID** y las mejores prácticas de **DevSecOps**. Este proyecto implementa una API REST robusta con autenticación JWT, protecciones contra OWASP Top 10, y un frontend moderno en React.

## Características Principales

### Funcionalidades

- **Gestión de Tareas**: CRUD completo con soporte para título, descripción, fecha límite, prioridad y estado
- **Autenticación Segura**: JWT con refresh token rotation y account lockout
- **Búsqueda Avanzada**: Filtros por estado, prioridad, fechas y búsqueda full-text
- **Paginación**: Navegación eficiente con metadata completa
- **Control de Acceso**: RBAC con roles Admin y User
- **Logging Estructurado**: Serilog con múltiples sinks para auditoría

### Seguridad (DevSecOps)

- Password hashing con BCrypt (work factor 12)
- Prevención de SQL Injection (Entity Framework Core)
- Protección contra XSS con validación de entrada
- Rate limiting (100 req/min general, 10 req/min auth)
- Security headers (HSTS, CSP, X-Frame-Options, X-Content-Type-Options)
- CORS restrictivo con whitelist de orígenes
- Secrets management (user-secrets para dev, env vars para prod)
- Input validation con FluentValidation

### Arquitectura y Patrones

- **Clean Architecture**: Separación en 4 capas (Domain, Application, Infrastructure, API)
- **SOLID Principles**: Código mantenible y testeable
- **CQRS**: Separación Commands/Queries usando MediatR
- **Repository Pattern**: Abstracción del acceso a datos
- **Unit of Work**: Transacciones consistentes
- **Result Pattern**: Manejo de errores sin excepciones

## Stack Tecnológico

### Backend
- **.NET 8**: Framework principal
- **ASP.NET Core Web API**: REST API
- **Entity Framework Core 8**: ORM para PostgreSQL
- **MediatR**: CQRS y mediator pattern
- **FluentValidation**: Validación de entrada
- **AutoMapper**: Mapeo Entity ↔ DTO
- **Serilog**: Logging estructurado
- **xUnit + Moq**: Testing

### Frontend
- **React 18**: UI library
- **TypeScript**: Type safety
- **Vite**: Build tool
- **Axios**: HTTP client
- **React Router**: Routing
- **Tailwind CSS**: Styling
- **Jest + RTL**: Testing

### Infraestructura
- **PostgreSQL 15**: Base de datos relacional
- **Redis 7**: Cache distribuido
- **Docker + Docker Compose**: Containerización
- **Nginx**: Reverse proxy
- **GitHub Actions**: CI/CD pipeline

## Inicio Rápido

### Prerrequisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Node.js 18+](https://nodejs.org/) (para frontend)
- [Git](https://git-scm.com/)

### Instalación con Docker (Recomendado)

```bash
# Clonar el repositorio
git clone https://github.com/tu-usuario/taskmanagement.git
cd taskmanagement

# Copiar archivo de configuración
cp .env.example .env

# Levantar todos los servicios
docker-compose up -d

# Verificar que los servicios están corriendo
docker-compose ps

# Acceder a la aplicación
# API: http://localhost:5000
# Frontend: http://localhost:3000
# Swagger: http://localhost:5000/swagger
```

### Instalación Manual

```bash
# 1. Configurar Base de Datos
# Asegúrate de tener PostgreSQL y Redis corriendo localmente

# 2. Configurar Secrets (Backend)
cd src/TaskManagement.API
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:Secret" "tu-super-secret-key-de-al-menos-32-caracteres"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=TaskManagementDb;Username=postgres;Password=tu-password"
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379"

# 3. Aplicar Migraciones
dotnet ef database update

# 4. Ejecutar API
dotnet run

# 5. Ejecutar Frontend (en otra terminal)
cd src/TaskManagement.WebApp
npm install
npm run dev
```

## Estructura del Proyecto

```
TaskManagement/
├── src/
│   ├── TaskManagement.Domain/          # Entidades, Value Objects, Enums
│   ├── TaskManagement.Application/     # Use Cases, DTOs, Validadores
│   ├── TaskManagement.Infrastructure/  # EF Core, Redis, Seguridad
│   ├── TaskManagement.API/            # Controllers, Middleware
│   └── TaskManagement.WebApp/         # React Frontend
├── tests/
│   ├── TaskManagement.UnitTests/      # Tests unitarios
│   └── TaskManagement.IntegrationTests/ # Tests de integración
├── docker/                             # Dockerfiles
├── docs/                               # Documentación detallada
└── .github/workflows/                  # CI/CD pipelines
```

Ver [Estructura Detallada](docs/architecture/clean-architecture.md) para más información.

## Documentación

### Arquitectura
- [Clean Architecture](docs/architecture/clean-architecture.md) - Explicación detallada de las capas
- [Security Design](docs/architecture/security-design.md) - Diseño de seguridad y mitigaciones OWASP

### Metodología Scrum
- [Product Backlog](docs/scrum/product-backlog.md) - 20 User Stories completas
- [Sprint 1 Plan](docs/scrum/sprint-1-plan.md) - Core Authentication & CRUD
- [Sprint 2 Plan](docs/scrum/sprint-2-plan.md) - Security Hardening & Docker
- [Sprint 3 Plan](docs/scrum/sprint-3-plan.md) - DevOps & Frontend

### Setup y Configuración
- [Desarrollo Local](docs/setup/local-development.md) - Guía paso a paso
- [Docker Setup](docs/setup/docker-setup.md) - Configuración de containers
- [API Documentation](docs/api/endpoints.md) - Referencia de endpoints

## Testing

```bash
# Ejecutar tests unitarios
dotnet test tests/TaskManagement.UnitTests/

# Ejecutar tests de integración
dotnet test tests/TaskManagement.IntegrationTests/

# Ejecutar tests frontend
cd src/TaskManagement.WebApp
npm test

# Coverage report
dotnet test /p:CollectCoverage=true /p:CoverageReportsDirectory=./coverage
```

Objetivo de cobertura: **80% mínimo**

## CI/CD Pipeline

El proyecto incluye workflows de GitHub Actions para:

- **Build & Test**: Compilación y ejecución de tests en cada push
- **SAST**: Análisis estático con SonarQube
- **Dependency Scanning**: Detección de vulnerabilidades en dependencias
- **Docker Build**: Construcción de imágenes optimizadas
- **Deploy**: Despliegue automático a staging en merge a main

Ver [CI/CD Configuration](.github/workflows/ci-cd.yml) para detalles.

## API Endpoints

### Authentication
```http
POST   /api/auth/register     # Registro de usuario
POST   /api/auth/login        # Login con JWT
POST   /api/auth/refresh      # Renovar token
POST   /api/auth/logout       # Cerrar sesión
```

### Tasks
```http
GET    /api/tasks             # Listar tareas (paginado)
GET    /api/tasks/{id}        # Obtener tarea por ID
POST   /api/tasks             # Crear tarea
PUT    /api/tasks/{id}        # Actualizar tarea
DELETE /api/tasks/{id}        # Eliminar tarea (soft delete)
```

Ver [Swagger UI](http://localhost:5000/swagger) para documentación interactiva en desarrollo.

## Variables de Entorno

Ver [.env.example](.env.example) para configuración completa.

Principales variables:

```env
# Database
DATABASE_CONNECTION=Host=postgres;Database=TaskManagementDb;Username=postgres;Password=postgres

# Redis
REDIS_CONNECTION=redis:6379

# JWT
JWT_SECRET=your-super-secret-key-minimum-32-characters
JWT_ISSUER=TaskManagementAPI
JWT_AUDIENCE=TaskManagementClient
JWT_ACCESS_TOKEN_EXPIRATION_MINUTES=15
JWT_REFRESH_TOKEN_EXPIRATION_DAYS=7

# CORS
ALLOWED_ORIGINS=http://localhost:3000,https://yourdomain.com

# Rate Limiting
RATE_LIMIT_GENERAL=100
RATE_LIMIT_AUTH=10
```

## Contribución

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

Ver [CONTRIBUTING.md](CONTRIBUTING.md) para guías detalladas.

## Seguridad

Si descubres una vulnerabilidad de seguridad, por favor revisa nuestra [Política de Seguridad](SECURITY.md) para reportarla de forma responsable.

## Roadmap

- [ ] **v1.0** - MVP con funcionalidades core (Sprint 1-3)
- [ ] **v1.1** - Notificaciones por email
- [ ] **v1.2** - Compartir tareas entre usuarios
- [ ] **v1.3** - Etiquetas y categorías
- [ ] **v2.0** - Aplicación móvil (React Native)
- [ ] **v2.1** - Integración con calendarios (Google, Outlook)
- [ ] **v2.2** - Tareas recurrentes
- [ ] **v3.0** - Colaboración en tiempo real (SignalR)

## Licencia

Este proyecto está bajo la licencia MIT. Ver [LICENSE](LICENSE) para más detalles.

## Contacto

- **Autor**: Senior Full-Stack Developer & DevSecOps Engineer
- **Proyecto**: [GitHub Repository](https://github.com/tu-usuario/taskmanagement)
- **Issues**: [Report Bug](https://github.com/tu-usuario/taskmanagement/issues)
- **Documentación**: [Wiki](https://github.com/tu-usuario/taskmanagement/wiki)

---

Desarrollado con Clean Architecture, SOLID Principles y siguiendo las mejores prácticas de DevSecOps.
