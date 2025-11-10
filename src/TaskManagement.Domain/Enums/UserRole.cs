namespace TaskManagement.Domain.Enums;

/// <summary>
/// Roles de usuario en el sistema (RBAC - Role-Based Access Control).
/// </summary>
/// <remarks>
/// EXPLICACIÓN DE RBAC (Role-Based Access Control):
///
/// RBAC es un método de control de acceso donde los permisos se asignan a roles,
/// y los usuarios son asignados a esos roles. En lugar de dar permisos individualmente
/// a cada usuario, asignamos roles que ya tienen permisos predefinidos.
///
/// VENTAJAS DE RBAC:
/// ✅ Simplifica la gestión de permisos
/// ✅ Reduce errores de configuración
/// ✅ Facilita auditorías de seguridad
/// ✅ Escalable para sistemas grandes
/// ✅ Cumplimiento de políticas de seguridad
///
/// ROLES EN ESTE SISTEMA:
///
/// 1. User (Rol básico):
///    - Puede crear sus propias tareas
///    - Puede ver solo sus tareas
///    - Puede editar solo sus tareas
///    - Puede eliminar solo sus tareas
///    - NO puede ver tareas de otros usuarios
///    - NO puede gestionar usuarios
///
/// 2. Admin (Rol administrativo):
///    - Todos los permisos de User
///    - Puede ver todas las tareas de todos los usuarios
///    - Puede editar/eliminar tareas de cualquier usuario
///    - Puede gestionar usuarios (crear, editar, eliminar)
///    - Puede ver estadísticas globales
///    - Puede configurar el sistema
///
/// FUTURAS EXPANSIONES:
/// En sistemas más complejos, podrías tener roles como:
/// - Manager: puede ver tareas de su equipo
/// - Auditor: solo puede ver, no puede modificar
/// - SuperAdmin: permisos completos incluyendo configuración crítica
///
/// IMPLEMENTACIÓN:
/// Los roles se almacenan como strings en la base de datos mediante conversión
/// de EF Core, pero se validan fuertemente tipados en código mediante este enum.
/// </remarks>
public enum UserRole
{
    /// <summary>
    /// Usuario estándar con permisos básicos.
    /// </summary>
    /// <remarks>
    /// Permisos:
    /// - CRUD completo sobre sus propias tareas
    /// - Ver solo sus propias tareas
    /// - No puede acceder a tareas de otros usuarios
    /// - No puede realizar operaciones administrativas
    ///
    /// Este es el rol por defecto al registrarse.
    /// </remarks>
    User = 0,

    /// <summary>
    /// Administrador con permisos elevados.
    /// </summary>
    /// <remarks>
    /// Permisos:
    /// - Todos los permisos de User
    /// - CRUD sobre tareas de cualquier usuario
    /// - Ver listado de todos los usuarios
    /// - Gestionar usuarios (crear, editar, eliminar, cambiar roles)
    /// - Acceder a endpoints administrativos (/api/admin/*)
    /// - Ver estadísticas y reportes globales
    ///
    /// Asignación:
    /// - Solo otro Admin puede promover a un User a Admin
    /// - Se debe limitar el número de Admins por seguridad
    /// - Registrar todas las acciones de Admin en logs de auditoría
    /// </remarks>
    Admin = 1
}
