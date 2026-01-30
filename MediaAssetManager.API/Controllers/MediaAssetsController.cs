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
    public class MediaAssetsController(IMediaAssetService service) : ControllerBase
    {

        /// <summary>
        /// Retrieves a paginated list of media assets that match the specified query parameters.
        /// </summary>
        /// <param name="query">
        /// The query parameters used to filter, sort, and paginate the list of media assets. Cannot be null.
        /// </param>
        /// <returns>
        /// An <see cref="ActionResult{T}"/> containing a <see cref="PaginatedResponse{MediaAssetResponse}"/> with the
        /// matching media assets and pagination details if the request is valid; otherwise, an <see cref="ErrorResponse"/> describing the validation errors.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<MediaAssetResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaginatedResponse<MediaAssetResponse>>> Get([FromQuery] MediaAssetQueryRequest query)
        {
            var pagedResult = await service.GetAsync(query.ToQuery());
            var response = pagedResult.ToPaginatedResponse();

            return Ok(response);
        }

        /// <summary>
        /// Retrieves a specific media asset by its unique ID.
        /// </summary>
        /// <param name="id">
        /// The unique identifier of the media asset to retrieve.
        /// </param>
        /// <returns>
        /// An <see cref="ActionResult{T}"/> containing the <see cref="MediaAssetResponse"/> if found;
        /// </returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(MediaAssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MediaAssetResponse>> GetById(int id)
        {
            var asset = await service.GetByIdAsync(id);

            if (asset == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Media asset with ID {id} not found",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            return Ok(asset.ToResponse());
        }
    }
}
