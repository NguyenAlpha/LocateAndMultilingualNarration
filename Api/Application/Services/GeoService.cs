using Api.Domain.Entities;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Geo;

namespace Api.Application.Services
{
    /// <summary>
    /// Contract cho GeoService — xử lý các tác vụ liên quan đến vị trí địa lý gian hàng.
    /// </summary>
    public interface IGeoService
    {
        /// <summary>
        /// Lấy toàn bộ danh sách gian hàng đang hoạt động,
        /// kèm AudioUrl được chọn theo ngôn ngữ và giọng đọc ưa thích của thiết bị.
        /// </summary>
        Task<List<GeoStallDto>> GetAllStallsAsync(string? deviceId, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Implementation của IGeoService.
    /// Thực hiện truy vấn database, tính toán khoảng cách địa lý và chọn audio phù hợp.
    /// </summary>
    public class GeoService : IGeoService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GeoService> _logger;

        public GeoService(AppDbContext context, ILogger<GeoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy toàn bộ gian hàng đang hoạt động và chọn AudioUrl phù hợp.
        /// Luồng xử lý:
        ///   1. Resolve ngôn ngữ + giọng đọc từ DevicePreference (hoặc fallback "vi")
        ///   2. Query stalls kèm narration content lọc theo ngôn ngữ
        ///   3. Map sang DTO với AudioUrl được chọn theo ưu tiên voice
        /// </summary>
        public async Task<List<GeoStallDto>> GetAllStallsAsync(string? deviceId, CancellationToken cancellationToken)
        {
            var deviceIdLog = deviceId ?? "(none)";
            _logger.LogInformation("GetAllStallsAsync: bắt đầu — DeviceId: {DeviceId}", deviceIdLog);

            // Bước 1: Resolve ngôn ngữ và giọng đọc từ DevicePreference
            Guid languageId;
            Guid? preferredVoice = null;

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                // Tìm preference đã lưu của thiết bị (ngôn ngữ + giọng đọc)
                var pref = await _context.DevicePreferences
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.DeviceId == deviceId, cancellationToken);

                if (pref != null)
                {
                    // Thiết bị đã từng chọn ngôn ngữ → dùng preference đó
                    languageId = pref.LanguageId;
                    preferredVoice = pref.VoiceId;
                }
                else
                {
                    // Thiết bị chưa có preference → fallback về ngôn ngữ mặc định ("vi")
                    languageId = await GetFallbackLanguageIdAsync(cancellationToken);
                }
            }
            else
            {
                // Không có deviceId → không xác định được thiết bị → dùng fallback
                languageId = await GetFallbackLanguageIdAsync(cancellationToken);
            }

            // Bước 2: Query stalls đang hoạt động kèm narration content theo ngôn ngữ
            // AsNoTracking: chỉ đọc, không cần EF theo dõi thay đổi → nhanh hơn
            var baseQuery = _context.StallLocations
                .AsNoTracking()
                .Where(l => l.IsActive && l.Stall.IsActive && l.Stall.Business.IsActive);

            IQueryable<StallLocation> locationsQuery;
            if (languageId != Guid.Empty)
            {
                // Có ngôn ngữ xác định: lọc narration content theo đúng ngôn ngữ đó
                // Filtered Include (EF Core 5+): .Where() bên trong ThenInclude lọc dữ liệu liên quan
                locationsQuery = baseQuery
                    .Include(l => l.Stall)
                        .ThenInclude(s => s.StallNarrationContents
                            .Where(c => c.IsActive))
                        .ThenInclude(c => c.NarrationAudios)
                    .Include(l => l.Stall)
                        .ThenInclude(s => s.StallMedia
                            .Where(m => m.IsActive && m.MediaType == "image")
                            .OrderBy(m => m.SortOrder));
            }
            else
            {
                // languageId = Guid.Empty: không tìm được ngôn ngữ fallback
                // → bỏ filter ngôn ngữ, lấy tất cả content đang active để tránh NarrationAudios rỗng
                locationsQuery = baseQuery
                    .Include(l => l.Stall)
                        .ThenInclude(s => s.StallNarrationContents
                            .Where(c => c.IsActive))
                        .ThenInclude(c => c.NarrationAudios)
                    .Include(l => l.Stall)
                        .ThenInclude(s => s.StallMedia
                            .Where(m => m.IsActive && m.MediaType == "image")
                            .OrderBy(m => m.SortOrder));
            }

            var locations = await locationsQuery.ToListAsync(cancellationToken);
            _logger.LogInformation("GetAllStallsAsync: truy vấn được {Total} vị trí gian hàng", locations.Count);

            // Bước 3: Map từng StallLocation sang GeoStallDto
            var result = locations.Select(l =>
            {
                var contents = l.Stall.StallNarrationContents; // đã được lọc IsActive = true từ query

                return new GeoStallDto
                {
                    StallId = l.StallId,
                    StallName = l.Stall.Name,
                    Latitude = (double)l.Latitude,
                    Longitude = (double)l.Longitude,
                    RadiusMeters = (double)l.RadiusMeters,
                    NarrationContent = contents.Select(c => new GeoStallNarrationContentDto
                    {
                        Id          = c.Id,
                        LanguageId  = c.LanguageId,
                        Title       = c.Title,
                        Description = c.Description,
                        ScriptText  = c.ScriptText,
                        UpdatedAt   = c.UpdatedAt,
                        AudioUrl    = PickAudioUrl(c.NarrationAudios, preferredVoice)
                    }).FirstOrDefault(),
                    MediaImages = l.Stall.StallMedia
                        .Select(m => new GeoStallMediaDto { Url = m.MediaUrl, Caption = m.Caption })
                        .ToList()
                };
            }).ToList();

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("GetAllStallsAsync: hoàn tất — trả về {Total} gian hàng", result.Count);
            return result;
        }

