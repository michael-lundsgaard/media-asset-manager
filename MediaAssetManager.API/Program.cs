using System.Text.Json.Serialization;
using MediaAssetManager.API.Configuration;
using MediaAssetManager.Services.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// === LOGGING ===
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// === OPTIONS PATTERN (Strongly-typed configuration) ===
builder.Services.Configure<B2StorageOptions>(
    builder.Configuration.GetSection(B2StorageOptions.SectionName));

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

// === ASP.NET CORE SERVICES ===
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Prevent circular reference errors in JSON serialization
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        // Don't serialize null values to reduce payload size
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// === APPLICATION SERVICES (Clean extension methods) ===
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddApplicationServices();
builder.Services.AddHttpClients();

var app = builder.Build();

// === MIDDLEWARE PIPELINE ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

Log.Information("Media Asset Manager API started successfully");
app.Run();