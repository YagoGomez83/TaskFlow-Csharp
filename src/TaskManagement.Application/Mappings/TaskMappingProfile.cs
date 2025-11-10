using AutoMapper;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Mappings;

/// <summary>
/// AutoMapper profile para mapeos de Task.
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE AUTOMAPPER:
///
/// AutoMapper es una biblioteca para mapear objetos automáticamente.
/// Evita escribir código repetitivo de mapeo manual.
///
/// SIN AUTOMAPPER (manual):
/// var dto = new TaskDto
/// {
///     Id = task.Id,
///     Title = task.Title,
///     Description = task.Description,
///     DueDate = task.DueDate,
///     Priority = task.Priority,
///     Status = task.Status,
///     UserId = task.UserId,
///     CreatedAt = task.CreatedAt,
///     UpdatedAt = task.UpdatedAt
/// };
///
/// CON AUTOMAPPER:
/// var dto = _mapper.Map<TaskDto>(task);
///
/// MAPEO POR CONVENCIÓN:
///
/// AutoMapper mapea propiedades automáticamente si:
/// - Tienen mismo nombre (case-insensitive)
/// - Tienen tipos compatibles o convertibles
///
/// TaskItem.Id (Guid) → TaskDto.Id (Guid) ✅ Auto
/// TaskItem.Title (string) → TaskDto.Title (string) ✅ Auto
/// TaskItem.Priority (enum) → TaskDto.Priority (enum) ✅ Auto
///
/// MAPEOS PERSONALIZADOS:
///
/// Para mapeos no convencionales:
///
/// CreateMap<Source, Dest>()
///     .ForMember(dest => dest.FullName,
///                opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
///
/// PROYECCIÓN EN QUERIES:
///
/// ProjectTo<T>() permite proyectar directamente en LINQ:
///
/// var tasks = await _context.Tasks
///     .ProjectTo<TaskDto>(_mapper.ConfigurationProvider)
///     .ToListAsync();
///
/// SQL generado selecciona solo columnas necesarias:
/// SELECT Id, Title, Description, ... FROM Tasks
///
/// En lugar de cargar entidad completa y mapear en memoria.
///
/// REVERSE MAPPING:
///
/// ReverseMap() crea mapeo bidireccional:
///
/// CreateMap<TaskItem, TaskDto>().ReverseMap();
///
/// Ahora puede mapear TaskItem → TaskDto Y TaskDto → TaskItem.
///
/// Para este proyecto, solo necesitamos TaskItem → TaskDto (one-way).
///
/// FLATTENING:
///
/// AutoMapper puede aplanar objetos anidados:
///
/// Source:
/// class Order { Customer Customer; }
/// class Customer { string Name; }
///
/// Dest:
/// class OrderDto { string CustomerName; }
///
/// CreateMap<Order, OrderDto>();
/// // AutoMapper mapea Customer.Name → CustomerName automáticamente
///
/// COLLECTIONS:
///
/// AutoMapper mapea colecciones automáticamente:
///
/// List<TaskItem> → List<TaskDto>
/// TaskItem[] → TaskDto[]
/// IEnumerable<TaskItem> → IEnumerable<TaskDto>
///
/// _mapper.Map<List<TaskDto>>(tasks);
///
/// REGISTRO EN DI:
///
/// builder.Services.AddAutoMapper(typeof(TaskMappingProfile).Assembly);
///
/// Esto escanea el assembly y registra todos los profiles.
///
/// PERFORMANCE:
///
/// AutoMapper compila mapeos en primera ejecución (un poco lento).
/// Ejecuciones subsecuentes son muy rápidas (comparable a mapeo manual).
///
/// Para performance crítica, considerar mapeo manual.
/// Para casos generales, AutoMapper es excelente.
///
/// TESTING:
///
/// [Fact]
/// public void TaskMappingProfile_ShouldHaveValidConfiguration()
/// {
///     var configuration = new MapperConfiguration(cfg =>
///         cfg.AddProfile<TaskMappingProfile>());
///
///     configuration.AssertConfigurationIsValid();
/// }
///
/// [Fact]
/// public void Map_TaskItemToTaskDto_MapsAllProperties()
/// {
///     var task = TaskItem.Create("Title", "Description", userId);
///     var dto = _mapper.Map<TaskDto>(task);
///
///     Assert.Equal(task.Id, dto.Id);
///     Assert.Equal(task.Title, dto.Title);
///     Assert.Equal(task.Description, dto.Description);
/// }
/// </remarks>
public class TaskMappingProfile : Profile
{
    public TaskMappingProfile()
    {
        // TaskItem → TaskDto (one-way)
        // Mapeo por convención: propiedades con mismo nombre se mapean automáticamente
        CreateMap<TaskItem, TaskDto>();

        // No necesitamos ReverseMap() porque nunca mapeamos TaskDto → TaskItem
        // DTOs solo son para lectura (output)
        // Para crear/actualizar, usamos Commands que extraen propiedades manualmente
    }
}
