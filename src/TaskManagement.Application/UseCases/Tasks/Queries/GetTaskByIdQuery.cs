using MediatR;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Tasks;

namespace TaskManagement.Application.UseCases.Tasks.Queries;

/// <summary>
/// Query para obtener tarea por ID.
/// </summary>
public class GetTaskByIdQuery : IRequest<Result<TaskDto>>
{
    /// <summary>
    /// ID de la tarea a obtener.
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// Constructor para crear query desde controller.
    /// </summary>
    public GetTaskByIdQuery(Guid taskId)
    {
        TaskId = taskId;
    }
}
