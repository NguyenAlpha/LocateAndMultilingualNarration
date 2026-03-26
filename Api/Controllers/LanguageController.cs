using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Languages;

namespace Api.Controllers
{
    /// <summary>
    /// Controller quản lý Language cho Admin
    /// </summary>
    [ApiController]
    [Route("api/languages")]
    public class LanguageController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LanguageController> _logger;

        /// <summary>
        /// Khởi tạo LanguageController với các dependencies cần thiết
        /// </summary>
        /// <param name="context">Database context để truy vấn dữ liệu</param>
        /// <param name="logger">Logger để ghi log</param>
        public LanguageController(AppDbContext context, ILogger<LanguageController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tạo mới language
        /// </summary>
        /// <param name="request">Thông tin tạo language</param>
        /// <returns>Language vừa tạo</returns>
        /// <response code="200">Tạo thành công</response>
        /// <response code="401">Không xác thực</response>
        /// <response code="403">Không có quyền truy cập</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateLanguage([FromBody] LanguageCreateDto request)
        {
            _logger.LogInformation("Bắt đầu tạo language - Code: {Code}", request.Code);

            var code = NormalizeCode(request.Code);
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("Language code không hợp lệ khi tạo");
                return this.BadRequestResult("Language code không hợp lệ", "Code");
            }

            var exists = await _context.Languages.AnyAsync(l => l.Code == code);
            if (exists)
            {
                _logger.LogWarning("Language code đã tồn tại - Code: {Code}", code);
                return this.ConflictResult("Language code đã tồn tại", "Code");
            }

            var language = new Api.Domain.Entities.Language
            {
                Name = request.Name,
                Code = code,
                IsActive = request.IsActive
            };

            _context.Languages.Add(language);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tạo language thành công - LanguageId: {LanguageId}", language.Id);

            return this.OkResult(MapLanguageDetail(language));
        }

        /// <summary>
        /// Cập nhật language theo Id
        /// </summary>
        /// <param name="id">Id của language</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Language sau khi cập nhật</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="401">Không xác thực</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="404">Không tìm thấy language</response>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateLanguage(Guid id, [FromBody] LanguageUpdateDto request)
        {
            _logger.LogInformation("Bắt đầu cập nhật language - Id: {LanguageId}", id);

            var language = await _context.Languages.FirstOrDefaultAsync(l => l.Id == id);
            if (language == null)
            {
                _logger.LogWarning("Không tìm thấy language - Id: {LanguageId}", id);
                return this.NotFoundResult("Không tìm thấy language");
            }

            var code = NormalizeCode(request.Code);
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("Language code không hợp lệ khi cập nhật - Id: {LanguageId}", id);
                return this.BadRequestResult("Language code không hợp lệ", "Code");
            }

            if (!string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _context.Languages.AnyAsync(l => l.Code == code && l.Id != id);
                if (exists)
                {
                    _logger.LogWarning("Language code đã tồn tại khi cập nhật - Code: {Code}", code);
                    return this.ConflictResult("Language code đã tồn tại", "Code");
                }
            }

            language.Name = request.Name;
            language.Code = code;
            language.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cập nhật language thành công - Id: {LanguageId}", id);

            return this.OkResult(MapLanguageDetail(language));
        }

        /// <summary>
        /// Lấy danh sách language
        /// </summary>
        /// <param name="isActive">Lọc theo trạng thái</param>
        /// <returns>Danh sách language</returns>
        /// <response code="200">Trả về danh sách language</response>
        /// <response code="401">Không xác thực</response>
        /// <response code="403">Không có quyền truy cập</response>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetLanguages([FromQuery] bool? isActive = null)
        {
            _logger.LogInformation("Bắt đầu lấy danh sách language");

            var query = _context.Languages.AsNoTracking();
            if (isActive.HasValue)
            {
                query = query.Where(l => l.IsActive == isActive.Value);
            }

            var languages = await query
                .OrderBy(l => l.Name)
                .ToListAsync();

            var result = languages.Select(MapLanguageDetail).ToList();

            _logger.LogInformation("Lấy danh sách language thành công - TotalCount: {TotalCount}", result.Count);

            return this.OkResult(result);
        }

        /// <summary>
        /// Lấy danh sách language đang active (public)
        /// </summary>
        /// <returns>Danh sách language active</returns>
        /// <response code="200">Trả về danh sách language active</response>
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveLanguages()
        {
            _logger.LogInformation("Bắt đầu lấy danh sách language active");

            var languages = await _context.Languages
                .AsNoTracking()
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();

            var result = languages.Select(MapLanguageDetail).ToList();

            _logger.LogInformation("Lấy danh sách language active thành công - TotalCount: {TotalCount}", result.Count);

            return this.OkResult(result);
        }

        /// <summary>
        /// Deactive language theo Id
        /// </summary>
        /// <param name="id">Id của language</param>
        /// <returns>Language sau khi deactive</returns>
        /// <response code="200">Deactive thành công</response>
        /// <response code="401">Không xác thực</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="404">Không tìm thấy language</response>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateLanguage(Guid id)
        {
            _logger.LogInformation("Bắt đầu deactive language - Id: {LanguageId}", id);

            var language = await _context.Languages.FirstOrDefaultAsync(l => l.Id == id);
            if (language == null)
            {
                _logger.LogWarning("Không tìm thấy language - Id: {LanguageId}", id);
                return this.NotFoundResult("Không tìm thấy language");
            }

            if (language.IsActive)
            {
                language.IsActive = false;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Deactive language thành công - Id: {LanguageId}", id);

            return this.OkResult(MapLanguageDetail(language));
        }

        private static string NormalizeCode(string input)
        {
            return input.Trim().ToLowerInvariant();
        }

        private static LanguageDetailDto MapLanguageDetail(Api.Domain.Entities.Language language)
        {
            return new LanguageDetailDto
            {
                Id = language.Id,
                Name = language.Name,
                DisplayName = language.DisplayName,
                Code = language.Code,
                FlagCode = language.FlagCode,
                IsActive = language.IsActive
            };
        }
    }
}
