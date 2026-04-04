using System.Security.Claims;
using Api.Application.DTOs.StallMedia;
using Api.Domain.Settings;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.DTOs.Common;
using Shared.DTOs.StallMedia;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/stall-media")]
    [Authorize]
    public class StallMediaController : ControllerBase
    {
        private const int MaxPageSize = 100;
        private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp", "image/jpg", "image/bmp"
        };

        private readonly AppDbContext _context;
        private readonly ILogger<StallMediaController> _logger;
        private readonly BlobStorageSettings _blobSettings;

        public StallMediaController(AppDbContext context, ILogger<StallMediaController> logger, IOptions<BlobStorageSettings> blobSettings)
        {
            _context = context;
            _logger = logger;
            _blobSettings = blobSettings.Value;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCreate([FromForm] StallMediaUploadCreateRequest request)
        {
            _logger.LogInformation("Bắt đầu upload và tạo stall media - StallId: {StallId}", request.StallId);

            if (!TryGetUserId(out var userId))
                return this.UnauthorizedResult("Không xác thực");

            if (!IsAdmin() && !IsBusinessOwner())
                return this.ForbiddenResult("Không có quyền truy cập");

            var stall = await _context.Stalls
                .Include(s => s.Business)
                .FirstOrDefaultAsync(s => s.Id == request.StallId);

            if (stall == null)
                return this.NotFoundResult("Không tìm thấy stall");

            if (!IsAdmin() && stall.Business.OwnerUserId != userId)
                return this.ForbiddenResult("Không có quyền truy cập");

            string mediaUrl;
            string mediaType;

            try
            {
                (mediaUrl, mediaType) = await UploadImageToBlobAsync(request.ImageFile, request.StallId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResult<StallMediaDetailDto>.FromError(new ErrorDetail
                {
                    Code = ErrorCode.Validation,
                    Message = ex.Message
                }));
            }

            var media = new Api.Domain.Entities.StallMedia
            {
                StallId = request.StallId,
                MediaUrl = mediaUrl,
                MediaType = mediaType,
                Caption = request.Caption,
                SortOrder = request.SortOrder,
                IsActive = request.IsActive
            };

            _context.StallMedia.Add(media);
            await _context.SaveChangesAsync();

            return this.OkResult(MapDetail(media));
        }

        [HttpPut("{id:guid}/upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadUpdate(Guid id, [FromForm] StallMediaUploadUpdateRequest request)
        {
            _logger.LogInformation("Bắt đầu upload và cập nhật stall media - Id: {MediaId}", id);

            if (!TryGetUserId(out var userId))
                return this.UnauthorizedResult("Không xác thực");

            if (!IsAdmin() && !IsBusinessOwner())
                return this.ForbiddenResult("Không có quyền truy cập");

            var media = await _context.StallMedia
                .Include(m => m.Stall)
                .ThenInclude(s => s.Business)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null)
                return this.NotFoundResult("Không tìm thấy stall media");

            if (!IsAdmin() && media.Stall.Business.OwnerUserId != userId)
                return this.ForbiddenResult("Không có quyền truy cập");

            string mediaUrl;
            string mediaType;

            try
            {
                (mediaUrl, mediaType) = await UploadImageToBlobAsync(request.ImageFile, media.StallId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResult<StallMediaDetailDto>.FromError(new ErrorDetail
                {
                    Code = ErrorCode.Validation,
                    Message = ex.Message
                }));
            }

            media.MediaUrl = mediaUrl;
            media.MediaType = mediaType;
            media.Caption = request.Caption;
            media.SortOrder = request.SortOrder;
            media.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return this.OkResult(MapDetail(media));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("Bắt đầu xóa stall media - Id: {MediaId}", id);

            if (!TryGetUserId(out var userId))
                return this.UnauthorizedResult("Không xác thực");

            if (!IsAdmin() && !IsBusinessOwner())
                return this.ForbiddenResult("Không có quyền truy cập");

            var media = await _context.StallMedia
                .Include(m => m.Stall)
                .ThenInclude(s => s.Business)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null)
                return this.NotFoundResult("Không tìm thấy stall media");

            if (!IsAdmin() && media.Stall.Business.OwnerUserId != userId)
                return this.ForbiddenResult("Không có quyền truy cập");

            _context.StallMedia.Remove(media);
            await _context.SaveChangesAsync();

            return this.OkResult(true);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StallMediaCreateDto request)
        {
            _logger.LogInformation("Bắt đầu tạo stall media - StallId: {StallId}", request.StallId);

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            if (!IsAdmin() && !IsBusinessOwner())
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            var stall = await _context.Stalls
                .Include(s => s.Business)
                .FirstOrDefaultAsync(s => s.Id == request.StallId);

            if (stall == null)
            {
                return this.NotFoundResult("Không tìm thấy stall");
            }

            if (!IsAdmin() && stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            var media = new Api.Domain.Entities.StallMedia
            {
                StallId = request.StallId,
                MediaUrl = request.MediaUrl,
                MediaType = request.MediaType,
                Caption = request.Caption,
                SortOrder = request.SortOrder,
                IsActive = request.IsActive
            };

            _context.StallMedia.Add(media);
            await _context.SaveChangesAsync();

            return this.OkResult(MapDetail(media));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] StallMediaUpdateDto request)
        {
            _logger.LogInformation("Bắt đầu cập nhật stall media - Id: {MediaId}", id);

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            if (!IsAdmin() && !IsBusinessOwner())
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            var media = await _context.StallMedia
                .Include(m => m.Stall)
                .ThenInclude(s => s.Business)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null)
            {
                return this.NotFoundResult("Không tìm thấy stall media");
            }

            if (!IsAdmin() && media.Stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            media.MediaUrl = request.MediaUrl;
            media.MediaType = request.MediaType;
            media.Caption = request.Caption;
            media.SortOrder = request.SortOrder;
            media.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return this.OkResult(MapDetail(media));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            _logger.LogInformation("Bắt đầu lấy chi tiết stall media - Id: {MediaId}", id);

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            var media = await _context.StallMedia
                .Include(m => m.Stall)
                .ThenInclude(s => s.Business)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null)
            {
                return this.NotFoundResult("Không tìm thấy stall media");
            }

            if (!IsAdmin() && media.Stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            return this.OkResult(MapDetail(media));
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? stallId = null, [FromQuery] bool? isActive = null)
        {
            _logger.LogInformation("Bắt đầu lấy danh sách stall media - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            if (!IsAdmin() && !IsBusinessOwner())
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _context.StallMedia
                .Include(m => m.Stall)
                .ThenInclude(s => s.Business)
                .AsNoTracking()
                .AsQueryable();

            if (!IsAdmin())
            {
                query = query.Where(m => m.Stall.Business.OwnerUserId == userId);
            }

            if (stallId.HasValue)
            {
                query = query.Where(m => m.StallId == stallId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(m => m.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            var mediaList = await query
                .OrderByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = mediaList.Select(MapDetail).ToList();

            var result = new PagedResult<StallMediaDetailDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return this.OkResult(result);
        }

        private static StallMediaDetailDto MapDetail(Api.Domain.Entities.StallMedia media)
        {
            return new StallMediaDetailDto
            {
                Id = media.Id,
                StallId = media.StallId,
                MediaUrl = media.MediaUrl,
                MediaType = media.MediaType,
                Caption = media.Caption,
                SortOrder = media.SortOrder,
                IsActive = media.IsActive
            };
        }

        private async Task<(string mediaUrl, string mediaType)> UploadImageToBlobAsync(IFormFile imageFile, Guid stallId)
        {
            if (!AllowedImageContentTypes.Contains(imageFile.ContentType))
                throw new InvalidOperationException($"Định dạng file không hợp lệ: {imageFile.ContentType}. Chỉ chấp nhận: {string.Join(", ", AllowedImageContentTypes)}");

            if (string.IsNullOrWhiteSpace(_blobSettings.ConnectionString) || string.IsNullOrWhiteSpace(_blobSettings.ContainerName))
                throw new InvalidOperationException("Thiếu cấu hình Blob Storage (ConnectionString/ContainerName).");

            var ext = Path.GetExtension(imageFile.FileName);
            var blobName = $"stall-media/{stallId}/{Guid.NewGuid()}{ext}";

            var containerClient = new BlobServiceClient(_blobSettings.ConnectionString)
                .GetBlobContainerClient(_blobSettings.ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(blobName);
            await using var stream = imageFile.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = imageFile.ContentType });

            _logger.LogInformation("Upload ảnh thành công - BlobName: {BlobName}", blobName);

            return (blobClient.Uri.ToString(), imageFile.ContentType);
        }

        private bool TryGetUserId(out Guid userId)
        {
            var currentUserIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(currentUserIdValue, out userId);
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin") || User.IsInRole("ADMIN");
        }

        private bool IsBusinessOwner()
        {
            return User.IsInRole("BusinessOwner") || User.IsInRole("BUSINESSOWNER");
        }
    }
}
