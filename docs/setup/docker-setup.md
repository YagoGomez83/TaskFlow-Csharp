# Guía de Docker Setup - TaskManagement API

Esta guía cubre cómo ejecutar el proyecto completo usando Docker y Docker Compose.

---

## Prerrequisitos

### Software Requerido

- **[Docker Desktop](https://www.docker.com/products/docker-desktop)** (Windows/Mac) o Docker Engine (Linux)
- **[Docker Compose](https://docs.docker.com/compose/install/)** (incluido en Docker Desktop)
- **[Git](https://git-scm.com/)** - Para clonar el repositorio

### Verificar Instalación

```bash
# Docker
docker --version
# Debe mostrar: Docker version 24.x.x o superior

# Docker Compose
docker-compose --version
# Debe mostrar: Docker Compose version v2.x.x o superior

# Verificar que Docker daemon está corriendo
docker ps
# Debe listar containers activos (vacío si no hay ninguno)
```

---

## Inicio Rápido (5 minutos)

```bash
# 1. Clonar el repositorio
git clone https://github.com/tu-usuario/taskmanagement.git
cd taskmanagement

# 2. Copiar archivo de configuración
cp .env.example .env

# 3. Levantar todos los servicios
docker-compose up -d

# 4. Verificar que los servicios están corriendo
docker-compose ps

# 5. Ver logs en tiempo real
docker-compose logs -f api

# Acceder a la aplicación:
# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
# Frontend: http://localhost:3000
# PostgreSQL: localhost:5432
# Redis: localhost:6379
```

---

## Arquitectura de Servicios

```
┌──────────────────────────────────────────────────────┐
│                    Nginx (Port 80)                   │
│               Reverse Proxy & Load Balancer          │
└───────────────┬──────────────────────────────────────┘
                │
    ┌───────────┼───────────┐
    │           │           │
    ▼           ▼           ▼
┌─────────┐ ┌─────────┐ ┌──────────┐
│   API   │ │  WebApp │ │   API    │
│ :5000   │ │  :3000  │ │  :5001   │ (scale)
└────┬────┘ └─────────┘ └────┬─────┘
     │                       │
     │  ┌────────────────────┘
     │  │
     ▼  ▼
┌──────────────┐      ┌──────────┐
│  PostgreSQL  │      │  Redis   │
│    :5432     │      │  :6379   │
└──────────────┘      └──────────┘
```

---

## Servicios Incluidos

### 1. API (TaskManagement.API)
- **Puerto:** 5000 (HTTP), 5001 (HTTPS)
- **Imagen:** Custom build desde `docker/api.Dockerfile`
- **Health Check:** `/health`
- **Dependencias:** PostgreSQL, Redis

### 2. WebApp (TaskManagement.WebApp)
- **Puerto:** 3000
- **Imagen:** Custom build desde `docker/webapp.Dockerfile`
- **Health Check:** Root endpoint `/`
- **Dependencias:** API

### 3. PostgreSQL
- **Puerto:** 5432
- **Imagen:** `postgres:15-alpine`
- **Volumen:** `postgres-data` (persistente)
- **Configuración:** Ver `.env`

### 4. Redis
- **Puerto:** 6379
- **Imagen:** `redis:7-alpine`
- **Volumen:** `redis-data` (persistente)
- **Configuración:** Memoria máxima 256MB

### 5. Nginx (opcional, para producción)
- **Puerto:** 80 (HTTP), 443 (HTTPS)
- **Configuración:** `docker/nginx/nginx.conf`
- **SSL/TLS:** Certificados en `docker/nginx/certs/`

---

## Configuración Detallada

### Archivo .env

Copiar `.env.example` a `.env` y ajustar valores:

```bash
# Database Configuration
DATABASE_HOST=postgres
DATABASE_PORT=5432
DATABASE_NAME=TaskManagementDb
DATABASE_USER=postgres
DATABASE_PASSWORD=YourStrongPassword123!

# Redis Configuration
REDIS_HOST=redis
REDIS_PORT=6379
REDIS_PASSWORD=

# JWT Configuration
JWT_SECRET=SuperSecretKeyForJWT-MustBe32CharsMinimum!
JWT_ISSUER=TaskManagementAPI
JWT_AUDIENCE=TaskManagementClient
JWT_ACCESS_TOKEN_EXPIRATION_MINUTES=15
JWT_REFRESH_TOKEN_EXPIRATION_DAYS=7

# CORS Configuration
ALLOWED_ORIGINS=http://localhost:3000,http://localhost

# Rate Limiting
RATE_LIMIT_GENERAL=100
RATE_LIMIT_AUTH=10

# Environment
ASPNETCORE_ENVIRONMENT=Development

# Frontend
VITE_API_URL=http://localhost:5000
```

**IMPORTANTE:** Cambiar credenciales antes de deploy a producción.

---

## Comandos Esenciales

### Gestión de Servicios

```bash
# Levantar todos los servicios (modo detached)
docker-compose up -d

# Levantar solo un servicio específico
docker-compose up -d api

# Ver status de servicios
docker-compose ps

# Ver logs de todos los servicios
docker-compose logs

# Ver logs de un servicio específico
docker-compose logs -f api

# Seguir logs en tiempo real
docker-compose logs -f --tail=100

# Detener todos los servicios
docker-compose stop

# Detener y eliminar containers
docker-compose down

# Detener, eliminar containers y volúmenes (CUIDADO: borra datos)
docker-compose down -v

# Reiniciar un servicio
docker-compose restart api
```

### Build y Rebuild

```bash
# Build de imágenes (sin cache)
docker-compose build --no-cache

# Build y levantar servicios
docker-compose up --build -d

# Build solo un servicio
docker-compose build api

# Pull de imágenes base actualizadas
docker-compose pull
```

### Scaling

```bash
# Escalar API a 3 instancias
docker-compose up -d --scale api=3

# Verificar instancias
docker-compose ps
```

---

## Health Checks

Todos los servicios tienen health checks configurados:

```bash
# Verificar health de API
curl http://localhost:5000/health

# Respuesta esperada:
# {"status":"Healthy","checks":[{"name":"database","status":"Healthy"},{"name":"redis","status":"Healthy"}]}

# Verificar health de PostgreSQL
docker-compose exec postgres pg_isready

# Verificar health de Redis
docker-compose exec redis redis-cli ping
# Respuesta: PONG
```

---

## Volúmenes y Persistencia

### Volúmenes Definidos

```yaml
volumes:
  postgres-data:    # Datos de PostgreSQL
  redis-data:       # Datos de Redis (opcional, cache puede ser efímero)
```

### Gestión de Volúmenes

```bash
# Listar volúmenes
docker volume ls

# Inspeccionar volumen
docker volume inspect taskmanagement_postgres-data

# Backup de PostgreSQL
docker-compose exec postgres pg_dump -U postgres TaskManagementDb > backup.sql

# Restore de PostgreSQL
docker-compose exec -T postgres psql -U postgres TaskManagementDb < backup.sql

# Eliminar volúmenes (CUIDADO: pérdida de datos)
docker-compose down -v
```

---

## Redes

Docker Compose crea redes aisladas automáticamente:

```yaml
networks:
  backend-network:    # API, PostgreSQL, Redis
  frontend-network:   # WebApp, Nginx
```

### Comandos de Red

```bash
# Listar redes
docker network ls

# Inspeccionar red
docker network inspect taskmanagement_backend-network

# Ver IPs de containers
docker-compose exec api hostname -i
```

---

## Debugging en Docker

### Acceder a Shell de Container

```bash
# Bash en container de API
docker-compose exec api /bin/bash

# Bash en container de PostgreSQL
docker-compose exec postgres /bin/bash

# Ejecutar comando en container
docker-compose exec api dotnet --version
```

### Ver Logs Detallados

```bash
# Logs con timestamps
docker-compose logs -f --timestamps api

# Últimas 50 líneas de logs
docker-compose logs --tail=50 api

# Logs desde timestamp específico
docker-compose logs --since 2025-01-09T10:00:00 api
```

### Inspeccionar Container

```bash
# Ver detalles del container
docker inspect taskmanagement-api-1

# Ver variables de entorno
docker-compose exec api printenv

# Ver procesos corriendo
docker-compose exec api ps aux
```

---

## Migraciones de Base de Datos

### Ejecutar Migraciones Automáticamente

Las migraciones se ejecutan automáticamente al iniciar la API (ver `Program.cs`).

### Ejecutar Migraciones Manualmente

```bash
# Opción 1: Desde container
docker-compose exec api dotnet ef database update

# Opción 2: Crear nueva migración desde host
dotnet ef migrations add MigrationName \
  --project src/TaskManagement.Infrastructure \
  --startup-project src/TaskManagement.API

# Revertir migración
docker-compose exec api dotnet ef database update PreviousMigration
```

---

## Optimización de Imágenes

### Dockerfile Multi-Stage (API)

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/TaskManagement.API/TaskManagement.API.csproj", "TaskManagement.API/"]
COPY ["src/TaskManagement.Application/TaskManagement.Application.csproj", "TaskManagement.Application/"]
COPY ["src/TaskManagement.Domain/TaskManagement.Domain.csproj", "TaskManagement.Domain/"]
COPY ["src/TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj", "TaskManagement.Infrastructure/"]

RUN dotnet restore "TaskManagement.API/TaskManagement.API.csproj"

COPY src/ .
RUN dotnet build "TaskManagement.API/TaskManagement.API.csproj" -c Release -o /app/build
RUN dotnet publish "TaskManagement.API/TaskManagement.API.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Crear usuario no root
RUN addgroup -g 1000 appuser && \
    adduser -u 1000 -G appuser -s /bin/sh -D appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskManagement.API.dll"]
```

### Verificar Tamaño de Imagen

```bash
# Listar imágenes con tamaños
docker images | grep taskmanagement

# Analizar capas de imagen
docker history taskmanagement-api:latest
```

**Objetivo:** Imagen API < 200MB

---

## Seguridad en Docker

### Buenas Prácticas Implementadas

- ✅ **Usuario no root:** API corre como `appuser`
- ✅ **Secrets en variables de entorno:** No hardcoded
- ✅ **Imagen base Alpine:** Menor superficie de ataque
- ✅ **Multi-stage build:** Solo runtime artifacts en imagen final
- ✅ **Health checks:** Monitoreo de estado
- ✅ **Networks aisladas:** Separación de concerns

### Escaneo de Vulnerabilidades

```bash
# Instalar Trivy
# macOS:
brew install trivy

# Linux:
wget -qO - https://aquasecurity.github.io/trivy-repo/deb/public.key | sudo apt-key add -
echo "deb https://aquasecurity.github.io/trivy-repo/deb $(lsb_release -sc) main" | sudo tee -a /etc/apt/sources.list.d/trivy.list
sudo apt-get update && sudo apt-get install trivy

# Escanear imagen
trivy image taskmanagement-api:latest

# Escanear solo vulnerabilidades críticas y altas
trivy image --severity CRITICAL,HIGH taskmanagement-api:latest
```

---

## Docker Compose para Diferentes Ambientes

### Development (default)

```bash
docker-compose up -d
```

Usa: `docker-compose.yml` + `docker-compose.override.yml`

### Production

```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

**docker-compose.prod.yml:**
```yaml
version: '3.8'

services:
  api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    restart: always

  nginx:
    ports:
      - "443:443"
    volumes:
      - ./docker/nginx/certs:/etc/nginx/certs:ro

  postgres:
    environment:
      - POSTGRES_PASSWORD=${DATABASE_PASSWORD}
    restart: always
```

### Testing

```bash
docker-compose -f docker-compose.test.yml up --abort-on-container-exit
```

---

## Troubleshooting

### Container No Inicia

```bash
# Ver logs de error
docker-compose logs api

# Ver eventos de Docker
docker events

# Inspeccionar exit code
docker-compose ps
```

### Puerto Ya en Uso

```bash
# Windows:
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# macOS/Linux:
lsof -i :5000
kill -9 <PID>

# O cambiar puerto en docker-compose.yml:
ports:
  - "5002:8080"
```

### Permission Denied (Linux)

```bash
# Agregar usuario a grupo docker
sudo usermod -aG docker $USER

# Logout y login nuevamente
```

### Container Saludable pero No Responde

```bash
# Verificar network connectivity
docker-compose exec api ping postgres

# Verificar DNS resolution
docker-compose exec api nslookup postgres

# Verificar variables de entorno
docker-compose exec api printenv | grep DATABASE
```

### Base de Datos No Inicializa

```bash
# Ver logs de PostgreSQL
docker-compose logs postgres

# Entrar a container y verificar
docker-compose exec postgres psql -U postgres -l

# Recrear volumen (PERDERÁS DATOS)
docker-compose down -v
docker-compose up -d
```

---

## Monitoreo y Observabilidad

### Logs Centralizados (Seq)

```bash
# Agregar Seq a docker-compose.yml
seq:
  image: datalust/seq:latest
  ports:
    - "5341:80"
  environment:
    - ACCEPT_EULA=Y
  volumes:
    - seq-data:/data

# Configurar API para enviar logs a Seq
environment:
  - Serilog__WriteTo__1__Name=Seq
  - Serilog__WriteTo__1__Args__serverUrl=http://seq:5341
```

**Acceder a Seq:** http://localhost:5341

### Prometheus + Grafana (Opcional)

```bash
# Agregar servicios de monitoreo
prometheus:
  image: prom/prometheus:latest
  ports:
    - "9090:9090"
  volumes:
    - ./docker/prometheus.yml:/etc/prometheus/prometheus.yml

grafana:
  image: grafana/grafana:latest
  ports:
    - "3001:3000"
  environment:
    - GF_SECURITY_ADMIN_PASSWORD=admin
```

---

## Comandos Útiles de Mantenimiento

```bash
# Limpiar containers detenidos
docker container prune -f

# Limpiar imágenes no usadas
docker image prune -a -f

# Limpiar volúmenes no usados
docker volume prune -f

# Limpiar todo (containers, images, volumes, networks)
docker system prune -a --volumes -f

# Ver uso de disco de Docker
docker system df

# Ver recursos usados por containers
docker stats
```

---

## CI/CD con Docker

### GitHub Actions Example

```yaml
name: Docker Build & Push

on:
  push:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Login to DockerHub
        uses: docker/login-action@v2
        with:
          username: ${{ secrets.DOCKERHUB_USERNAME }}
          password: ${{ secrets.DOCKERHUB_TOKEN }}

      - name: Build and push
        uses: docker/build-push-action@v4
        with:
          context: .
          file: ./docker/api.Dockerfile
          push: true
          tags: |
            yourusername/taskmanagement-api:latest
            yourusername/taskmanagement-api:${{ github.sha }}
```

---

## Recursos Adicionales

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Reference](https://docs.docker.com/compose/compose-file/)
- [Best practices for writing Dockerfiles](https://docs.docker.com/develop/develop-images/dockerfile_best-practices/)
- [Docker Security](https://docs.docker.com/engine/security/)

---

**¿Problemas?** Consulta [Troubleshooting completo](../troubleshooting.md) o abre un issue en GitHub.

**Última actualización:** 2025-01-09
