namespace Smigafestival.Domain.Entities;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string MobileNumber { get; set; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string? Website { get; init; } = string.Empty;

    public string BusinessName { get; init; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NormalizedMobileNumber { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? SubscribedUserId { get; set; }

    public bool IsPaymentDone { get; set; }

    public string Role { get; set; } = "User";

    public bool isPlanExpire { get; set; }

    public int PlanID { get; set; }

    public DateTime? PlanStartDate { get; set; }

    public DateTime? PlanEndDate { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
