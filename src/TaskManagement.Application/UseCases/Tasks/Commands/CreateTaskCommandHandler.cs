using AutoMapper;
using MediatR;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.UseCases.Tasks.Commands;

/// <summary>
/// Handler para CreateTaskCommand.
/// </summary>
public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Result<TaskDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public CreateTaskCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _context = context;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<TaskDto>> Handle(
        CreateTaskCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Obtener userId del token JWT (seguro, no puede ser falsificado)
        var userId = _currentUser.UserId;

        // 2. Crear entidad de dominio
        var task = TaskItem.Create(
            request.Title,
            request.Description,
            userId,
            request.DueDate,
            request.Priority
        );

        // 3. Guardar en base de datos
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Mapear a DTO y retornar
        var dto = _mapper.Map<TaskDto>(task);
        return Result<TaskDto>.Success(dto);
    }
}
