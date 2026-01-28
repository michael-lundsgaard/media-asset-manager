using MediaAssetManager.Infrastructure.Data;
using MediaAssetManager.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace MediaAssetManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TestController> _logger;
        private readonly MediaAssetContext _context;
        private readonly IStorageService _storageService;

        public TestController(IConfiguration configuration, ILogger<TestController> logger, MediaAssetContext context, IStorageService storageService)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _storageService = storageService;
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            });
        }

        [HttpGet("database")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                await using var cmd = new NpgsqlCommand("SELECT version()", connection);
                var version = await cmd.ExecuteScalarAsync();

                _logger.LogInformation("Database connection successful");

                return Ok(new
                {
                    status = "connected",
                    database = "PostgreSQL (Supabase)",
                    version = version?.ToString()?.Substring(0, 50) + "..."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection failed");
                return StatusCode(500, new
                {
                    status = "failed",
                    error = ex.Message
                });
            }
        }

        [HttpGet("connection-info")]
        public IActionResult GetConnectionInfo()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Parse connection string safely (hide password)
            var builder = new NpgsqlConnectionStringBuilder(connectionString);

            return Ok(new
            {
                host = builder.Host,
                port = builder.Port,
                database = builder.Database,
                username = builder.Username,
                sslMode = builder.SslMode.ToString(),
                hasPassword = !string.IsNullOrEmpty(builder.Password),
                fullConnectionString = connectionString?.Replace(builder.Password ?? "", "***HIDDEN***")
            });
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] UploadMediaRequest req)
        {
            try
            {
                if (req.File == null || req.File.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                _logger.LogInformation("Uploading file: {FileName} ({Size} bytes)", req.File.FileName, req.File.Length);

                using var stream = req.File.OpenReadStream();
                var result = await _storageService.UploadFileAsync(stream, req.File.FileName, req.File.ContentType);

                _logger.LogInformation(
                    "Uploading file: {FileName} ({Size} bytes)",
                    req.File.FileName,
                    req.File.Length
                );

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File upload failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public class UploadMediaRequest
        {
            public IFormFile File { get; set; } = null!;
        }
    }
}
