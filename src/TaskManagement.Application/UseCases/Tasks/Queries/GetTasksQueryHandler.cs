using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Tasks;

namespace TaskManagement.Application.UseCases.Tasks.Queries;

/// <summary>
/// Handler para GetTasksQuery.
/// </summary>
public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, Result<PaginatedList<TaskDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public GetTasksQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _context = context;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedList<TaskDto>>> Handle(
        GetTasksQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Query base: solo tareas del usuario autenticado (no eliminadas)
        var query = _context.Tasks
            .Where(t => t.UserId == _currentUser.UserId && !t.IsDeleted);

        // 2. Aplicar filtro de Status si existe
        if (request.Status.HasValue)
        {
            query = query.Where(t => t.Status == request.Status.Value);
        }

        // 3. Aplicar filtro de Priority si existe
        if (request.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == request.Priority.Value);
        }

        // 4. Ordenar por fecha de creación (más reciente primero)
        query = query.OrderByDescending(t => t.CreatedAt);

        // 5. Proyectar a DTO y paginar (eficiente: solo selecciona campos necesarios)
        var paginatedList = await query
            .ProjectTo<TaskDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.Page, request.PageSize);

        return Result.Success(paginatedList);
    }
}

/// <summary>
/// Extension method para crear PaginatedList desde IQueryable.
/// </summary>
public static class PaginatedListExtensions
{
    public static async Task<PaginatedList<T>> PaginatedListAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize)
    {
        return await PaginatedList<T>.CreateAsync(source, pageNumber, pageSize);
    }
}
