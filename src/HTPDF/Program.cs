using AspNetCoreRateLimit;
using HTPDF.Infrastructure;
using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure Services
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Initialize Database (Migrations and Seeding)
await app.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HTML To PDF API v3.0");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<HTPDF.Infrastructure.Middleware.GlobalExceptionHandler>();
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var appLogger = app.Services.GetRequiredService<ILoggingService<Program>>();
appLogger.LogInfo(LogMessages.Infrastructure.ApiStarted);
appLogger.LogInfo(LogMessages.Infrastructure.SwaggerUrl);

await app.RunAsync();


