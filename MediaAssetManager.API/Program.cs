using System.Text.Json;
using System.Text.Json.Serialization;
using MediaAssetManager.API.Configuration;
using MediaAssetManager.Services.Configuration;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerUI;

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
        // Use camelCase naming convention in JSON
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        // Serialize enums as strings instead of numbers
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
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

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Media Asset Manager API V1");
    options.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root

    options.DocumentTitle = "Media Asset Manager API Documentation";
    options.DocExpansion(DocExpansion.List);
    options.DefaultModelExpandDepth(2);
    options.DisplayRequestDuration();
    options.EnableFilter();

    // Enable "Try it out" by default
    options.EnableTryItOutByDefault();

    // Persist authorization
    options.EnablePersistAuthorization();
});

app.UseHttpsRedirection();
app.MapControllers();

Log.Information("Media Asset Manager API started successfully");
app.Run();