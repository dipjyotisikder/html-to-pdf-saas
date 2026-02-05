using AspNetCoreRateLimit;
using HTPDF.Features.Auth;
using HTPDF.Features.Health;
using HTPDF.Features.Pdf;
using HTPDF.Infrastructure;
using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

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

app.MapAuthEndpoints();
app.MapPdfEndpoints();
app.MapHealthEndpoints();
var appLogger = app.Services.GetRequiredService<ILoggingService<Program>>();
appLogger.LogInfo(LogMessages.Infrastructure.ApiStarted);
appLogger.LogInfo(LogMessages.Infrastructure.SwaggerUrl);

await app.RunAsync();


