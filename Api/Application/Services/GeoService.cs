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
        /// Tìm gian hàng gần nhất với tọa độ GPS được cung cấp,
        /// kèm nội dung thuyết minh theo ngôn ngữ yêu cầu.
        /// </summary>
        Task<GeoNearestStallDto?> FindNearestStallAsync(decimal latitude, decimal longitude, string? languageCode, decimal? radiusMeters, CancellationToken cancellationToken);

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
        // EF Core DbContext — truy cập database
        private readonly AppDbContext _context;
        private readonly ILogger<GeoService> _logger;

        public GeoService(AppDbContext context, ILogger<GeoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tìm gian hàng gần nhất theo thuật toán:
        ///   1. Lọc sơ bộ bằng bounding box hình chữ nhật (nhanh, dùng được index DB)
        ///   2. Tính khoảng cách Haversine chính xác trên tập kết quả nhỏ
        ///   3. Kiểm tra lại radius chính xác sau khi sắp xếp
        ///   4. Tải nội dung thuyết minh theo ngôn ngữ (nếu có yêu cầu)
        /// </summary>
        public async Task<GeoNearestStallDto?> FindNearestStallAsync(decimal latitude, decimal longitude, string? languageCode, decimal? radiusMeters, CancellationToken cancellationToken)
        {
            // Bước 1: Lấy các vị trí gian hàng đang hoạt động kèm thông tin gian hàng và doanh nghiệp
            // AsNoTracking: không cần theo dõi thay đổi vì chỉ đọc, giúp tăng hiệu suất
            var query = _context.StallLocations
                .AsNoTracking()
                .Where(l => l.IsActive)
                .Include(l => l.Stall)
                .ThenInclude(s => s.Business)
                .AsQueryable();

            if (radiusMeters.HasValue)
            {
                // Bước 1b: Thu hẹp sơ bộ bằng hình chữ nhật bao quanh bán kính
                // Mục đích: giảm số record phải tính Haversine (tốn CPU hơn)
                // 111_000 mét ≈ 1 độ vĩ độ; kinh độ thu hẹp theo cos(lat) khi xa xích đạo
                var radius = (double)radiusMeters.Value;
                var lat = (double)latitude;
                var lng = (double)longitude;
                var latDelta = radius / 111_000d;
                var lngDelta = radius / (111_000d * Math.Cos(ToRadians(lat)));

                var minLat = (decimal)(lat - latDelta);
                var maxLat = (decimal)(lat + latDelta);
                var minLng = (decimal)(lng - lngDelta);
                var maxLng = (decimal)(lng + lngDelta);

                query = query.Where(l => l.Latitude >= minLat && l.Latitude <= maxLat
                                      && l.Longitude >= minLng && l.Longitude <= maxLng);
            }

            // Bước 2: Thực thi query — chỉ lấy gian hàng và doanh nghiệp đang hoạt động
            var candidates = await query
                .Where(l => l.Stall.IsActive && l.Stall.Business.IsActive)
                .ToListAsync(cancellationToken);

            if (candidates.Count == 0)
            {
                return null; // Không có ứng viên nào trong vùng
            }

            // Bước 3: Tính khoảng cách Haversine chính xác cho từng ứng viên, lấy gần nhất
            var nearest = candidates
                .Select(l => new
                {
                    Location = l,
                    Distance = CalculateDistanceMeters(latitude, longitude, l.Latitude, l.Longitude)
                })
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            if (nearest == null)
            {
                return null;
            }

            // Bước 3b: Kiểm tra lại radius chính xác — bounding box có thể bao gồm điểm ngoài vòng tròn
            if (radiusMeters.HasValue && nearest.Distance > radiusMeters.Value)
            {
                return null; // Gian hàng gần nhất vẫn nằm ngoài bán kính yêu cầu
            }

            // Bước 4: Tải nội dung thuyết minh theo ngôn ngữ (nếu có yêu cầu)
            string? contentText = null;
            string? audioUrl = null;

            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                // Tìm LanguageId từ code (vd: "vi", "en", "fr")
                var languageId = await _context.Languages
                    .AsNoTracking()
                    .Where(l => l.Code == languageCode)
                    .Select(l => l.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (languageId != Guid.Empty)
                {
                    // Lấy nội dung thuyết minh đang active của gian hàng gần nhất theo ngôn ngữ
                    var narration = await _context.StallNarrationContents
                        .AsNoTracking()
                        .Include(n => n.NarrationAudios)
                        .FirstOrDefaultAsync(n => n.StallId == nearest.Location.StallId
                            && n.LanguageId == languageId
                            && n.IsActive, cancellationToken);

                    if (narration != null)
                    {
                        contentText = narration.ScriptText;

                        // Lấy audio URL mới nhất có URL hợp lệ (không null/rỗng)
                        audioUrl = narration.NarrationAudios
                            .OrderByDescending(a => a.UpdatedAt)
                            .Select(a => a.AudioUrl)
                            .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
                    }
                }
            }

            // Bước 5: Trả về DTO kết quả với khoảng cách làm tròn 2 chữ số thập phân
            return new GeoNearestStallDto
            {
                StallId = nearest.Location.StallId,
                StallName = nearest.Location.Stall.Name,
                DistanceMeters = decimal.Round(nearest.Distance, 2, MidpointRounding.AwayFromZero),
                ContentText = contentText,
                AudioUrl = audioUrl
            };
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
            if (_logger.IsEnabled(LogLevel.Information))
            {
                var deviceIdLog = deviceId ?? "(none)";
                _logger.LogInformation("GetAllStallsAsync: bắt đầu — DeviceId: {DeviceId}", deviceIdLog);
            }

            // Bước 1: Resolve ngôn ngữ và giọng đọc từ DevicePreference
            Guid languageId;
            string? preferredVoice = null;

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
                    preferredVoice = pref.Voice;
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
                        .ThenInclude(c => c.NarrationAudios);
            }
            else
            {
                // languageId = Guid.Empty: không tìm được ngôn ngữ fallback
                // → bỏ filter ngôn ngữ, lấy tất cả content đang active để tránh NarrationAudios rỗng
                locationsQuery = baseQuery
                    .Include(l => l.Stall)
                        .ThenInclude(s => s.StallNarrationContents
                            .Where(c => c.IsActive))
                        .ThenInclude(c => c.NarrationAudios);
            }

            var locations = await locationsQuery.ToListAsync(cancellationToken);
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("GetAllStallsAsync: truy vấn được {Total} vị trí gian hàng", locations.Count);

            // Bước 3: Map từng StallLocation sang GeoStallDto
            var result = locations.Select(l =>
            {
                var contents = l.Stall.StallNarrationContents; // đã được lọc IsActive = true từ query

                // Narration content theo ngôn ngữ ưu tiên — dùng để chọn AudioUrl chính
                var preferredContent = languageId != Guid.Empty
                    ? contents.FirstOrDefault(c => c.LanguageId == languageId)
                    : contents.FirstOrDefault();

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
                    }).FirstOrDefault()
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
        private static string? PickAudioUrl(IEnumerable<NarrationAudio>? audios, string? preferredVoice)
        {
            if (audios is null) return null;

            // Lọc ra các audio có URL hợp lệ (bỏ qua null/rỗng)
            var list = audios.Where(a => !string.IsNullOrWhiteSpace(a.AudioUrl)).ToList();
            if (list.Count == 0) return null;

            // Ưu tiên 1: khớp voice preference của thiết bị (so sánh qua TtsVoiceProfileId)
            if (!string.IsNullOrWhiteSpace(preferredVoice))
            {
                var voiceMatch = list.FirstOrDefault(a => a.TtsVoiceProfileId?.ToString() == preferredVoice);
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
