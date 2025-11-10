using MediatR;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Auth;

namespace TaskManagement.Application.UseCases.Auth.Commands;

/// <summary>
/// Command para registrar nuevo usuario.
/// </summary>
public class RegisterCommand : IRequest<Result<AuthResponse>>
{
    /// <summary>
    /// Email del nuevo usuario.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña del nuevo usuario.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Confirmación de contraseña.
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Constructor para crear comando desde controller.
    /// </summary>
    public RegisterCommand(string email, string password, string confirmPassword)
    {
        Email = email;
        Password = password;
        ConfirmPassword = confirmPassword;
    }
}
