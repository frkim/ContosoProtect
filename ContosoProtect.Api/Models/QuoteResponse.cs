namespace ContosoProtect.Api.Models;

public sealed class QuoteResponse
{
    public decimal Premium { get; set; }
    public IReadOnlyList<RuleImpact> Breakdown { get; set; } = Array.Empty<RuleImpact>();
}
