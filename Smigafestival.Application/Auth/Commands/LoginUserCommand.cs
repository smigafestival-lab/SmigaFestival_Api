using System.ComponentModel.DataAnnotations;
using MediatR;
using Smigafestival.Application.Common.Models;

namespace Smigafestival.Application.Auth.Commands;

public sealed record LoginUserCommand(
    [property: Required, StringLength(256)] string LoginIdentifier,
    [property: Required, MinLength(8), StringLength(100)] string Password) : IRequest<AuthResponse>;
