using AutoMapper;
using MediatR;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Tasks;

namespace TaskManagement.Application.UseCases.Tasks.Queries;

/// <summary>
/// Handler para GetTaskByIdQuery.
/// </summary>
public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, Result<TaskDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public GetTaskByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _context = context;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<TaskDto>> Handle(
        GetTaskByIdQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Buscar tarea
        var task = await _context.Tasks.FindAsync(
            new object[] { request.TaskId }, cancellationToken);

        // 2. Validar existencia
        if (task == null || task.IsDeleted)
        {
            return Result<TaskDto>.Failure("Task not found");
        }

        // 3. Validar ownership (solo owner o Admin puede ver)
        if (task.UserId != _currentUser.UserId && !_currentUser.IsInRole("Admin"))
        {
            return Result<TaskDto>.Failure("You don't have permission to view this task");
        }

        // 4. Mapear a DTO y retornar
        var dto = _mapper.Map<TaskDto>(task);
        return Result<TaskDto>.Success(dto);
    }
}
