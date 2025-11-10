using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.UseCases.Auth.Commands;

/// <summary>
/// Handler para RefreshTokenCommand.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        ITokenService tokenService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Buscar refresh token en BD
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        if (refreshToken == null)
        {
            return Result.Failure<AuthResponse>("Invalid refresh token");
        }

        // 2. Validar estado del token
        if (!refreshToken.IsValid())
        {
            return Result.Failure<AuthResponse>("Refresh token is not valid or has expired");
        }

        // 3. DETECTAR REUSO (Token Rotation Security)
        if (refreshToken.IsUsed)
        {
            _logger.LogWarning(
                "Refresh token {TokenId} was reused - possible token theft detected for user {UserId}",
                refreshToken.Id, refreshToken.UserId);

            // Revocar toda la familia de tokens
            await RevokeTokenFamily(refreshToken, cancellationToken);

            return Result.Failure<AuthResponse>(
                "Refresh token has been revoked due to suspicious activity. Please log in again.");
        }

        // 4. Obtener usuario
        var user = await _context.Users.FindAsync(new object[] { refreshToken.UserId }, cancellationToken);
        if (user == null)
        {
            return Result.Failure<AuthResponse>("User not found");
        }

        // 5. Marcar token actual como usado (Token Rotation)
        refreshToken.MarkAsUsed();

        // 6. Generar nuevo access token
        var accessToken = _tokenService.GenerateAccessToken(user);

        // 7. Generar nuevo refresh token (Rotation)
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        var newRefreshToken = RefreshToken.Create(
            user.Id,
            newRefreshTokenValue,
            DateTime.UtcNow.AddDays(7),
            refreshToken.Id  // ParentTokenId para token family tracking
        );

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        // 8. Retornar nuevos tokens
        return Result.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresIn = 900,
            TokenType = "Bearer"
        });
    }

    /// <summary>
    /// Revoca toda la familia de tokens (detectado token theft).
    /// </summary>
    private async Task RevokeTokenFamily(RefreshToken token, CancellationToken cancellationToken)
    {
        // Revocar el token actual
        token.Revoke();

        // Revocar todos los tokens descendientes (family chain)
        var childTokens = await _context.RefreshTokens
            .Where(t => t.ParentTokenId == token.Id)
            .ToListAsync(cancellationToken);

        foreach (var childToken in childTokens)
        {
            await RevokeTokenFamily(childToken, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
