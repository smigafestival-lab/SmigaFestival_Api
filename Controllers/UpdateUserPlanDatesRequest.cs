using System.ComponentModel.DataAnnotations;

namespace Smigafestival.Controllers;

public sealed class UpdateUserPlanDatesRequest
{
    [Required]
    public DateTime PlanStartDate { get; set; }

    [Required]
    public DateTime PlanEndDate { get; set; }
}
