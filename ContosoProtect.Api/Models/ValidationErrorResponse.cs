namespace ContosoProtect.Api.Models;

public sealed class ValidationErrorResponse
{
    public string Error { get; set; } = "validation_error";
    public IReadOnlyList<ValidationDetail> Details { get; set; } = Array.Empty<ValidationDetail>();
}

public sealed class ValidationDetail
{
    public string Field { get; set; } = default!;
    public string Message { get; set; } = default!;
}
