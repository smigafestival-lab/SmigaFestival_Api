using System.ComponentModel.DataAnnotations;

namespace Smigafestival.Controllers;

public sealed class AssignUserPlanRequest
{
    [Required]
    public int PlanId { get; set; }
}
