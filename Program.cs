using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Register Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure Swagger for API Testing
if (app.Environment.IsDevelopment() || true) 
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// -------------------------------------------------------------------------
// CRITERIA 5: CUSTOM LOGGING AND PERFORMANCE MIDDLEWARE
// -------------------------------------------------------------------------
app.Use(async (context, next) =>
{
    var startTime = DateTime.UtcNow;
    var requestPath = context.Request.Path;
    var method = context.Request.Method;
    
    Console.WriteLine($"[INFO] Incoming Request: {method} {requestPath} at {startTime}");

    await next(); // Process the request down the pipeline

    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
    Console.WriteLine($"[INFO] Outgoing Response: {context.Response.StatusCode} processed in {duration}ms");
});

// Mock In-Memory Database for Users
var users = new List<User>
{
    new User { Id = 1, Name = "Vignesh R", Email = "vignesh@example.com", Age = 25 },
    new User { Id = 2, Name = "Jane Doe", Email = "jane.doe@example.com", Age = 30 }
};

// -------------------------------------------------------------------------
// CRITERIA 2 & 4: CRUD ENDPOINTS WITH INLINE DATA VALIDATION
// -------------------------------------------------------------------------

// 1. GET ALL USERS
app.MapGet("/api/users", () => Results.Ok(users))
   .WithName("GetAllUsers");

// 2. GET USER BY ID (Fixes previous routing constraint bugs)
app.MapGet("/api/users/{id:int}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is not null ? Results.Ok(user) : Results.NotFound(new { Message = $"User with ID {id} not found." });
});

// 3. POST - CREATE USER (With Data Validation)
app.MapPost("/api/users", ([FromBody] User newUser) =>
{
    // Programmatic Validation Execution
    var validationContext = new ValidationContext(newUser);
    var validationResults = new List<ValidationResult>();
    
    if (!Validator.TryValidateObject(newUser, validationContext, validationResults, true))
    {
        return Results.BadRequest(validationResults.Select(r => r.ErrorMessage));
    }

    newUser.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
    users.Add(newUser);
    
    return Results.Created($"/api/users/{newUser.Id}", newUser);
});

// 4. PUT - UPDATE USER (With Data Validation)
app.MapPut("/api/users/{id:int}", (int id, [FromBody] User updatedUser) =>
{
    var existingUser = users.FirstOrDefault(u => u.Id == id);
    if (existingUser is null) return Results.NotFound(new { Message = "User matching that ID was not found." });

    // Validate incoming updated object integrity
    var validationContext = new ValidationContext(updatedUser);
    var validationResults = new List<ValidationResult>();
    if (!Validator.TryValidateObject(updatedUser, validationContext, validationResults, true))
    {
        return Results.BadRequest(validationResults.Select(r => r.ErrorMessage));
    }

    existingUser.Name = updatedUser.Name;
    existingUser.Email = updatedUser.Email;
    existingUser.Age = updatedUser.Age;

    return Results.Ok(existingUser);
});

// 5. DELETE - REMOVE USER
app.MapDelete("/api/users/{id:int}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound(new { Message = "User not found." });

    users.Remove(user);
    return Results.Ok(new { Message = $"User profile {id} successfully deleted." });
});

app.Run();

// -------------------------------------------------------------------------
// USER DATA MODEL (Built-in DataAnnotation Rules)
// -------------------------------------------------------------------------
public class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "User name field is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format schema passed.")]
    public string Email { get; set; } = string.Empty;

    [Range(18, 120, ErrorMessage = "User must meet the regulatory legal age parameter of 18-120.")]
    public int Age { get; set; }
}
