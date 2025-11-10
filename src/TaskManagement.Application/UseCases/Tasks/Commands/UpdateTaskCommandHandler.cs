using AutoMapper;
using MediatR;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Tasks;

namespace TaskManagement.Application.UseCases.Tasks.Commands;

/// <summary>
/// Handler para UpdateTaskCommand.
/// </summary>
public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, Result<TaskDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public UpdateTaskCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _context = context;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<TaskDto>> Handle(
        UpdateTaskCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Buscar tarea
        var task = await _context.Tasks.FindAsync(
            new object[] { request.TaskId }, cancellationToken);

        if (task == null)
        {
            return Result<TaskDto>.Failure("Task not found");
        }

        // 2. Validar ownership (solo dueño o Admin puede actualizar)
        if (task.UserId != _currentUser.UserId && !_currentUser.IsInRole("Admin"))
        {
            return Result<TaskDto>.Failure("You don't have permission to update this task");
        }

        // 3. Actualizar campos
        task.UpdateTitle(request.Title);
        task.UpdateDescription(request.Description);
        task.UpdateDueDate(request.DueDate);
        task.UpdatePriority(request.Priority);
        task.UpdateStatus(request.Status);

        // 4. Guardar cambios
        await _context.SaveChangesAsync(cancellationToken);

        // 5. Retornar tarea actualizada
        var dto = _mapper.Map<TaskDto>(task);
        return Result<TaskDto>.Success(dto);
    }
}
