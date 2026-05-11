using Api.Domain;
using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence.Seeds
{
    // Seeder Saigon Food - thay the toan bo stall cu bang 15 quan an noi tieng SG.
    // Co che idempotent: dung business "system" co Id co dinh lam marker.
    // - Khi business da ton tai => da seed roi, bo qua.
    // - Khi chua co => xoa toan bo Stalls (cascade Location/Media/Narration/Audio)
    //   roi tao moi 15 quan an + narration tieng Viet + anh minh hoa.
    // Narration luu o TtsStatus=Pending de TtsBackgroundService tu dong sinh audio
    // cho cac ngon ngu khac qua Azure Translator + Azure Speech.
    public static class FamousSaigonFoodSeeder
    {
        // Id co dinh business he thong - marker idempotent
        private static readonly Guid SystemBusinessId = new("a11ce7e0-5a1c-4001-9001-5a1c0000feed");

        public static async Task SeedAsync(AppDbContext db)
        {
            var alreadySeeded = await db.Businesses.AnyAsync(b => b.Id == SystemBusinessId);
            if (alreadySeeded) return;

            // Buoc 2: tao business he thong, Plan=Pro de TTS service chap nhan
            var now = DateTimeOffset.UtcNow;
            var business = new Business
            {
                Id = SystemBusinessId,
                Name = "VinhThuc Saigon Food Tour",
                ContactEmail = "system@vinhthucaudioguide.local",
                CreatedAt = now,
                IsActive = true,
                Plan = SubscriptionPlan.Pro
            };
            db.Businesses.Add(business);

            // Buoc 3: resolve LanguageId tieng Viet (DB thuong da seed san vi-VN)
            var viLanguage = await db.Languages
                .FirstOrDefaultAsync(l => l.Code == "vi-VN" || l.Code == "vi");
            if (viLanguage == null)
            {
                viLanguage = new Language
                {
                    Id = Guid.NewGuid(),
                    Code = "vi-VN",
                    Name = "Vietnamese",
                    DisplayName = "Tieng Viet",
                    FlagCode = "vn",
                    IsActive = true
                };
                db.Languages.Add(viLanguage);
            }

            // Buoc 4: seed 15 quan
            foreach (var poi in FamousSaigonFoodSeederData.Pois)
            {
                var stallId = Guid.NewGuid();
                var slug = SlugFromName(poi.Name) + "-" + stallId.ToString("N")[..6];

                db.Stalls.Add(new Stall
                {
                    Id = stallId,
                    BusinessId = business.Id,
                    Name = poi.Name,
                    Description = poi.ShortDesc,
                    Slug = slug,
                    IsActive = true,
                    CreatedAt = now
                });

                db.StallLocations.Add(new StallLocation
                {
                    Id = Guid.NewGuid(),
                    StallId = stallId,
                    Latitude = poi.Lat,
                    Longitude = poi.Lng,
                    RadiusMeters = 25m,
                    Address = poi.Address,
                    IsActive = true,
                    UpdatedAt = now
                });

                db.StallMedia.Add(new StallMedia
                {
                    Id = Guid.NewGuid(),
                    StallId = stallId,
                    MediaUrl = poi.ImageUrl,
                    MediaType = "image",
                    Caption = poi.Name,
                    SortOrder = 0,
                    IsActive = true
                });

                // Narration tieng Viet, TtsStatus=Pending de TtsBackgroundService
                // tu dong sinh audio cho cac ngon ngu khac qua Azure Translator + Speech.
                db.StallNarrationContents.Add(new StallNarrationContent
                {
                    Id = Guid.NewGuid(),
                    StallId = stallId,
                    LanguageId = viLanguage.Id,
                    Title = poi.Name,
                    Description = poi.ShortDesc,
                    ScriptText = poi.NarrationVi,
                    IsActive = true,
                    TtsStatus = TtsJobStatus.Pending,
                    UpdatedAt = now
                });
            }

            await db.SaveChangesAsync();
        }

        // Bo dau tieng Viet, lower-case, thay khoang trang bang '-' de tao slug
        private static string SlugFromName(string name)
        {
            var normalized = name.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalized)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
                else if (char.IsWhiteSpace(c) || c == '-') sb.Append('-');
            }
            var slug = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
            // ky tu d-bar khong tach duoc qua FormD, xu ly tay
            return string.IsNullOrEmpty(slug) ? "stall" : slug.Replace("\u0111", "d");
        }
    }

    // Ban ghi mot POI Sai Gon dung trong seeder
    internal sealed class SaigonPoiRecord
    {
        public string Name { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public decimal Lat { get; init; }
        public decimal Lng { get; init; }
        public string ShortDesc { get; init; } = string.Empty;
        public string ImageUrl { get; init; } = string.Empty;
        public string NarrationVi { get; init; } = string.Empty;
    }
}