        /// <summary>
        /// Lấy ngôn ngữ mặc định fallback — ưu tiên tiếng Việt ("vi"),
        /// nếu không có thì lấy ngôn ngữ active đầu tiên trong DB.
        /// Trả về Guid.Empty nếu DB không có ngôn ngữ nào active.
        /// </summary>
        private async Task<Guid> GetFallbackLanguageIdAsync(CancellationToken cancellationToken)
        {
            var language = await _context.Languages
                .AsNoTracking()
                .Where(l => l.IsActive)
                // Sắp xếp: "vi" lên đầu (0), tất cả ngôn ngữ khác xếp sau (1)
                .OrderBy(l => l.Code == "vi" ? 0 : 1)
                .FirstOrDefaultAsync(cancellationToken);

            return language?.Id ?? Guid.Empty;
        }

        /// <summary>
        /// Chọn AudioUrl từ danh sách audio của một narration content
        /// theo thứ tự ưu tiên:
        ///   1. Khớp với voice preference của thiết bị (TtsVoiceProfileId)
        ///   2. Audio được sinh bởi TTS (IsTts = true)
        ///   3. Bất kỳ audio nào có URL hợp lệ (fallback cuối cùng)
        /// </summary>
        private static string? PickAudioUrl(IEnumerable<NarrationAudio>? audios, Guid? preferredVoice)
        {
            if (audios is null) return null;

            // Lọc ra các audio có URL hợp lệ (bỏ qua null/rỗng)
            var list = audios.Where(a => !string.IsNullOrWhiteSpace(a.AudioUrl)).ToList();
            if (list.Count == 0) return null;

            // Ưu tiên 1: khớp voice preference của thiết bị (so sánh qua TtsVoiceProfileId)
            if (preferredVoice.HasValue)
            {
                var voiceMatch = list.FirstOrDefault(a => a.TtsVoiceProfileId == preferredVoice.Value);
                if (voiceMatch != null) return voiceMatch.AudioUrl;
            }

            // Ưu tiên 2: audio TTS tự sinh (chất lượng đồng nhất, không cần upload thủ công)
            var tts = list.FirstOrDefault(a => a.IsTts);
            if (tts != null) return tts.AudioUrl;

            // Ưu tiên 3: bất kỳ audio đầu tiên có URL (fallback cuối cùng)
            return list[0].AudioUrl;
        }

        /// <summary>
        /// Tính khoảng cách giữa 2 điểm GPS bằng công thức Haversine.
        /// Haversine tính khoảng cách trên mặt cầu, chính xác hơn công thức phẳng
        /// khi 2 điểm ở xa nhau hoặc gần cực.
        /// Bán kính Trái Đất dùng: 6,371,000 mét (6,371 km).
        /// </summary>
        private static decimal CalculateDistanceMeters(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            var r = 6_371_000d; // Bán kính trung bình của Trái Đất (mét)

            var dLat = ToRadians((double)(lat2 - lat1)); // Hiệu vĩ độ (radian)
            var dLon = ToRadians((double)(lon2 - lon1)); // Hiệu kinh độ (radian)

            // Công thức Haversine: a = sin²(Δlat/2) + cos(lat1)·cos(lat2)·sin²(Δlon/2)
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2))
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            // c = 2·atan2(√a, √(1−a)) — góc trung tâm (radian)
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return (decimal)(r * c); // Khoảng cách = bán kính × góc trung tâm
        }

        /// <summary>Chuyển đổi góc từ độ sang radian.</summary>
        private static double ToRadians(double degrees) => degrees * (Math.PI / 180d);
    }
}
