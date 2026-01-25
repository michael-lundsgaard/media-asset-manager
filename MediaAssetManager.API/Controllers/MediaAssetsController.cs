using MediaAssetManager.Core.Entities;
using MediaAssetManager.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediaAssetManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaAssetsController : ControllerBase
    {
        private readonly MediaAssetContext _context;
        private readonly ILogger<MediaAssetsController> _logger;

        public MediaAssetsController(MediaAssetContext context, ILogger<MediaAssetsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/mediaassets
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var assets = await _context.MediaAssets
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();

            return Ok(new
            {
                count = assets.Count,
                assets
            });
        }

        // GET: api/mediaassets/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var asset = await _context.MediaAssets.FindAsync(id);

            if (asset == null)
                return NotFound(new { error = "Asset not found" });

            return Ok(asset);
        }

        // POST: api/mediaassets
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAssetRequest request)
        {
            var asset = new MediaAsset
            {
                FileName = request.FileName,
                OriginalFileName = request.OriginalFileName,
                FileSizeBytes = request.FileSizeBytes,
                Title = request.Title,
                UploadedAt = DateTime.UtcNow
            };

            _context.MediaAssets.Add(asset);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created asset {AssetId}", asset.AssetId);

            return CreatedAtAction(nameof(GetById), new { id = asset.AssetId }, asset);
        }

        // DELETE: api/mediaassets/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var asset = await _context.MediaAssets.FindAsync(id);

            if (asset == null)
                return NotFound(new { error = "Asset not found" });

            _context.MediaAssets.Remove(asset);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted asset {AssetId}", id);

            return NoContent();
        }
    }

    public record CreateAssetRequest(
        string FileName,
        string OriginalFileName,
        long FileSizeBytes,
        string? Title
    );
}
