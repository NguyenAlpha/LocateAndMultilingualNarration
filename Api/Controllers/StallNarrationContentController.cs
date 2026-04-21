using Api.Application.Services;
using Api.Authorization;
using Api.Domain.Entities;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Api.Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Common;
using Shared.DTOs.Narrations;

namespace Api.Controllers
{
    /// <summary>
    /// Controller quản lý nội dung narration cho stall (CRUD).
    /// Business rules chính:
    /// - Yêu cầu người dùng phải xác thực (token hợp lệ).
    /// - Chỉ <c>Admin</c> hoặc <c>BusinessOwner</c> được phép tạo/cập nhật/danh sách.
    /// - Nếu không phải <c>Admin</c>, chỉ có thể thao tác trên các bản ghi thuộc business do user sở hữu.
    /// </summary>
    [ApiController]
    [Route("api/stall-narration-content")]
    [Authorize]
    public class StallNarrationContentController : AppControllerBase
    {
        private const int MaxPageSize = 100;
        private readonly AppDbContext _context;
        private readonly ILogger<StallNarrationContentController> _logger;

        public StallNarrationContentController(AppDbContext context, ILogger<StallNarrationContentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tạo một StallNarrationContent mới.
        /// Business:
        /// - Người gọi phải được xác thực.
        /// - Chỉ <c>Admin</c> hoặc <c>BusinessOwner</c> được phép.
        /// - Nếu là <c>BusinessOwner</c>, chỉ được tạo cho stall thuộc business của mình.
        /// </summary>
        /// <param name="request">Dữ liệu tạo narration content.</param>
        /// <returns>Trả về <see cref="IActionResult"/> với kết quả tạo hoặc lỗi tương ứng.</returns>
        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOrBusinessOwner)]
        public async Task<IActionResult> Create([FromBody] StallNarrationContentCreateDto request)
        {
            _logger.LogInformation("Bắt đầu tạo narration content - StallId: {StallId}", request.StallId);

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            // Tải stall cùng business để kiểm tra quyền sở hữu (nếu user không phải Admin)
            var stall = await _context.Stalls
                .Include(s => s.Business)
                .FirstOrDefaultAsync(s => s.Id == request.StallId);

            // Nếu stall không tồn tại -> 404
            if (stall == null)
            {
                return this.NotFoundResult("Không tìm thấy stall");
            }

            // Nếu không phải Admin thì đảm bảo stall thuộc business của user
            if (!IsAdmin() && stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            // Kiểm tra plan có hỗ trợ TTS không (Admin bypass)
            if (!IsAdmin())
            {
                var effectivePlan = stall.Business.PlanExpiresAt.HasValue && stall.Business.PlanExpiresAt.Value <= DateTimeOffset.UtcNow
                    ? Api.Domain.SubscriptionPlan.Free
                    : stall.Business.Plan;

                if (!Api.Domain.SubscriptionPlan.AllowsTts(effectivePlan))
                {
                    _logger.LogWarning("Plan không hỗ trợ TTS - BusinessId: {BusinessId}, Plan: {Plan}", stall.BusinessId, effectivePlan);
                    return this.ForbiddenResult("Gói Free không hỗ trợ tính năng TTS. Vui lòng nâng cấp lên Basic hoặc Pro.");
                }
            }

            // Kiểm tra language tồn tại và đang active
            var language = await _context.Languages.GetActiveByIdAsync(request.LanguageId);

            if (language == null)
            {
                // Trả về lỗi kèm tên trường để client biết field nào sai
                return this.NotFoundResult("Không tìm thấy language", "LanguageId");
            }

            // Nếu content mới là active thì deactivate tất cả content khác của cùng stall
            if (request.IsActive)
            {
                var others = await _context.StallNarrationContents
                    .Where(c => c.StallId == request.StallId && c.IsActive)
                    .ToListAsync();
                foreach (var other in others)
                    other.IsActive = false;
            }

            // Tạo entity mới từ DTO (lưu UpdatedAt ở UTC)
            var content = new StallNarrationContent
            {
                StallId     = request.StallId,
                LanguageId  = request.LanguageId,
                Title       = request.Title,
                Description = request.Description,
                ScriptText  = request.ScriptText,
                IsActive    = request.IsActive,
                TtsStatus   = TtsJobStatus.Pending,
                UpdatedAt   = DateTimeOffset.UtcNow
            };

            // Thêm và lưu thay đổi (deactivate others + add new trong cùng transaction)
            _context.StallNarrationContents.Add(content);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Narration content tạo thành công, TTS đã queue - ContentId: {ContentId}", content.Id);

            var timeZone = GetTimeZone();
            return this.OkResult(new StallNarrationContentWithAudiosDto
            {
                Content = MapDetail(content, timeZone),
                Audios  = []
            });
        }

        /// <summary>
        /// Cập nhật một StallNarrationContent.
        /// Business:
        /// - Người gọi phải được xác thực.
        /// - Chỉ <c>Admin</c> hoặc <c>BusinessOwner</c> được phép.
        /// - Nếu là <c>BusinessOwner</c>, chỉ được cập nhật bản ghi thuộc business của mình.
        /// </summary>
        /// <param name="id">Id của narration content cần cập nhật.</param>
        /// <param name="request">Dữ liệu cập nhật.</param>
        /// <returns>Trả về <see cref="IActionResult"/> với kết quả cập nhật hoặc lỗi tương ứng.</returns>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = AppPolicies.AdminOrBusinessOwner)]
        public async Task<IActionResult> Update(Guid id, [FromBody] StallNarrationContentUpdateDto request)
        {
            _logger.LogInformation("Bắt đầu cập nhật narration content - Id: {ContentId}", id);

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            // Tải content kèm thông tin stall->business để kiểm tra quyền sở hữu
            var content = await _context.StallNarrationContents
                .Include(n => n.Stall)
                .ThenInclude(s => s.Business)
                .FirstOrDefaultAsync(n => n.Id == id);

            // Nếu không tìm thấy -> 404
            if (content == null)
            {
                return this.NotFoundResult("Không tìm thấy narration content");
            }

            // Nếu không phải Admin thì kiểm tra owner của business
            if (!IsAdmin() && content.Stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            // Nếu cập nhật thành active thì deactivate tất cả content khác của cùng stall
            if (request.IsActive)
            {
                var others = await _context.StallNarrationContents
                    .Where(c => c.StallId == content.StallId && c.Id != id && c.IsActive)
                    .ToListAsync();
                foreach (var other in others)
                    other.IsActive = false;
            }

            // Kiểm tra ScriptText có thay đổi không để quyết định có chạy TTS lại không
            var scriptChanged = content.ScriptText != request.ScriptText;

            // Áp các cập nhật từ DTO vào entity và set UpdatedAt
            content.Title = request.Title;
            content.Description = request.Description;
            content.ScriptText = request.ScriptText;
            content.IsActive = request.IsActive;
            content.UpdatedAt = DateTimeOffset.UtcNow;

            // Lưu deactivate others + cập nhật content trong cùng transaction
            await _context.SaveChangesAsync();

            if (scriptChanged)
            {
                // Kiểm tra plan có hỗ trợ TTS không (Admin bypass; BusinessOwner Free bỏ qua TTS)
                var canRunTts = IsAdmin();
                if (!canRunTts)
                {
                    var effectivePlan = content.Stall.Business.PlanExpiresAt.HasValue && content.Stall.Business.PlanExpiresAt.Value <= DateTimeOffset.UtcNow
                        ? Api.Domain.SubscriptionPlan.Free
                        : content.Stall.Business.Plan;
                    canRunTts = Api.Domain.SubscriptionPlan.AllowsTts(effectivePlan);
                }

                if (canRunTts)
                {
                    content.TtsStatus = TtsJobStatus.Pending;
                    content.TtsError  = null;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Script thay đổi, TTS đã queue lại - ContentId: {ContentId}", content.Id);
                }
                else
                {
                    _logger.LogInformation("Bỏ qua TTS vì plan không hỗ trợ - BusinessId: {BusinessId}", content.Stall.BusinessId);
                }
            }

            // Trả về DTO đã map (đã convert thời gian theo timezone client)
            var timeZone = GetTimeZone();
            return this.OkResult(MapDetail(content, timeZone));
        }

        /// <summary>
        /// Đổi trạng thái IsActive của một StallNarrationContent.
        /// Nếu set active = true thì deactivate tất cả content khác của cùng stall.
        /// </summary>
        [HttpPatch("{id:guid}/status")]
        [Authorize(Policy = AppPolicies.AdminOrBusinessOwner)]
        public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] bool isActive)
        {
            _logger.LogInformation("Bắt đầu đổi trạng thái narration content - Id: {ContentId}, IsActive: {IsActive}", id, isActive);

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            var content = await _context.StallNarrationContents
                .Include(n => n.Stall)
                .ThenInclude(s => s.Business)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (content == null)
            {
                return this.NotFoundResult("Không tìm thấy narration content");
            }

            if (!IsAdmin() && content.Stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            if (isActive)
            {
                var others = await _context.StallNarrationContents
                    .Where(c => c.StallId == content.StallId && c.Id != id && c.IsActive)
                    .ToListAsync();
                foreach (var other in others)
                    other.IsActive = false;
            }

            content.IsActive = isActive;
            content.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            var timeZone = GetTimeZone();
            return this.OkResult(MapDetail(content, timeZone));
        }

        /// <summary>
        /// Lấy chi tiết một StallNarrationContent theo Id.
        /// Business:
        /// - Người gọi phải xác thực.
        /// - Nếu không phải Admin, chỉ có thể xem các nội dung của business mình.
        /// </summary>
        /// <param name="id">Id của narration content.</param>
        /// <returns>Chi tiết narration content hoặc lỗi tương ứng.</returns>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = AppPolicies.AdminOrBusinessOwner)]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            _logger.LogInformation("Bắt đầu lấy chi tiết narration content - Id: {ContentId}", id);

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            // Lấy nội dung (AsNoTracking vì chỉ đọc)
            var content = await _context.StallNarrationContents
                .Include(n => n.Stall)
                .ThenInclude(s => s.Business)
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (content == null)
            {
                return this.NotFoundResult("Không tìm thấy narration content");
            }

            // Nếu không phải Admin thì kiểm tra quyền sở hữu business
            if (!IsAdmin() && content.Stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            // Convert thời gian theo timezone client trước khi trả về
            var timeZone = GetTimeZone();
            var audios = await _context.NarrationAudios
                .Include(a => a.TtsVoiceProfile)
                .ThenInclude(p => p.Language)
                .AsNoTracking()
                .Where(a => a.NarrationContentId == content.Id)
                .OrderByDescending(a => a.UpdatedAt)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            var audioItems = audios.Select(a => MapAudioDetail(a, timeZone)).ToList();

            return this.OkResult(new StallNarrationContentWithAudiosDto
            {
                Content = MapDetail(content, timeZone),
                Audios = audioItems
            });
        }

        /// <summary>
        /// Lấy danh sách StallNarrationContent với phân trang và lọc.
        /// Business:
        /// - Người gọi phải xác thực.
        /// - Nếu không phải Admin, chỉ nhận về các bản ghi thuộc business của user.
        /// </summary>
        /// <param name="page">Số trang (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số bản ghi trên trang.</param>
        /// <param name="stallId">Lọc theo StallId (tùy chọn).</param>
        /// <param name="languageId">Lọc theo LanguageId (tùy chọn).</param>
        /// <returns>Danh sách phân trang các narration content.</returns>
        [HttpGet]
        [Authorize(Policy = AppPolicies.AdminOrBusinessOwner)]
        public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? stallId = null, [FromQuery] Guid? languageId = null, [FromQuery] string? search = null, [FromQuery] bool? isActive = null)
        {
            _logger.LogInformation("Bắt đầu lấy danh sách narration content - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            // Chuẩn hóa paging
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            // Xây dựng query cơ bản (kèm Stall->Business để filter theo owner nếu cần)
            var query = _context.StallNarrationContents
                .Include(n => n.Stall)
                .ThenInclude(s => s.Business)
                .AsNoTracking()
                .AsQueryable();

            // Nếu không phải Admin thì chỉ lấy các record thuộc business của user
            if (!IsAdmin())
            {
                query = query.Where(n => n.Stall.Business.OwnerUserId == userId);
            }

            // Áp filter tùy chọn
            if (stallId.HasValue)
            {
                query = query.Where(n => n.StallId == stallId.Value);
            }

            if (languageId.HasValue)
            {
                query = query.Where(n => n.LanguageId == languageId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(n => n.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(n =>
                    EF.Functions.Like(n.Title, $"%{keyword}%") ||
                    (n.Description != null && EF.Functions.Like(n.Description, $"%{keyword}%")) ||
                    EF.Functions.Like(n.ScriptText, $"%{keyword}%"));
            }

            // Lấy tổng số và phân trang
            var totalCount = await query.CountAsync();
            var contents = await query
                .OrderByDescending(n => n.UpdatedAt)
                .ThenByDescending(n => n.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map entity -> DTO và convert thời gian
            var timeZone = GetTimeZone();
            var items = contents.Select(c => MapDetail(c, timeZone)).ToList();

            var result = new PagedResult<StallNarrationContentDetailDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return this.OkResult(result);
        }

        /// <summary>
        /// Lấy trạng thái TTS và danh sách audio của một StallNarrationContent.
        /// Dùng cho JS polling để biết khi nào TTS hoàn thành.
        /// </summary>
        [HttpGet("{id:guid}/tts-status")]
        [Authorize(Policy = AppPolicies.AdminOrBusinessOwner)]
        public async Task<IActionResult> GetTtsStatus(Guid id)
        {
            if (!TryGetUserId(out var userId))
                return this.UnauthorizedResult("Không xác thực");

            var content = await _context.StallNarrationContents
                .Include(n => n.Stall).ThenInclude(s => s.Business)
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (content == null)
                return this.NotFoundResult("Không tìm thấy narration content");

            if (!IsAdmin() && content.Stall.Business.OwnerUserId != userId)
                return this.ForbiddenResult("Không có quyền truy cập");

            var audios = content.TtsStatus == TtsJobStatus.Completed
                ? await _context.NarrationAudios
                    .Include(a => a.TtsVoiceProfile).ThenInclude(p => p!.Language)
                    .AsNoTracking()
                    .Where(a => a.NarrationContentId == id)
                    .OrderByDescending(a => a.UpdatedAt)
                    .ToListAsync()
                : [];

            var timeZone = GetTimeZone();
            return this.OkResult(new TtsStatusDto
            {
                Id        = content.Id,
                TtsStatus = content.TtsStatus,
                TtsError  = content.TtsError,
                Audios    = audios.Select(a => MapAudioDetail(a, timeZone)).ToList()
            });
        }

        /// <summary>
        /// Reset TTS từ Failed về Pending để thử lại.
        /// </summary>
        [HttpPost("{id:guid}/retry-tts")]
        [Authorize(Policy = AppPolicies.AdminOrBusinessOwner)]
        public async Task<IActionResult> RetryTts(Guid id)
        {
            if (!TryGetUserId(out var userId))
                return this.UnauthorizedResult("Không xác thực");

            var content = await _context.StallNarrationContents
                .Include(n => n.Stall).ThenInclude(s => s.Business)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (content == null)
                return this.NotFoundResult("Không tìm thấy narration content");

            if (!IsAdmin() && content.Stall.Business.OwnerUserId != userId)
                return this.ForbiddenResult("Không có quyền truy cập");

            if (content.TtsStatus != TtsJobStatus.Failed)
                return this.BadRequestResult("Chỉ có thể thử lại khi TTS đang ở trạng thái Failed.");

            content.TtsStatus = TtsJobStatus.Pending;
            content.TtsError  = null;
            content.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return this.OkResult(MapDetail(content, GetTimeZone()));
        }

        private static StallNarrationContentDetailDto MapDetail(StallNarrationContent content, TimeZoneInfo timeZone)
        {
            return new StallNarrationContentDetailDto
            {
                Id          = content.Id,
                StallId     = content.StallId,
                LanguageId  = content.LanguageId,
                Title       = content.Title,
                Description = content.Description,
                ScriptText  = content.ScriptText,
                IsActive    = content.IsActive,
                TtsStatus   = content.TtsStatus,
                TtsError    = content.TtsError,
                UpdatedAt   = ConvertFromUtc(content.UpdatedAt, timeZone)
            };
        }

        private static NarrationAudioDetailDto MapAudioDetail(NarrationAudio audio, TimeZoneInfo timeZone)
        {
            return new NarrationAudioDetailDto
            {
                Id = audio.Id,
                NarrationContentId = audio.NarrationContentId,
                TtsVoiceProfileId = audio.TtsVoiceProfileId,
                TtsVoiceProfileDisplayName = audio.TtsVoiceProfile?.DisplayName,
                TtsVoiceProfileDescription = audio.TtsVoiceProfile?.Description,
                TtsVoiceProfileLanguageName = audio.TtsVoiceProfile?.Language?.Name,
                AudioUrl = audio.AudioUrl,
                BlobId = audio.BlobId,
                Voice = audio.Voice,
                Provider = audio.Provider,
                DurationSeconds = audio.DurationSeconds,
                IsTts = audio.IsTts,
                UpdatedAt = ConvertFromUtc(audio.UpdatedAt, timeZone)
            };
        }

    }
}
