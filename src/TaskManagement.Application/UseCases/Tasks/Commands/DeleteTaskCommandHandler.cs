using MediatR;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Models;

namespace TaskManagement.Application.UseCases.Tasks.Commands;

/// <summary>
/// Handler para DeleteTaskCommand.
/// </summary>
public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteTaskCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        DeleteTaskCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Buscar tarea
        var task = await _context.Tasks.FindAsync(
            new object[] { request.TaskId }, cancellationToken);

        if (task == null)
        {
            return Result.Failure("Task not found");
        }

        // 2. Validar ownership
        if (task.UserId != _currentUser.UserId && !_currentUser.IsInRole("Admin"))
        {
            return Result.Failure("You don't have permission to delete this task");
        }

        // 3. Soft delete (IsDeleted = true, DeletedAt = now)
        task.Delete();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
