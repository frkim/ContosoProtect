namespace ContosoProtect.Api.Models;

public sealed class QuoteRequest
{
    public int Age { get; set; }
    public CustomerStatus Status { get; set; }
    public bool FamilyOption { get; set; }
    public bool AccidentOption { get; set; }
    public int SeniorityMonths { get; set; }
}
