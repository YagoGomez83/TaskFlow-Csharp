using MediatR;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Domain.Enums;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.UseCases.Tasks.Commands;

/// <summary>
/// Command para actualizar tarea existente.
/// </summary>
public class UpdateTaskCommand : IRequest<Result<TaskDto>>
{
    /// <summary>
    /// ID de la tarea a actualizar.
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// Título actualizado.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción actualizada.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Fecha límite actualizada.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Prioridad actualizada.
    /// </summary>
    public TaskPriority Priority { get; set; }

    /// <summary>
    /// Estado actualizado.
    /// </summary>
    public TaskStatus Status { get; set; }

    /// <summary>
    /// Constructor para crear comando desde controller.
    /// </summary>
    public UpdateTaskCommand(Guid taskId, string title, string? description, DateTime? dueDate, TaskPriority priority, TaskStatus status)
    {
        TaskId = taskId;
        Title = title;
        Description = description;
        DueDate = dueDate;
        Priority = priority;
        Status = status;
    }
}
