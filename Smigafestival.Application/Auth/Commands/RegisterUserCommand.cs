using System.ComponentModel.DataAnnotations;
using MediatR;
using Smigafestival.Application.Common.Models;

namespace Smigafestival.Application.Auth.Commands;

public sealed class RegisterUserCommand : IRequest<AuthResponse>
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string MobileNumber { get; init; } = string.Empty;


    [Required]
    [StringLength(500)]

    public string Address { get; init; } = string.Empty;


    [StringLength(200)]

    public string? Website { get; init; } = string.Empty;

    [StringLength(50)]
    public string BusinessName { get; init; } = string.Empty;

    
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [StringLength(100)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; init; } = string.Empty;
}
