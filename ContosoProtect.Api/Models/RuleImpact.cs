namespace ContosoProtect.Api.Models;

public sealed class RuleImpact
{
    public string Code { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
}
