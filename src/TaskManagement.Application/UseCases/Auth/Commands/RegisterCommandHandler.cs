using MediatR;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.ValueObjects;

namespace TaskManagement.Application.UseCases.Auth.Commands;

/// <summary>
/// Handler para RegisterCommand.
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public RegisterCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponse>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Hashear contraseña con BCrypt
        var passwordHash = _passwordHasher.Hash(request.Password);

        // 2. Crear usuario con rol User por defecto
        var email = Email.Create(request.Email);
        var user = User.Create(email, passwordHash, UserRole.User);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        // 3. Auto-login: Generar tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = RefreshToken.Create(
            user.Id,
            refreshTokenValue,
            DateTime.UtcNow.AddDays(7)
        );

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Retornar tokens (usuario ya logueado)
        return Result.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresIn = 900,
            TokenType = "Bearer"
        });
    }
}
