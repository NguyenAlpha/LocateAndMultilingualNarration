using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Geo;

namespace Api.Application.Services
{
    public interface IGeoService
    {
        Task<GeoNearestStallDto?> FindNearestStallAsync(decimal latitude, decimal longitude, string? languageCode, decimal? radiusMeters, CancellationToken cancellationToken);
        Task<List<GeoStallDto>> GetAllStallsAsync(string? deviceId, CancellationToken cancellationToken);
    }

    public class GeoService : IGeoService
    {
        private readonly AppDbContext _context;

        public GeoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<GeoNearestStallDto?> FindNearestStallAsync(decimal latitude, decimal longitude, string? languageCode, decimal? radiusMeters, CancellationToken cancellationToken)
        {
            // L?y c�c v? tr� gian h�ng �ang ho?t �?ng k�m th�ng tin gian h�ng v� doanh nghi?p.
            var query = _context.StallLocations
                .AsNoTracking()
                .Where(l => l.IsActive)
                .Include(l => l.Stall)
                .ThenInclude(s => s.Business)
                .AsQueryable();

            if (radiusMeters.HasValue)
            {
                // Khoanh v�ng s� b? theo h?nh ch? nh?t �? gi?m s? l�?ng ?ng vi�n.
                var radius = (double)radiusMeters.Value;
                var lat = (double)latitude;
                var lng = (double)longitude;
                var latDelta = radius / 111_000d;
                var lngDelta = radius / (111_000d * Math.Cos(ToRadians(lat)));

                var minLat = (decimal)(lat - latDelta);
                var maxLat = (decimal)(lat + latDelta);
                var minLng = (decimal)(lng - lngDelta);
                var maxLng = (decimal)(lng + lngDelta);

                query = query.Where(l => l.Latitude >= minLat && l.Latitude <= maxLat && l.Longitude >= minLng && l.Longitude <= maxLng);
            }

            var candidates = await query
                .Where(l => l.Stall.IsActive && l.Stall.Business.IsActive)
                .ToListAsync(cancellationToken);

            if (candidates.Count == 0)
            {
                return null;
            }

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

            if (radiusMeters.HasValue && nearest.Distance > radiusMeters.Value)
            {
                return null;
            }

            // T?i n?i dung thuy?t minh theo ng�n ng? n?u c� y�u c?u.
            string? contentText = null;
            string? audioUrl = null;

            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                // Tra c?u ng�n ng? v� n?i dung thuy?t minh m?i nh?t c?a gian h�ng.
                var languageId = await _context.Languages
                    .AsNoTracking()
                    .Where(l => l.Code == languageCode)
                    .Select(l => l.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (languageId != Guid.Empty)
                {
                    var narration = await _context.StallNarrationContents
                        .AsNoTracking()
                        .Include(n => n.NarrationAudios)
                        .FirstOrDefaultAsync(n => n.StallId == nearest.Location.StallId
                            && n.LanguageId == languageId
                            && n.IsActive, cancellationToken);

                    if (narration != null)
                    {
                        contentText = narration.ScriptText;
                        audioUrl = narration.NarrationAudios
                            .OrderByDescending(a => a.UpdatedAt)
                            .Select(a => a.AudioUrl)
                            .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
                    }
                }
            }

            return new GeoNearestStallDto
            {
                // Tr? v? gian h�ng g?n nh?t v� kho?ng c�ch �? l�m tr?n.
                StallId = nearest.Location.StallId,
                StallName = nearest.Location.Stall.Name,
                DistanceMeters = decimal.Round(nearest.Distance, 2, MidpointRounding.AwayFromZero),
                ContentText = contentText,
                AudioUrl = audioUrl
            };
        }

        public async Task<List<GeoStallDto>> GetAllStallsAsync(string? deviceId, CancellationToken cancellationToken)
        {
            // Bước 1: resolve LanguageId + Voice từ DevicePreference hoặc fallback về "vi"
            Guid languageId;
            string? preferredVoice = null;

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                var pref = await _context.DevicePreferences
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.DeviceId == deviceId, cancellationToken);

                if (pref != null)
                {
                    languageId = pref.LanguageId;
                    preferredVoice = pref.Voice;
                }
                else
                {
                    languageId = await GetFallbackLanguageIdAsync(cancellationToken);
                }
            }
            else
            {
                languageId = await GetFallbackLanguageIdAsync(cancellationToken);
            }

            // Bước 2: query stalls kèm narration content đã lọc theo ngôn ngữ
            var locations = await _context.StallLocations
                .AsNoTracking()
                .Where(l => l.IsActive && l.Stall.IsActive && l.Stall.Business.IsActive)
                .Include(l => l.Stall)
                    .ThenInclude(s => s.StallNarrationContents
                        .Where(c => c.IsActive && c.LanguageId == languageId))
                    .ThenInclude(c => c.NarrationAudios)
                .ToListAsync(cancellationToken);

            // Bước 3: map sang DTO kèm AudioUrl
            return locations.Select(l =>
            {
                var content = l.Stall.StallNarrationContents.FirstOrDefault();
                return new GeoStallDto
                {
                    StallId      = l.StallId,
                    StallName    = l.Stall.Name,
                    Latitude     = (double)l.Latitude,
                    Longitude    = (double)l.Longitude,
                    RadiusMeters = (double)l.RadiusMeters,
                    AudioUrl     = PickAudioUrl(content?.NarrationAudios, preferredVoice)
                };
            }).ToList();
        }

        private async Task<Guid> GetFallbackLanguageIdAsync(CancellationToken cancellationToken)
        {
            var language = await _context.Languages
                .AsNoTracking()
                .Where(l => l.IsActive)
                .OrderBy(l => l.Code == "vi" ? 0 : 1)
                .FirstOrDefaultAsync(cancellationToken);

            return language?.Id ?? Guid.Empty;
        }

        private static string? PickAudioUrl(IEnumerable<Api.Domain.Entities.NarrationAudio>? audios, string? preferredVoice)
        {
            if (audios is null) return null;

            var list = audios.Where(a => !string.IsNullOrWhiteSpace(a.AudioUrl)).ToList();
            if (list.Count == 0) return null;

            // Ưu tiên 1: khớp voice preference của thiết bị
            if (!string.IsNullOrWhiteSpace(preferredVoice))
            {
                var voiceMatch = list.FirstOrDefault(a => a.Voice == preferredVoice);
                if (voiceMatch != null) return voiceMatch.AudioUrl;
            }

            // Ưu tiên 2: audio TTS tự sinh
            var tts = list.FirstOrDefault(a => a.IsTts);
            if (tts != null) return tts.AudioUrl;

            // Ưu tiên 3: bất kỳ audio đầu tiên có URL
            return list[0].AudioUrl;
        }

        private static decimal CalculateDistanceMeters(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            // T�nh kho?ng c�ch theo c�ng th?c Haversine.
            var r = 6_371_000d;
            var dLat = ToRadians((double)(lat2 - lat1));
            var dLon = ToRadians((double)(lon2 - lon1));
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = r * c;
            return (decimal)distance;
        }

        private static double ToRadians(double degrees) => degrees * (Math.PI / 180d);
    }
}
