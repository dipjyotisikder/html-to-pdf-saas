using DinkToPdf.Contracts;
using DinkToPdf;
using HTPDF;
using HTPDF.Configuration;
using HTPDF.Middleware;
using HTPDF.Services;
using AspNetCoreRateLimit;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure settings
builder.Services.Configure<ApiKeySettings>(builder.Configuration.GetSection("ApiKeySettings"));
builder.Services.Configure<RequestLimits>(builder.Configuration.GetSection("RequestLimits"));

// Configure rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Register services
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
builder.Services.AddScoped<IPdfMaker, PdfMaker>();
builder.Services.AddSingleton<IHtmlSanitizerService, HtmlSanitizerService>();
builder.Services.AddSingleton<IPdfJobService, PdfJobService>();
builder.Services.AddHostedService<PdfJobService>(provider => 
    (PdfJobService)provider.GetRequiredService<IPdfJobService>());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with API Key authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "HTML to PDF API", 
        Version = "v1",
        Description = "API for converting HTML content to PDF documents with security features including API key authentication, rate limiting, and HTML sanitization."
    });

    // Add API Key authentication to Swagger
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key authentication. Enter your API key in the header 'X-API-Key'.",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HTML to PDF API v1");
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles();

// Add rate limiting middleware
app.UseIpRateLimiting();

// Add API key authentication middleware
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
