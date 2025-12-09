using ContosoProtect.Api.Models;

namespace ContosoProtect.Api.Services;

public interface IQuoteService
{
    QuoteResponse CalculateQuote(QuoteRequest request);
    (bool IsValid, List<ValidationDetail> Errors) ValidateRequest(QuoteRequest request);
}

public sealed class QuoteService : IQuoteService
{
    private const decimal BasePremium = 12.00m;

    public QuoteResponse CalculateQuote(QuoteRequest request)
    {
        var breakdown = new List<RuleImpact>();
        decimal premium = BasePremium;

        // Base rule
        breakdown.Add(new RuleImpact
        {
            Code = "BASE",
            Description = "Base premium",
            Amount = BasePremium
        });

        // Age-based pricing
        var ageImpact = GetAgeImpact(request.Age);
        if (ageImpact.Amount != 0)
        {
            premium += ageImpact.Amount;
            breakdown.Add(ageImpact);
        }

        // Status-based pricing
        var statusImpact = GetStatusImpact(request.Status);
        if (statusImpact.Amount != 0)
        {
            premium += statusImpact.Amount;
            breakdown.Add(statusImpact);
        }

        // Family option
        if (request.FamilyOption)
        {
            premium += 4m;
            breakdown.Add(new RuleImpact
            {
                Code = "FAMILY_OPTION",
                Description = "Family coverage option",
                Amount = 4m
            });
        }

        // Accident option
        if (request.AccidentOption)
        {
            premium += 3m;
            breakdown.Add(new RuleImpact
            {
                Code = "ACCIDENT_OPTION",
                Description = "Accident coverage option",
                Amount = 3m
            });
        }

        // Seniority discount
        if (request.SeniorityMonths >= 24)
        {
            premium -= 1m;
            breakdown.Add(new RuleImpact
            {
                Code = "SENIORITY_DISCOUNT",
                Description = "Loyalty discount (24+ months)",
                Amount = -1m
            });
        }

        return new QuoteResponse
        {
            Premium = premium,
            Breakdown = breakdown.AsReadOnly()
        };
    }

    public (bool IsValid, List<ValidationDetail> Errors) ValidateRequest(QuoteRequest request)
    {
        var errors = new List<ValidationDetail>();

        if (request.Age < 18 || request.Age > 75)
        {
            errors.Add(new ValidationDetail
            {
                Field = "age",
                Message = "Age must be between 18 and 75."
            });
        }

        if (request.SeniorityMonths < 0)
        {
            errors.Add(new ValidationDetail
            {
                Field = "seniorityMonths",
                Message = "Seniority months cannot be negative."
            });
        }

        if (!Enum.IsDefined(typeof(CustomerStatus), request.Status))
        {
            errors.Add(new ValidationDetail
            {
                Field = "status",
                Message = "Invalid customer status."
            });
        }

        return (errors.Count == 0, errors);
    }

    private RuleImpact GetAgeImpact(int age)
    {
        return age switch
        {
            >= 18 and <= 30 => new RuleImpact
            {
                Code = "AGE_18_30",
                Description = "Age bracket 18-30",
                Amount = 0m
            },
            >= 31 and <= 50 => new RuleImpact
            {
                Code = "AGE_31_50",
                Description = "Age bracket 31-50",
                Amount = 3m
            },
            >= 51 and <= 65 => new RuleImpact
            {
                Code = "AGE_51_65",
                Description = "Age bracket 51-65",
                Amount = 6m
            },
            >= 66 and <= 75 => new RuleImpact
            {
                Code = "AGE_66_75",
                Description = "Age bracket 66-75",
                Amount = 9m
            },
            _ => new RuleImpact
            {
                Code = "AGE_INVALID",
                Description = "Invalid age",
                Amount = 0m
            }
        };
    }

    private RuleImpact GetStatusImpact(CustomerStatus status)
    {
        return status switch
        {
            CustomerStatus.SalariedEmployee => new RuleImpact
            {
                Code = "STATUS_EMPLOYEE",
                Description = "Salaried employee discount",
                Amount = -1m
            },
            CustomerStatus.HouseholdEmployer => new RuleImpact
            {
                Code = "STATUS_EMPLOYER",
                Description = "Household employer premium",
                Amount = 2m
            },
            _ => new RuleImpact
            {
                Code = "STATUS_UNKNOWN",
                Description = "Unknown status",
                Amount = 0m
            }
        };
    }
}
