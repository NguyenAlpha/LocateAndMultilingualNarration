using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/audiocontent")]
[AllowAnonymous]
public class AudioContentController : ControllerBase
{
    private readonly AppDbContext _context;

    public AudioContentController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("sync")]
    public async Task<IActionResult> SyncData([FromQuery] DateTimeOffset? lastSync)
    {
        var stallQuery = _context.Stalls
            .Include(s => s.StallLocations)
            .Include(s => s.StallMedia)
            .Where(s => s.IsActive);

        if (lastSync.HasValue)
        {
            stallQuery = stallQuery.Where(s => s.UpdatedAt >= lastSync || s.CreatedAt >= lastSync);
        }

        var locations = await stallQuery
            .Select(s => new
            {
                ServerId = s.Id.ToString(),
                Name = s.Name,
                Address = s.StallLocations.Where(l => l.IsActive).Select(l => l.Address).FirstOrDefault() ?? "",
                ImageUrl = s.StallMedia.Where(m => m.IsActive).OrderBy(m => m.SortOrder).Select(m => m.MediaUrl).FirstOrDefault(),
                Latitude = s.StallLocations.Where(l => l.IsActive).Select(l => (double)l.Latitude).FirstOrDefault(),
                Longitude = s.StallLocations.Where(l => l.IsActive).Select(l => (double)l.Longitude).FirstOrDefault(),
                IsActive = s.IsActive
            })
            .ToListAsync();

        // Thêm FlagCode để mobile hiển thị cờ quốc gia khi chọn ngôn ngữ thuyết minh.
        // FlagCode dạng ISO-2 lowercase (vn, us, fr, jp, kr, cn...) - mobile sẽ
        // tự convert sang emoji 🇻🇳, 🇺🇸... bằng regional indicator symbols.
        var languages = await _context.Languages
            .Where(l => l.IsActive)
            .Select(l => new
            {
                ServerId = l.Id.ToString(),
                Code = l.Code,
                Name = l.DisplayName ?? l.Name,
                FlagCode = l.FlagCode
            })
            .ToListAsync();

        var scriptQuery = _context.StallNarrationContents
            .Where(c => c.Stall.IsActive); // Lấy cả những kịch bản không active để app biết mà ẩn

        if (lastSync.HasValue)
        {
            scriptQuery = scriptQuery.Where(c => c.UpdatedAt >= lastSync);
        }

        var scripts = await scriptQuery
            .Select(c => new
            {
                ServerId = c.Id.ToString(),
                LocationId = c.StallId.ToString(),
                LanguageId = c.LanguageId.ToString(),
                Title = c.Title,
                Content = c.ScriptText,
                IsActive = c.IsActive
            })
            .ToListAsync();

        return Ok(new
        {
            Locations = locations,
            Languages = languages,
            Scripts = scripts
        });
    }
}