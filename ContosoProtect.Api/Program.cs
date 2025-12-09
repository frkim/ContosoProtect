using ContosoProtect.Api.Data;
using ContosoProtect.Api.Models;
using ContosoProtect.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "ContosoProtect API", 
        Version = "v1",
        Description = "API de démonstration pour ContosoProtect - Calcul de devis d'assurance prévoyance"
    });
});

// Add in-memory database
builder.Services.AddDbContext<QuoteDbContext>(options =>
    options.UseInMemoryDatabase("ContosoProtectDb"));

// Add services
builder.Services.AddScoped<IQuoteService, QuoteService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ContosoProtect API v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

// Seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QuoteDbContext>();
    db.Database.EnsureCreated();
}

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health")
    .Produces(200);

// Quote endpoint
app.MapPost("/quote", (QuoteRequest request, IQuoteService quoteService, QuoteDbContext db) =>
{
    // Validate request
    var (isValid, errors) = quoteService.ValidateRequest(request);
    if (!isValid)
    {
        return Results.BadRequest(new ValidationErrorResponse
        {
            Error = "validation_error",
            Details = errors.AsReadOnly()
        });
    }

    // Calculate quote
    var response = quoteService.CalculateQuote(request);

    // Save to database
    var quote = new Quote
    {
        Age = request.Age,
        Status = request.Status,
        FamilyOption = request.FamilyOption,
        AccidentOption = request.AccidentOption,
        SeniorityMonths = request.SeniorityMonths,
        Premium = response.Premium,
        CreatedAt = DateTime.UtcNow
    };
    db.Quotes.Add(quote);
    db.SaveChanges();

    return Results.Ok(response);
})
.WithName("CalculateQuote")
.WithTags("Quote")
.Produces<QuoteResponse>(200)
.Produces<ValidationErrorResponse>(400);

// Admin reset endpoint
app.MapPost("/admin/reset", (HttpContext context, QuoteDbContext db) =>
{
    // Check admin token
    if (!context.Request.Headers.TryGetValue("X-Admin-Token", out var token) || token != "demo-reset")
    {
        return Results.Unauthorized();
    }

    // Clear all quotes
    db.Quotes.RemoveRange(db.Quotes);
    db.SaveChanges();

    return Results.Ok(new { message = "Database reset successfully", timestamp = DateTime.UtcNow });
})
.WithName("ResetDatabase")
.WithTags("Admin")
.Produces(200)
.Produces(401);

app.Run();

// Make Program class accessible for testing
public partial class Program { }
