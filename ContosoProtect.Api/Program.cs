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

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors();

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

// Get all quotes with pagination
app.MapGet("/quotes", (QuoteDbContext db, int page = 1, int pageSize = 10) =>
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 10;

    var totalQuotes = db.Quotes.Count();
    var quotes = db.Quotes
        .OrderByDescending(q => q.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    return Results.Ok(new
    {
        page,
        pageSize,
        totalQuotes,
        totalPages = (int)Math.Ceiling(totalQuotes / (double)pageSize),
        quotes
    });
})
.WithName("GetAllQuotes")
.WithTags("Quote")
.Produces(200);

// Get specific quote by ID
app.MapGet("/quotes/{id}", (int id, QuoteDbContext db) =>
{
    var quote = db.Quotes.Find(id);
    if (quote == null)
    {
        return Results.NotFound(new { error = "Quote not found", id });
    }

    return Results.Ok(quote);
})
.WithName("GetQuoteById")
.WithTags("Quote")
.Produces(200)
.Produces(404);

// Get statistics
app.MapGet("/statistics", (QuoteDbContext db) =>
{
    var quotes = db.Quotes.ToList();
    
    if (!quotes.Any())
    {
        return Results.Ok(new
        {
            totalQuotes = 0,
            averagePremium = 0m,
            minPremium = 0m,
            maxPremium = 0m,
            byStatus = new Dictionary<string, int>(),
            withFamilyOption = 0,
            withAccidentOption = 0
        });
    }

    var byStatus = quotes
        .GroupBy(q => q.Status.ToString())
        .ToDictionary(g => g.Key, g => g.Count());

    return Results.Ok(new
    {
        totalQuotes = quotes.Count,
        averagePremium = Math.Round(quotes.Average(q => q.Premium), 2),
        minPremium = quotes.Min(q => q.Premium),
        maxPremium = quotes.Max(q => q.Premium),
        byStatus,
        withFamilyOption = quotes.Count(q => q.FamilyOption),
        withAccidentOption = quotes.Count(q => q.AccidentOption)
    });
})
.WithName("GetStatistics")
.WithTags("Statistics")
.Produces(200);

app.Run();

// Make Program class accessible for testing
public partial class Program { }
