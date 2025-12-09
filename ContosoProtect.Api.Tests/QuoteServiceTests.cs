using ContosoProtect.Api.Models;
using ContosoProtect.Api.Services;
using Xunit;

namespace ContosoProtect.Api.Tests;

public class QuoteServiceTests
{
    private readonly IQuoteService _quoteService;

    public QuoteServiceTests()
    {
        _quoteService = new QuoteService();
    }

    [Fact]
    public void CalculateQuote_BasePremiumOnly_ReturnsCorrectAmount()
    {
        // Arrange
        var request = new QuoteRequest
        {
            Age = 25,
            Status = CustomerStatus.SalariedEmployee,
            FamilyOption = false,
            AccidentOption = false,
            SeniorityMonths = 0
        };

        // Act
        var result = _quoteService.CalculateQuote(request);

        // Assert
        Assert.Equal(11m, result.Premium); // 12 (base) - 1 (employee discount)
        Assert.Contains(result.Breakdown, b => b.Code == "BASE");
        Assert.Contains(result.Breakdown, b => b.Code == "STATUS_EMPLOYEE");
    }

    [Fact]
    public void CalculateQuote_WithAllOptions_ReturnsCorrectAmount()
    {
        // Arrange
        var request = new QuoteRequest
        {
            Age = 42,
            Status = CustomerStatus.SalariedEmployee,
            FamilyOption = true,
            AccidentOption = true,
            SeniorityMonths = 36
        };

        // Act
        var result = _quoteService.CalculateQuote(request);

        // Assert
        // 12 (base) + 3 (age 31-50) - 1 (employee) + 4 (family) + 3 (accident) - 1 (seniority) = 20
        Assert.Equal(20m, result.Premium);
        Assert.Contains(result.Breakdown, b => b.Code == "BASE");
        Assert.Contains(result.Breakdown, b => b.Code == "AGE_31_50");
        Assert.Contains(result.Breakdown, b => b.Code == "STATUS_EMPLOYEE");
        Assert.Contains(result.Breakdown, b => b.Code == "FAMILY_OPTION");
        Assert.Contains(result.Breakdown, b => b.Code == "ACCIDENT_OPTION");
        Assert.Contains(result.Breakdown, b => b.Code == "SENIORITY_DISCOUNT");
    }

    [Fact]
    public void CalculateQuote_HouseholdEmployer_ReturnsCorrectAmount()
    {
        // Arrange
        var request = new QuoteRequest
        {
            Age = 55,
            Status = CustomerStatus.HouseholdEmployer,
            FamilyOption = false,
            AccidentOption = false,
            SeniorityMonths = 12
        };

        // Act
        var result = _quoteService.CalculateQuote(request);

        // Assert
        // 12 (base) + 6 (age 51-65) + 2 (employer) = 20
        Assert.Equal(20m, result.Premium);
        Assert.Contains(result.Breakdown, b => b.Code == "AGE_51_65");
        Assert.Contains(result.Breakdown, b => b.Code == "STATUS_EMPLOYER");
    }

    [Fact]
    public void CalculateQuote_Age18To30_NoAgeImpact()
    {
        // Arrange
        var request = new QuoteRequest
        {
            Age = 30,
            Status = CustomerStatus.SalariedEmployee,
            FamilyOption = false,
            AccidentOption = false,
            SeniorityMonths = 0
        };

        // Act
        var result = _quoteService.CalculateQuote(request);

        // Assert
        // Only age impact with 0 amount should not appear in breakdown
        Assert.DoesNotContain(result.Breakdown, b => b.Code == "AGE_18_30");
    }

    [Fact]
    public void CalculateQuote_Age66To75_HighestAgeImpact()
    {
        // Arrange
        var request = new QuoteRequest
        {
            Age = 70,
            Status = CustomerStatus.SalariedEmployee,
            FamilyOption = false,
            AccidentOption = false,
            SeniorityMonths = 0
        };

        // Act
        var result = _quoteService.CalculateQuote(request);

        // Assert
        // 12 (base) + 9 (age 66-75) - 1 (employee) = 20
        Assert.Equal(20m, result.Premium);
        Assert.Contains(result.Breakdown, b => b.Code == "AGE_66_75" && b.Amount == 9m);
    }

    [Fact]
    public void CalculateQuote_WithSeniorityDiscount_AppliesCorrectly()
    {
        // Arrange
        var request = new QuoteRequest
        {
            Age = 40,
            Status = CustomerStatus.SalariedEmployee,
            FamilyOption = false,
            AccidentOption = false,
            SeniorityMonths = 24
        };

        // Act
        var result = _quoteService.CalculateQuote(request);

        // Assert
        // 12 (base) + 3 (age) - 1 (employee) - 1 (seniority) = 13
        Assert.Equal(13m, result.Premium);
        Assert.Contains(result.Breakdown, b => b.Code == "SENIORITY_DISCOUNT" && b.Amount == -1m);
    }

    [Fact]
    public void ValidateRequest_ValidRequest_ReturnsTrue()
    {
        // Arrange
        var request = new QuoteRequest
        {
            Age = 30,
            Status = CustomerStatus.SalariedEmployee,
            FamilyOption = false,
            AccidentOption = false,
            SeniorityMonths = 12
        };

        // Act
        var (isValid, errors) = _quoteService.ValidateRequest(request);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateRequest_AgeTooYoung_ReturnsError()
    {
        // Arrange
        var request = new QuoteRequest
        {
            Age = 17,
            Status = CustomerStatus.SalariedEmployee,
            FamilyOption = false,
            AccidentOption = false,
            SeniorityMonths = 0
        };

        // Act
        var (isValid, errors) = _quoteService.ValidateRequest(request);

        // Assert
        Assert.False(isValid);
        Assert.Single(errors);
        Assert.Equal("age", errors[0].Field);
        Assert.Contains("between 18 and 75", errors[0].Message);
    }

    [Fact]
    public void ValidateRequest_AgeTooOld_ReturnsError()
    {
        // Arrange
        var request = new QuoteRequest
        {
            Age = 76,
            Status = CustomerStatus.SalariedEmployee,
            FamilyOption = false,
            AccidentOption = false,
            SeniorityMonths = 0
        };

        // Act
        var (isValid, errors) = _quoteService.ValidateRequest(request);

        // Assert
        Assert.False(isValid);
        Assert.Single(errors);
        Assert.Equal("age", errors[0].Field);
    }

    [Fact]
    public void ValidateRequest_NegativeSeniority_ReturnsError()
    {
        // Arrange
        var request = new QuoteRequest
        {
            Age = 30,
            Status = CustomerStatus.SalariedEmployee,
            FamilyOption = false,
            AccidentOption = false,
            SeniorityMonths = -1
        };

        // Act
        var (isValid, errors) = _quoteService.ValidateRequest(request);

        // Assert
        Assert.False(isValid);
        Assert.Single(errors);
        Assert.Equal("seniorityMonths", errors[0].Field);
    }
}
