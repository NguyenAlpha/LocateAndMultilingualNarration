using System.Security.Claims;
using Api.Extensions;
using Api.Application.Services;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Common;
using Shared.DTOs.Narrations;

namespace Api.Controllers
{
    /// <summary>
    /// Controller quản lý các bản ghi audio cho narration (CRUD).
    /// Business rules chính:
    /// - Yêu cầu user phải xác thực (token hợp lệ) để truy cập tất cả các endpoint.
    /// - Chỉ <c>Admin</c> hoặc <c>BusinessOwner</c> được phép tạo / cập nhật / lấy danh sách.
    /// - Nếu user không phải <c>Admin</c>, chỉ có thể thao tác trên các bản ghi thuộc business mà user sở hữu.
    /// </summary>
    [ApiController]
    [Route("api/narration-audio")]
    [Authorize]
    public class NarrationAudioController : ControllerBase
    {
        private const int MaxPageSize = 100;
        private readonly AppDbContext _context;
        private readonly ILogger<NarrationAudioController> _logger;
        private readonly INarrationAudioService _narrationAudioService;

        /// <summary>
        /// Constructor inject các dependency cần thiết.
        /// </summary>
        /// <param name="context">DbContext để thao tác database.</param>
        /// <param name="logger">Logger cho controller này.</param>
        public NarrationAudioController(AppDbContext context, ILogger<NarrationAudioController> logger, INarrationAudioService narrationAudioService)
        {
            _context = context;
            _logger = logger;
            _narrationAudioService = narrationAudioService;
        }

