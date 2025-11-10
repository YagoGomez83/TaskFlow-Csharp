using MediatR;
using TaskManagement.Application.Common.Models;

namespace TaskManagement.Application.UseCases.Tasks.Commands;

/// <summary>
/// Command para eliminar tarea (soft delete).
/// </summary>
public class DeleteTaskCommand : IRequest<Result>
{
    /// <summary>
    /// ID de la tarea a eliminar.
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// Constructor para crear comando desde controller.
    /// </summary>
    public DeleteTaskCommand(Guid taskId)
    {
        TaskId = taskId;
    }
}
