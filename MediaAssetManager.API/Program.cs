using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Infrastructure.Data;
using MediaAssetManager.Infrastructure.Repositories;
using MediaAssetManager.Services;
using MediaAssetManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient(); // For B2 tests

// Register Repositories (Infrastructure Layer)
builder.Services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();

// Register Services (Service Layer)
builder.Services.AddScoped<IStorageService, B2StorageService>();
builder.Services.AddScoped<IMediaAssetService, MediaAssetService>();

// Database
builder.Services.AddDbContext<MediaAssetContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// Simple health check
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

Log.Information("Starting Media Asset Manager API");
app.Run();