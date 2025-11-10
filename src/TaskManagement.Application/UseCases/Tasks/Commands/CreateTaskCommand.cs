using MediatR;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.UseCases.Tasks.Commands;

/// <summary>
/// Command para crear nueva tarea.
/// </summary>
public class CreateTaskCommand : IRequest<Result<TaskDto>>
{
    /// <summary>
    /// Título de la tarea.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción de la tarea (opcional).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Fecha límite (opcional).
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Prioridad de la tarea.
    /// </summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>
    /// Constructor para crear comando desde controller.
    /// </summary>
    public CreateTaskCommand(string title, string? description, DateTime? dueDate, TaskPriority priority)
    {
        Title = title;
        Description = description;
        DueDate = dueDate;
        Priority = priority;
    }
}
