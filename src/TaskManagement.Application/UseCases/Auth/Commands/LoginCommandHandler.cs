using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.ValueObjects;

namespace TaskManagement.Application.UseCases.Auth.Commands;

/// <summary>
/// Handler para LoginCommand.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Buscar usuario por email
        var email = Email.Create(request.Email);
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);

        // 2. Si no existe, retornar error genérico (no revelar si email existe)
        if (user == null)
        {
            return Result.Failure<AuthResponse>("Invalid credentials");
        }

        // 3. Verificar contraseña
        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            // Registrar intento fallido (lockout después de 5 intentos)
            user.RecordFailedLogin();
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Failure<AuthResponse>("Invalid credentials");
        }

        // 4. Verificar si cuenta está bloqueada
        if (!user.CanLogin())
        {
            return Result.Failure<AuthResponse>(
                $"Account is locked due to multiple failed login attempts. Try again after {user.LockedOutUntil:yyyy-MM-dd HH:mm:ss} UTC");
        }

        // 5. Login exitoso - resetear intentos fallidos
        user.ResetLoginAttempts();

        // 6. Generar access token (JWT)
        var accessToken = _tokenService.GenerateAccessToken(user);

        // 7. Generar refresh token
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshToken = RefreshToken.Create(
            user.Id,
            refreshTokenValue,
            DateTime.UtcNow.AddDays(7)  // Expira en 7 días
        );

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        // 8. Retornar tokens
        return Result.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresIn = 900,  // 15 minutos en segundos
            TokenType = "Bearer"
        });
    }
}