        /// <summary>
        /// Tạo một NarrationAudio mới.
        /// Business:
        /// - User phải authenticated.
        /// - Chỉ Admin hoặc BusinessOwner được phép tạo.
        /// - Nếu không phải Admin thì chỉ được tạo cho narration content thuộc business của mình.
        /// </summary>
        /// <param name="request">DTO chứa thông tin audio cần tạo.</param>
        /// <returns>Trả về IActionResult chứa DTO chi tiết audio đã tạo hoặc lỗi tương ứng.</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NarrationAudioCreateDto request)
        {
            _logger.LogInformation("Bắt đầu tạo narration audio - NarrationContentId: {NarrationContentId}", request.NarrationContentId);

            // Lấy userId từ claim; nếu không có -> 401
            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            // Chỉ Admin hoặc BusinessOwner được phép thao tác
            if (!IsAdmin() && !IsBusinessOwner())
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            // Tải narration content kèm stall->business để kiểm tra quyền sở hữu
            var narrationContent = await _context.StallNarrationContents
                .Include(n => n.Stall)
                .ThenInclude(s => s.Business)
                .FirstOrDefaultAsync(n => n.Id == request.NarrationContentId);

            // Nếu không tìm thấy narration content -> 404
            if (narrationContent == null)
            {
                return this.NotFoundResult("Không tìm thấy narration content");
            }

            // Nếu không phải Admin thì kiểm tra owner của business
            if (!IsAdmin() && narrationContent.Stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            // Tạo audio mới qua service
            var audio = await _narrationAudioService.CreateFromUploadAsync(
                request.NarrationContentId,
                request.AudioUrl,
                request.BlobId,
                request.Voice,
                request.Provider,
                request.DurationSeconds,
                request.IsTts);

            // Convert thời gian theo timezone client trước khi trả về
            var timeZone = GetTimeZone();
            return this.OkResult(MapDetail(audio, timeZone));
        }

        /// <summary>
        /// Cập nhật một NarrationAudio theo Id.
        /// Business:
        /// - User phải xác thực.
        /// - Chỉ Admin hoặc BusinessOwner được phép cập nhật.
        /// - Nếu không phải Admin thì chỉ được cập nhật các bản ghi thuộc business của mình.
        /// </summary>
        /// <param name="id">Id của audio cần cập nhật.</param>
        /// <param name="request">DTO chứa dữ liệu cập nhật.</param>
        /// <returns>Trả về IActionResult chứa DTO chi tiết sau khi cập nhật hoặc lỗi tương ứng.</returns>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] NarrationAudioUpdateDto request)
        {
            _logger.LogInformation("Bắt đầu cập nhật narration audio - Id: {AudioId}", id);

            // Xác thực user
            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            // Kiểm tra role
            if (!IsAdmin() && !IsBusinessOwner())
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            // Tải audio kèm context (narrationContent -> stall -> business) để kiểm tra quyền
            var audio = await _context.NarrationAudios
                .Include(a => a.NarrationContent)
                .ThenInclude(n => n.Stall)
                .ThenInclude(s => s.Business)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (audio == null)
            {
                return this.NotFoundResult("Không tìm thấy narration audio");
            }

            // Nếu không phải Admin thì kiểm tra quyền sở hữu
            if (!IsAdmin() && audio.NarrationContent.Stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            // Cập nhật audio qua service
            audio = await _narrationAudioService.UpdateFromUploadAsync(
                audio,
                request.AudioUrl,
                request.BlobId,
                request.Voice,
                request.Provider,
                request.DurationSeconds,
                request.IsTts);

            // Trả về DTO đã map
            var timeZone = GetTimeZone();
            return this.OkResult(MapDetail(audio, timeZone));
        }

        /// <summary>
        /// Lấy chi tiết một NarrationAudio theo Id.
        /// Business: User phải xác thực; nếu không phải Admin thì chỉ được xem các bản ghi thuộc business của mình.
        /// </summary>
        /// <param name="id">Id của audio.</param>
        /// <returns>Chi tiết audio hoặc lỗi tương ứng.</returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            _logger.LogInformation("Bắt đầu lấy chi tiết narration audio - Id: {AudioId}", id);

            // Xác thực user
            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            // Lấy audio kèm context để kiểm tra quyền sở hữu (AsNoTracking vì chỉ đọc)
            var audio = await _context.NarrationAudios
                .Include(a => a.NarrationContent)
                .ThenInclude(n => n.Stall)
                .ThenInclude(s => s.Business)
                .Include(a => a.TtsVoiceProfile)
                .ThenInclude(p => p.Language)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (audio == null)
            {
                return this.NotFoundResult("Không tìm thấy narration audio");
            }

            // Nếu không phải Admin thì kiểm tra quyền sở hữu business
            if (!IsAdmin() && audio.NarrationContent.Stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            var timeZone = GetTimeZone();
            return this.OkResult(MapDetail(audio, timeZone));
        }

        /// <summary>
        /// Lấy danh sách NarrationAudio có phân trang và lọc.
        /// Business: User phải xác thực; nếu không phải Admin thì chỉ nhận về các bản ghi thuộc business của user.
        /// </summary>
        /// <param name="page">Số trang (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số bản ghi trên mỗi trang.</param>
        /// <param name="narrationContentId">Lọc theo narrationContentId (tùy chọn).</param>
        /// <param name="stallId">Lọc theo stallId (tùy chọn).</param>
        /// <returns>PagedResult chứa danh sách DTO.</returns>
        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? narrationContentId = null, [FromQuery] Guid? stallId = null)
        {
            _logger.LogInformation("Bắt đầu lấy danh sách narration audio - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            // Xác thực user
            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            // Kiểm tra role đọc danh sách
            if (!IsAdmin() && !IsBusinessOwner())
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            // Chuẩn hóa paging
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            // Xây dựng query cơ bản (kèm navigation properties để filter theo owner nếu cần)
            var query = _context.NarrationAudios
                .Include(a => a.NarrationContent)
                .ThenInclude(n => n.Stall)
                .ThenInclude(s => s.Business)
                .Include(a => a.TtsVoiceProfile)
                .ThenInclude(p => p.Language)
                .AsNoTracking()
                .AsQueryable();

            // Nếu user không phải Admin thì chỉ trả về các bản ghi thuộc business của user
            if (!IsAdmin())
            {
                query = query.Where(a => a.NarrationContent.Stall.Business.OwnerUserId == userId);
            }

            // Áp filter tùy chọn
            if (narrationContentId.HasValue)
            {
                query = query.Where(a => a.NarrationContentId == narrationContentId.Value);
            }

            if (stallId.HasValue)
            {
                query = query.Where(a => a.NarrationContent.StallId == stallId.Value);
            }

            // Lấy tổng và phân trang
            var totalCount = await query.CountAsync();
            var audios = await query
                .OrderByDescending(a => a.UpdatedAt)
                .ThenByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map entity -> DTO và convert thời gian
            var timeZone = GetTimeZone();
            var items = audios.Select(a => MapDetail(a, timeZone)).ToList();

            var result = new PagedResult<NarrationAudioDetailDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return this.OkResult(result);
        }

        private static NarrationAudioDetailDto MapDetail(Api.Domain.Entities.NarrationAudio audio, TimeZoneInfo timeZone)
        {
            return new NarrationAudioDetailDto
            {
                Id = audio.Id,
                NarrationContentId = audio.NarrationContentId,
                TtsVoiceProfileId = audio.TtsVoiceProfileId,
                TtsVoiceProfileDescription = audio.TtsVoiceProfile?.Description,
                TtsVoiceProfileLanguageName = audio.TtsVoiceProfile?.Language?.Name,
                AudioUrl = audio.AudioUrl,
                BlobId = audio.BlobId,
                Voice = audio.Voice,
                Provider = audio.Provider,
                DurationSeconds = audio.DurationSeconds,
                IsTts = audio.IsTts,
                // Convert UpdatedAt (lưu ở UTC) sang timezone client
                UpdatedAt = ConvertFromUtc(audio.UpdatedAt, timeZone)
            };
        }

        private static DateTimeOffset? ConvertFromUtc(DateTimeOffset? utcDateTime, TimeZoneInfo timeZone)
        {
            if (utcDateTime == null)
            {
                return null;
            }

            // Lấy DateTime ở dạng UTC từ DateTimeOffset và chuyển sang thời gian local theo timeZone
            var utc = utcDateTime.Value.UtcDateTime;
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
            var offset = timeZone.GetUtcOffset(utc);
            return new DateTimeOffset(local, offset);
        }

        private TimeZoneInfo GetTimeZone()
        {
            // Đọc header X-TimeZoneId do client gửi. Nếu không có -> dùng múi giờ mặc định SE Asia
            var timeZoneId = HttpContext.Request.Headers["X-TimeZoneId"].ToString();
            return string.IsNullOrWhiteSpace(timeZoneId)
                ? TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
                : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }

        private bool TryGetUserId(out Guid userId)
        {
            // Lấy claim NameIdentifier (thường là user id) và parse sang Guid
            var currentUserIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(currentUserIdValue, out userId);
        }

        private bool IsAdmin()
        {
            // Kiểm tra user có role Admin (case-insensitive check bằng 2 giá trị)
            return User.IsInRole("Admin") || User.IsInRole("ADMIN");
        }

        private bool IsBusinessOwner()
        {
            // Kiểm tra user có role BusinessOwner
            return User.IsInRole("BusinessOwner") || User.IsInRole("BUSINESSOWNER");
        }
    }
}
