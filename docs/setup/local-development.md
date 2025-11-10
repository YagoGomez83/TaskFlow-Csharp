# Guía de Desarrollo Local - TaskManagement API

Esta guía cubre cómo configurar el proyecto para desarrollo local sin Docker.

---

## Prerrequisitos

### Software Requerido

- **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** - Framework principal
- **[PostgreSQL 15+](https://www.postgresql.org/download/)** - Base de datos
- **[Redis 7+](https://redis.io/download)** - Cache
- **[Node.js 18+](https://nodejs.org/)** - Para frontend
- **[Git](https://git-scm.com/)** - Control de versiones
- **IDE:** Visual Studio 2022, VS Code, o JetBrains Rider

### Verificar Instalación

```bash
# .NET
dotnet --version
# Debe mostrar: 8.0.x

# PostgreSQL
psql --version
# Debe mostrar: psql (PostgreSQL) 15.x

# Redis
redis-server --version
# Debe mostrar: Redis server v=7.x.x

# Node.js
node --version
# Debe mostrar: v18.x.x o superior

# Git
git --version
```

---

## 1. Clonar el Repositorio

```bash
git clone https://github.com/tu-usuario/taskmanagement.git
cd taskmanagement
```

---

## 2. Configurar Base de Datos (PostgreSQL)

### Opción A: Instalación Local

```bash
# Iniciar servicio PostgreSQL
# Windows: Buscar "Services" → PostgreSQL → Start
# macOS: brew services start postgresql
# Linux: sudo systemctl start postgresql

# Conectar a PostgreSQL
psql -U postgres

# Crear base de datos
CREATE DATABASE TaskManagementDb;

# Crear usuario (opcional)
CREATE USER taskuser WITH PASSWORD 'YourStrongPassword123!';
GRANT ALL PRIVILEGES ON DATABASE TaskManagementDb TO taskuser;

# Salir
\q
```

### Opción B: Docker Solo para Base de Datos

```bash
docker run --name taskmanagement-postgres \
  -e POSTGRES_DB=TaskManagementDb \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  -d postgres:15-alpine
```

---

## 3. Configurar Redis

### Opción A: Instalación Local

```bash
# Iniciar servicio Redis
# Windows: redis-server
# macOS: brew services start redis
# Linux: sudo systemctl start redis

# Verificar que Redis está corriendo
redis-cli ping
# Debe responder: PONG
```

### Opción B: Docker Solo para Redis

```bash
docker run --name taskmanagement-redis \
  -p 6379:6379 \
  -d redis:7-alpine
```

---

## 4. Configurar Secrets (Backend)

.NET 8 usa **User Secrets** para desarrollo, evitando hardcodear credenciales.

```bash
# Navegar al proyecto API
cd src/TaskManagement.API

# Inicializar user secrets
dotnet user-secrets init

# Configurar JWT Secret (IMPORTANTE: usar clave segura de 32+ caracteres)
dotnet user-secrets set "JwtSettings:Secret" "SuperSecretKeyForJWT-MustBe32CharsMin!"

# Configurar conexión a PostgreSQL
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=TaskManagementDb;Username=postgres;Password=postgres"

# Configurar conexión a Redis
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379"

# Verificar secrets configurados
dotnet user-secrets list
```

**Resultado esperado:**
```
ConnectionStrings:DefaultConnection = Host=localhost;Database=TaskManagementDb...
ConnectionStrings:Redis = localhost:6379
JwtSettings:Secret = SuperSecretKeyForJWT-MustBe32CharsMin!
```

---

## 5. Restaurar Dependencias & Build

```bash
# Desde la raíz del proyecto
dotnet restore

# Build de la solution completa
dotnet build

# Verificar que no hay errores de compilación
```

---

## 6. Aplicar Migraciones de Base de Datos

```bash
# Navegar al proyecto API
cd src/TaskManagement.API

# Instalar herramienta EF Core (si no está instalada)
dotnet tool install --global dotnet-ef

# Verificar instalación
dotnet ef --version

# Crear migración inicial
dotnet ef migrations add InitialCreate --project ../TaskManagement.Infrastructure --startup-project .

# Aplicar migración a la base de datos
dotnet ef database update --project ../TaskManagement.Infrastructure --startup-project .
```

**Verificar en PostgreSQL:**
```bash
psql -U postgres -d TaskManagementDb -c "\dt"
```

Deberías ver tablas: `Users`, `Tasks`, `RefreshTokens`, `__EFMigrationsHistory`

---

## 7. Ejecutar API (Backend)

```bash
# Desde src/TaskManagement.API
dotnet run

# O con hot reload (recomendado para desarrollo)
dotnet watch run
```

**Salida esperada:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

**Endpoints disponibles:**
- API: http://localhost:5000
- API HTTPS: https://localhost:5001
- Swagger UI: https://localhost:5001/swagger (solo en Development)
- Health Check: https://localhost:5001/health

---

## 8. Configurar Frontend (React)

```bash
# Navegar al proyecto frontend
cd src/TaskManagement.WebApp

# Instalar dependencias
npm install

# Crear archivo .env.local
cat > .env.local << EOF
VITE_API_URL=https://localhost:5001
EOF

# Ejecutar en modo desarrollo
npm run dev
```

**Salida esperada:**
```
  VITE v5.x.x  ready in xxx ms

  ➜  Local:   http://localhost:3000/
  ➜  Network: use --host to expose
```

**Frontend disponible en:** http://localhost:3000

---

## 9. Ejecutar Tests

### Tests Backend

```bash
# Tests unitarios
dotnet test tests/TaskManagement.UnitTests/

# Tests de integración
dotnet test tests/TaskManagement.IntegrationTests/

# Todos los tests con coverage
dotnet test --collect:"XPlat Code Coverage"

# Ver coverage (requiere ReportGenerator)
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
# Abrir: coverage-report/index.html
```

### Tests Frontend

```bash
cd src/TaskManagement.WebApp

# Tests con Jest
npm test

# Tests con coverage
npm run test:coverage

# Tests en modo watch
npm run test:watch
```

---

## 10. Debugging

### Visual Studio 2022

1. Abrir `TaskManagement.sln`
2. Configurar múltiples startup projects:
   - Right-click solution → Properties → Multiple startup projects
   - Seleccionar: `TaskManagement.API` (Start), `TaskManagement.WebApp` (Start)
3. Presionar `F5` para debug

### VS Code

1. Instalar extensiones:
   - C# Dev Kit
   - REST Client (para testing API)
2. Abrir carpeta del proyecto
3. Usar configuración de launch (`.vscode/launch.json` pre-configurado)
4. Presionar `F5`

### JetBrains Rider

1. Abrir `TaskManagement.sln`
2. Configurar Run Configuration para API
3. Run → Debug

---

## 11. Troubleshooting Común

### Error: "Unable to connect to PostgreSQL"

**Solución:**
```bash
# Verificar que PostgreSQL está corriendo
# Windows:
services.msc → PostgreSQL → Status: Running

# macOS/Linux:
pg_isready -h localhost -p 5432

# Verificar connection string en user-secrets
dotnet user-secrets list
```

### Error: "Redis connection failed"

**Solución:**
```bash
# Verificar que Redis está corriendo
redis-cli ping

# Si no responde, iniciar Redis:
# Windows: redis-server.exe
# macOS: brew services start redis
# Linux: sudo systemctl start redis
```

### Error: "JWT token validation failed"

**Solución:**
```bash
# Verificar que JWT Secret está configurado y es suficientemente largo (32+ chars)
dotnet user-secrets list | grep JwtSettings

# Si no existe o es corto, configurar:
dotnet user-secrets set "JwtSettings:Secret" "NewSuperSecretKeyMinimum32Characters!"
```

### Error: "Migration already applied"

**Solución:**
```bash
# Revertir última migración
dotnet ef database update 0 --project ../TaskManagement.Infrastructure

# Eliminar migración
dotnet ef migrations remove --project ../TaskManagement.Infrastructure

# Crear nueva migración
dotnet ef migrations add NewMigrationName --project ../TaskManagement.Infrastructure
dotnet ef database update --project ../TaskManagement.Infrastructure
```

### Error: "Port 5000 already in use"

**Solución:**
```bash
# Windows:
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# macOS/Linux:
lsof -i :5000
kill -9 <PID>

# O cambiar puerto en appsettings.Development.json:
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5002"
      }
    }
  }
}
```

### Error: "Cannot find module in frontend"

**Solución:**
```bash
cd src/TaskManagement.WebApp

# Limpiar node_modules y reinstalar
rm -rf node_modules package-lock.json
npm install

# Si persiste, limpiar cache
npm cache clean --force
npm install
```

---

## 12. Herramientas Útiles para Desarrollo

### REST Client (Testing API)

**Opción A: Postman**
1. Importar collection desde `docs/api/TaskManagement.postman_collection.json`
2. Configurar environment: `base_url = https://localhost:5001`

**Opción B: REST Client (VS Code)**
```bash
# Instalar extensión: REST Client
# Crear archivo: api-tests.http

### Register User
POST https://localhost:5001/api/auth/register
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!@#"
}

### Login
POST https://localhost:5001/api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!@#"
}
```

### Database GUI

- **pgAdmin 4** - GUI para PostgreSQL
- **DBeaver** - Universal database tool
- **RedisInsight** - GUI para Redis

### API Monitoring

```bash
# Ver logs en tiempo real (si usas Serilog con Seq)
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest

# Configurar en appsettings.Development.json:
"Serilog": {
  "WriteTo": [
    {
      "Name": "Seq",
      "Args": {
        "serverUrl": "http://localhost:5341"
      }
    }
  ]
}
```

**Acceder a Seq:** http://localhost:5341

---

## 13. Seed Data (Opcional)

Para poblar la base de datos con datos de prueba:

```bash
cd scripts/

# Ejecutar script de seed
psql -U postgres -d TaskManagementDb -f seed-data.sql
```

O implementar en código:

```csharp
// Infrastructure/Persistence/DbInitializer.cs
public static async Task SeedAsync(ApplicationDbContext context)
{
    if (await context.Users.AnyAsync()) return;

    // Crear usuarios de prueba
    var admin = User.Create(
        Email.Create("admin@taskmanagement.com"),
        passwordHash, // Pre-hasheado
        UserRole.Admin
    );

    var user = User.Create(
        Email.Create("user@taskmanagement.com"),
        passwordHash,
        UserRole.User
    );

    context.Users.AddRange(admin, user);
    await context.SaveChangesAsync();
}
```

---

## 14. Ambiente de Desarrollo Recomendado

### Extensiones de VS Code

```bash
# Instalar extensiones recomendadas
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.csdevkit
code --install-extension dbaeumer.vscode-eslint
code --install-extension esbenp.prettier-vscode
code --install-extension humao.rest-client
code --install-extension ms-azuretools.vscode-docker
```

### Configuración de .editorconfig

Ya incluido en el proyecto para mantener consistencia de código.

---

## 15. Workflow Típico de Desarrollo

```bash
# 1. Asegurarse de estar en branch actualizado
git checkout develop
git pull origin develop

# 2. Crear feature branch
git checkout -b feature/US-XX-descripcion

# 3. Iniciar servicios de desarrollo
# Terminal 1: API
cd src/TaskManagement.API
dotnet watch run

# Terminal 2: Frontend
cd src/TaskManagement.WebApp
npm run dev

# Terminal 3: Tests en watch mode
cd tests/TaskManagement.UnitTests
dotnet watch test

# 4. Desarrollar feature

# 5. Ejecutar tests
dotnet test

# 6. Commit con mensaje descriptivo
git add .
git commit -m "feat(US-XX): implement feature description"

# 7. Push y crear PR
git push origin feature/US-XX-descripcion
```

---

## 16. Recursos Adicionales

- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [React Documentation](https://react.dev/)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- [Tailwind CSS](https://tailwindcss.com/docs)

---

**¿Problemas?** Abre un issue en GitHub o consulta [Troubleshooting completo](../troubleshooting.md)

**Última actualización:** 2025-01-09
