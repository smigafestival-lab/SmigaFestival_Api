namespace Smigafestival.Domain.Entities;

public sealed class SubscriptionPlan
{
    public int PlanId { get; set; }

    public decimal PlanAmount { get; set; }

    public int PlanDuration { get; set; }

    public string PlanCategory { get; set; } = string.Empty;
}
