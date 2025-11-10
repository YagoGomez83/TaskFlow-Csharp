using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.UseCases.Tasks.Commands;
using TaskManagement.Application.UseCases.Tasks.Queries;
using TaskManagement.Domain.Enums;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.API.Controllers;

/// <summary>
/// Controller para gestión de tareas.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE [Authorize]:
///
/// [Authorize] indica que todos los endpoints de este controller
/// requieren autenticación JWT.
///
/// Cliente debe enviar token en header:
/// Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
///
/// Si no hay token o es inválido:
/// - HTTP 401 Unauthorized
///
/// JWT Middleware extrae claims del token y crea ClaimsPrincipal.
/// ICurrentUserService accede a estos claims para obtener UserId.
///
/// AUTORIZACIÓN POR ROL:
///
/// [Authorize(Roles = "Admin")]              → Solo Admin
/// [Authorize(Roles = "Admin,Moderator")]    → Admin O Moderator
/// [Authorize(Policy = "MinimumAge")]        → Policy personalizado
///
/// Para este proyecto, validamos ownership en Handlers:
/// - Usuario solo puede ver/editar/eliminar sus propias tareas
/// - Admin puede ver/editar/eliminar cualquier tarea
///
/// RESOURCE-BASED AUTHORIZATION:
///
/// Validación de ownership no se puede hacer a nivel de Controller
/// porque necesitamos cargar la tarea primero.
///
/// Flujo:
/// 1. Usuario solicita DELETE /api/tasks/123
/// 2. [Authorize] verifica que usuario esté autenticado
/// 3. Handler carga tarea 123
/// 4. Handler verifica: task.UserId == currentUser.UserId || currentUser.IsAdmin
/// 5. Si no es dueño ni Admin → Result.Failure("No permission")
/// 6. Controller retorna 403 Forbidden
///
/// QUERY PARAMETERS:
///
/// [FromQuery] parsea parámetros de URL:
///
/// GET /api/tasks?page=2&pageSize=20&status=InProgress&priority=High
///
/// ASP.NET Core mapea automáticamente a:
/// GetTasksRequest { Page = 2, PageSize = 20, Status = InProgress, Priority = High }
///
/// [FromQuery] no es necesario con [ApiController], se infiere automáticamente.
///
/// ROUTE PARAMETERS:
///
/// [HttpGet("{id}")]  → GET /api/tasks/123
///
/// {id} en ruta se mapea a parámetro:
/// public async Task<IActionResult> GetById(Guid id)
///
/// VALIDACIÓN DE GUID:
///
/// ASP.NET Core valida automáticamente que "id" sea un Guid válido.
/// Si no es válido, retorna 400 Bad Request.
///
/// GET /api/tasks/invalid-guid → 400 Bad Request
/// GET /api/tasks/123 → 400 Bad Request (no es formato Guid)
/// GET /api/tasks/00000000-0000-0000-0000-000000000000 → 200 OK (Guid válido aunque sea empty)
///
/// PAGINATION:
///
/// GetTasks retorna PaginatedList<TaskDto> con metadata:
///
/// {
///   "items": [...],
///   "pageNumber": 2,
///   "totalPages": 10,
///   "totalCount": 95,
///   "pageSize": 10,
///   "hasPreviousPage": true,
///   "hasNextPage": true
/// }
///
/// Cliente puede usar metadata para navegación:
/// - hasPreviousPage → mostrar botón "Anterior"
/// - hasNextPage → mostrar botón "Siguiente"
/// - totalPages → mostrar números de página
///
/// HTTP STATUS CODES:
///
/// CRUD Operations:
/// - GET /api/tasks → 200 OK (lista)
/// - GET /api/tasks/123 → 200 OK (encontrado) o 404 Not Found
/// - POST /api/tasks → 201 Created (con Location header)
/// - PUT /api/tasks/123 → 200 OK o 404 Not Found
/// - DELETE /api/tasks/123 → 204 No Content o 404 Not Found
///
/// Errors:
/// - 400 Bad Request → Datos inválidos
/// - 401 Unauthorized → No autenticado
/// - 403 Forbidden → Autenticado pero sin permisos
/// - 404 Not Found → Recurso no existe
/// - 500 Internal Server Error → Error no manejado
///
/// LOCATION HEADER:
///
/// Para POST (crear recurso), retornar 201 Created con Location header:
///
/// HTTP/1.1 201 Created
/// Location: https://api.example.com/api/tasks/123
/// {
///   "id": "123",
///   "title": "Nueva tarea"
/// }
///
/// CreatedAtAction genera esto automáticamente:
/// return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
///
/// IDEMPOTENCIA:
///
/// GET, PUT, DELETE son idempotentes (múltiples requests = mismo resultado):
/// - GET /api/tasks/123 → Siempre retorna misma tarea
/// - DELETE /api/tasks/123 → Primera vez elimina, siguientes 404
///
/// POST NO es idempotente (múltiples requests = múltiples recursos):
/// - POST /api/tasks → Crea nueva tarea cada vez
///
/// CORS (Cross-Origin Resource Sharing):
///
/// Si frontend está en dominio diferente (localhost:3000 → localhost:5000),
/// necesitas configurar CORS en Program.cs:
///
/// builder.Services.AddCors(options =>
/// {
///     options.AddPolicy("AllowFrontend", policy =>
///     {
///         policy.WithOrigins("http://localhost:3000")
///               .AllowAnyHeader()
///               .AllowAnyMethod()
///               .AllowCredentials();
///     });
/// });
///
/// app.UseCors("AllowFrontend");
///
/// CONTENT-TYPE:
///
/// Request debe tener Content-Type: application/json:
///
/// POST /api/tasks
/// Content-Type: application/json
/// Authorization: Bearer <token>
/// {
///   "title": "Nueva tarea",
///   "description": "Descripción",
///   "priority": "High"
/// }
///
/// Sin Content-Type correcto, ASP.NET Core no puede parsear body.
///
/// MODEL BINDING:
///
/// ASP.NET Core mapea automáticamente request a DTOs:
///
/// [FromBody] → Request body (JSON)
/// [FromQuery] → Query string (?page=1&pageSize=10)
/// [FromRoute] → Route parameters (/api/tasks/{id})
/// [FromHeader] → HTTP headers
/// [FromForm] → Form data (multipart/form-data)
///
/// Con [ApiController], no necesitas especificar:
/// - [FromBody] se infiere para tipos complejos en POST/PUT
/// - [FromQuery] se infiere para tipos complejos en GET
/// - [FromRoute] se infiere para parámetros en ruta
///
/// FILTERS:
///
/// Puedes agregar filtros a nivel de Controller o Action:
///
/// [ServiceFilter(typeof(ValidationFilterAttribute))]  → Custom validation
/// [TypeFilter(typeof(LogActionFilter))]               → Custom logging
/// [ResponseCache(Duration = 60)]                      → Cache response
///
/// SWAGGER CUSTOMIZATION:
///
/// Para mejor documentación Swagger:
///
/// /// <summary>
/// /// Crea una nueva tarea.
/// /// </summary>
/// /// <param name="request">Datos de la tarea.</param>
/// /// <returns>Tarea creada.</returns>
/// /// <response code="201">Tarea creada exitosamente.</response>
/// /// <response code="400">Datos inválidos.</response>
/// /// <response code="401">Usuario no autenticado.</response>
///
/// Swagger usa estos XML comments para generar documentación.
///
/// TESTING:
///
/// Integration testing con WebApplicationFactory:
///
/// [Fact]
/// public async Task GetTasks_WithValidToken_ReturnsOk()
/// {
///     var client = _factory.CreateAuthenticatedClient(userId, "user@example.com");
///
///     var response = await client.GetAsync("/api/tasks?page=1&pageSize=10");
///
///     response.EnsureSuccessStatusCode();
///     var result = await response.Content.ReadFromJsonAsync<PaginatedList<TaskDto>>();
///     Assert.NotNull(result);
/// }
///
/// Unit testing con mock de MediatR:
///
/// [Fact]
/// public async Task Create_ValidRequest_ReturnsCreatedAtAction()
/// {
///     var mockMediator = new Mock<IMediator>();
///     mockMediator.Setup(m => m.Send(It.IsAny<CreateTaskCommand>(), default))
///         .ReturnsAsync(Result.Success(taskDto));
///
///     var controller = new TasksController(mockMediator.Object);
///     var result = await controller.Create(request);
///
///     var createdResult = Assert.IsType<CreatedAtActionResult>(result);
///     Assert.Equal(nameof(TasksController.GetById), createdResult.ActionName);
/// }
///
/// MEJORES PRÁCTICAS:
///
/// 1. ✅ Usar [Authorize] para endpoints protegidos
/// 2. ✅ Validar ownership en Handlers, no en Controller
/// 3. ✅ Retornar status codes apropiados
/// 4. ✅ Usar CreatedAtAction para POST
/// 5. ✅ Documentar con XML comments para Swagger
/// 6. ✅ Usar DTOs para request/response
/// 7. ❌ NO poner lógica de negocio en Controller
/// 8. ❌ NO retornar entidades directamente (usar DTOs)
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Todos los endpoints requieren autenticación
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtiene lista paginada de tareas del usuario actual.
    /// </summary>
    /// <param name="page">Número de página (1-indexed).</param>
    /// <param name="pageSize">Tamaño de página (1-100).</param>
    /// <param name="status">Filtro opcional por estado.</param>
    /// <param name="priority">Filtro opcional por prioridad.</param>
    /// <returns>Lista paginada de tareas.</returns>
    /// <response code="200">Lista de tareas obtenida exitosamente.</response>
    /// <response code="400">Parámetros de paginación inválidos.</response>
    /// <response code="401">Usuario no autenticado.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] TaskStatus? status = null,
        [FromQuery] TaskPriority? priority = null)
    {
        var query = new GetTasksQuery(page, pageSize, status, priority);

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Obtiene una tarea por ID.
    /// </summary>
    /// <param name="id">ID de la tarea.</param>
    /// <returns>Tarea con el ID especificado.</returns>
    /// <response code="200">Tarea encontrada.</response>
    /// <response code="404">Tarea no encontrada o no pertenece al usuario.</response>
    /// <response code="401">Usuario no autenticado.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetTaskByIdQuery(id);

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Crea una nueva tarea.
    /// </summary>
    /// <param name="request">Datos de la tarea a crear.</param>
    /// <returns>Tarea creada.</returns>
    /// <response code="201">Tarea creada exitosamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="401">Usuario no autenticado.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
    {
        var command = new CreateTaskCommand(
            request.Title,
            request.Description,
            request.DueDate,
            request.Priority);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        // 201 Created con Location header apuntando a GetById
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            result.Value);
    }

    /// <summary>
    /// Actualiza una tarea existente.
    /// </summary>
    /// <param name="id">ID de la tarea a actualizar.</param>
    /// <param name="request">Nuevos datos de la tarea.</param>
    /// <returns>Tarea actualizada.</returns>
    /// <response code="200">Tarea actualizada exitosamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="403">Usuario no tiene permisos para actualizar esta tarea.</response>
    /// <response code="404">Tarea no encontrada.</response>
    /// <response code="401">Usuario no autenticado.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request)
    {
        var command = new UpdateTaskCommand(
            id,
            request.Title,
            request.Description,
            request.DueDate,
            request.Priority,
            request.Status);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            // Distinguir entre Not Found y Forbidden
            if (result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            if (result.Error.Contains("permission", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Elimina una tarea (soft delete).
    /// </summary>
    /// <param name="id">ID de la tarea a eliminar.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Tarea eliminada exitosamente.</response>
    /// <response code="403">Usuario no tiene permisos para eliminar esta tarea.</response>
    /// <response code="404">Tarea no encontrada.</response>
    /// <response code="401">Usuario no autenticado.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteTaskCommand(id);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            if (result.Error.Contains("permission", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        // 204 No Content para DELETE exitoso
        return NoContent();
    }
}
