using MediatR;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Domain.Enums;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.UseCases.Tasks.Queries;

/// <summary>
/// Query para obtener lista paginada de tareas del usuario autenticado.
/// </summary>
public class GetTasksQuery : IRequest<Result<PaginatedList<TaskDto>>>
{
    /// <summary>
    /// Número de página (1-indexed).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Cantidad de items por página.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Filtro opcional por estado.
    /// </summary>
    public TaskStatus? Status { get; set; }

    /// <summary>
    /// Filtro opcional por prioridad.
    /// </summary>
    public TaskPriority? Priority { get; set; }
}
