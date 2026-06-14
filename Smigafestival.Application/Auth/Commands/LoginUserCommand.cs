using System.ComponentModel.DataAnnotations;
using MediatR;
using Smigafestival.Application.Common.Models;

namespace Smigafestival.Application.Auth.Commands;

public sealed class LoginUserCommand : IRequest<AuthResponse>
{
    [Required]
    [StringLength(256)]
    public string LoginIdentifier { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [StringLength(100)]
    public string Password { get; init; } = string.Empty;
}
