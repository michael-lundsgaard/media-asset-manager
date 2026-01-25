using MediaAssetManager.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MediaAssetManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TestController> _logger;
        private readonly MediaAssetContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public TestController(IConfiguration configuration, ILogger<TestController> logger, MediaAssetContext context, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _httpClientFactory = httpClientFactory;
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

        [HttpGet("b2")]
        public async Task<IActionResult> TestB2()
        {
            try
            {
                var accountId = _configuration["B2:AccountId"];
                var applicationKey = _configuration["B2:ApplicationKey"];
                var bucketName = _configuration["B2:BucketName"];

                // Test B2 authorization
                var authString = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{accountId}:{applicationKey}")
                );

                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Get,
                    "https://api.backblazeb2.com/b2api/v2/b2_authorize_account");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<JsonElement>(json);

                _logger.LogInformation("B2 authorization successful");

                return Ok(new
                {
                    status = "connected",
                    service = "Backblaze B2",
                    bucketName = bucketName,
                    accountId = accountId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B2 connection failed");
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
    }
}
