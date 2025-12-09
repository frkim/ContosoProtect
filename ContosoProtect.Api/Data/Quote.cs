using ContosoProtect.Api.Models;

namespace ContosoProtect.Api.Data;

public class Quote
{
    public int Id { get; set; }
    public int Age { get; set; }
    public CustomerStatus Status { get; set; }
    public bool FamilyOption { get; set; }
    public bool AccidentOption { get; set; }
    public int SeniorityMonths { get; set; }
    public decimal Premium { get; set; }
    public DateTime CreatedAt { get; set; }
}
