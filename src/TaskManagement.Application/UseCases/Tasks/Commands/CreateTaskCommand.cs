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
}
