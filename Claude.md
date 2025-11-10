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

### Commit 3: Database configuration and migrations (Pending) 🔲
- Fixed ApplicationDbContext Email Value Object configuration
- Added EF Core Design package
- Created initial migration (20251110184433_InitialCreate)
- Updated Claude.md with comprehensive documentation
- Added Repository Pattern explanation
- **Status:** Ready to commit

---

## Notes

- All explanations are in Spanish (as per project documentation style)
- Extensive inline documentation for educational purposes
- Following Microsoft C# coding standards
- Security-first approach throughout implementation

---

**Last Updated:** 2025-11-10
**Build Status:** ✅ SUCCESS (0 errors, 31 warnings)
**Next Phase:** Entity Framework Migrations & Database Configuration
