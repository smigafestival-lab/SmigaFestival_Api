using MediatR;
using Smigafestival.Application.Abstractions;
using Smigafestival.Application.Auth.Commands;
using Smigafestival.Application.Common.Models;

namespace Smigafestival.Application.Auth.Handlers;

public sealed class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasherService passwordHasherService,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasherService = passwordHasherService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByEmailOrMobileAsync(request.LoginIdentifier.Trim(), cancellationToken);

        if (user is null || !_passwordHasherService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid login credentials.");
        }

        return _jwtTokenService.CreateToken(user);
    }
}
