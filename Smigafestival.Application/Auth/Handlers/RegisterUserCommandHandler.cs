using MediatR;
using Smigafestival.Application.Abstractions;
using Smigafestival.Application.Auth.Commands;
using Smigafestival.Application.Common.Models;
using Smigafestival.Domain.Entities;

namespace Smigafestival.Application.Auth.Handlers;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasherService passwordHasherService,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasherService = passwordHasherService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Password and confirm password do not match.");
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedMobileNumber = NormalizeMobileNumber(request.MobileNumber);

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        if (await _userRepository.MobileExistsAsync(normalizedMobileNumber, cancellationToken))
        {
            throw new InvalidOperationException("An account with this mobile number already exists.");
        }

        var user = new AppUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            MobileNumber = request.MobileNumber.Trim(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            NormalizedMobileNumber = normalizedMobileNumber,
            Address = request.Address,
            Website = request.Website,
            PasswordHash = _passwordHasherService.HashPassword(request.Password)
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return _jwtTokenService.CreateToken(user);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static string NormalizeMobileNumber(string mobileNumber)
    {
        return mobileNumber.Trim().ToUpperInvariant();
    }
}
