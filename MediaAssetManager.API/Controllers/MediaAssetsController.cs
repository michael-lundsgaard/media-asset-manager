using MediaAssetManager.API.DTOs;
using MediaAssetManager.API.DTOs.Common;
using MediaAssetManager.API.Extensions;
using MediaAssetManager.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MediaAssetManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class MediaAssetsController : ControllerBase
    {
        private readonly IMediaAssetService _service;
        private readonly ILogger<MediaAssetsController> _logger;

        public MediaAssetsController(IMediaAssetService service, ILogger<MediaAssetsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Gets a paginated list of media assets based on query parameters
        /// </summary>
        /// <param name="dto">Query parameters for filtering, sorting, and pagination</param>
        /// <returns>Paginated list of media assets</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<MediaAssetResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResultDto<MediaAssetResponseDto>>> Get([FromQuery] MediaAssetQueryDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var assets = await _service.GetAsync(dto.ToQuery());
            return Ok(assets.ToDto());
        }

        /// <summary>
        /// Gets a specific media asset by ID
        /// </summary>
        /// <param name="id">The media asset ID</param>
        /// <returns>The media asset details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(MediaAssetResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MediaAssetResponseDto>> GetById(int id)
        {
            var asset = await _service.GetByIdAsync(id);

            if (asset == null)
            {
                return NotFound(new ErrorResponseDto
                {
                    Message = $"Media asset with ID {id} not found",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            return Ok(asset.ToDto());
        }
    }
}
