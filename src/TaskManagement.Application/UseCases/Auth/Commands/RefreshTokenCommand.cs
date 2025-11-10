using MediatR;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Auth;

namespace TaskManagement.Application.UseCases.Auth.Commands;

/// <summary>
/// Command para refrescar access token usando refresh token.
/// </summary>
public class RefreshTokenCommand : IRequest<Result<AuthResponse>>
{
    /// <summary>
    /// Refresh token para obtener nuevo access token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Constructor para crear comando desde controller.
    /// </summary>
    public RefreshTokenCommand(string refreshToken)
    {
        RefreshToken = refreshToken;
    }
}
