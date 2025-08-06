using Serilog;
using EnterpriseAuth.API.Extensions;
using EnterpriseAuth.API.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Custom service extensions
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerDocumentation();
builder.Services.AddCorsPolicy();
builder.Services.AddCustomHealthChecks(builder.Configuration);
builder.Services.AddProductionConfiguration(builder.Configuration);

// Add authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Enterprise Auth API V1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

// Custom middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'");

    await next();
});

// Serilog request logging
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

// CORS
app.UseCors("AllowSpecificOrigins");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Health checks
app.MapHealthChecks("/health");

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EnterpriseAuth.Infrastructure.Data.ApplicationDbContext>();
    try
    {
        if (app.Environment.IsDevelopment())
        {
            // Development: Ensure database is created
            context.Database.EnsureCreated();
            Log.Information("Database ensured created successfully");
        }
        else
        {
            // Production: Run migrations
            context.Database.Migrate();
            Log.Information("Database migration completed successfully");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred during database initialization");
        throw;
    }
}

try
{
    Log.Information("Starting Enterprise Auth API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
