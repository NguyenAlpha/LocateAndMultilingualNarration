using Api.Domain;
using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence.Seeds
{
    public static class StallSeeder
    {
        // ── Điều chỉnh tại đây ─────────────────────────────────
        private const bool  LimitTotalStalls = true;   // true = bật giới hạn, false = tạo bình thường
        private const int   MaxTotalStalls   = 15;     // chỉ có hiệu lực khi LimitTotalStalls = true

        private const decimal CenterLat = 10.777370m;  // tọa độ trung tâm phố ẩm thực
        private const decimal CenterLng = 106.710677m;
        private const double  MaxOffsetDegrees = 0.002; // ~220m quanh trung tâm
        // ────────────────────────────────────────────────────────

        // (Tên hiển thị, slug base đã latin hoá)
        private static readonly (string Name, string SlugBase)[] StallNames =
        [
            ("Quán Phở Bắc",          "pho-bac"),
            ("Bún Bò Huế",            "bun-bo-hue"),
            ("Bánh Mì Sài Gòn",       "banh-mi-sai-gon"),
            ("Cơm Tấm Đặc Biệt",      "com-tam-dac-biet"),
            ("Bánh Xèo Miền Tây",     "banh-xeo-mien-tay"),
            ("Chè Thái",              "che-thai"),
            ("Hủ Tiếu Nam Vang",      "hu-tieu-nam-vang"),
            ("Bún Riêu Cua",          "bun-rieu-cua"),
            ("Gỏi Cuốn Tôm Thịt",    "goi-cuon-tom-thit"),
            ("Bánh Cuốn Nóng",        "banh-cuon-nong"),
            ("Bún Chả Hà Nội",        "bun-cha-ha-noi"),
            ("Mì Quảng Đà Nẵng",      "mi-quang-da-nang"),
            ("Cháo Lòng Sườn",        "chao-long-suon"),
            ("Nem Nướng Nha Trang",   "nem-nuong-nha-trang"),
            ("Lẩu Thái Hải Sản",      "lau-thai-hai-san"),
            ("Bánh Tráng Trộn",       "banh-trang-tron"),
            ("Xôi Mặn Đặc Biệt",      "xoi-man-dac-biet"),
            ("Súp Cua Biển",          "sup-cua-bien"),
            ("Cơm Chiên Dương Châu",  "com-chien-duong-chau"),
            ("Bánh Canh Cua",         "banh-canh-cua"),
        ];

        public static async Task SeedAsync(AppDbContext db)
        {
            // Idempotent: skip nếu số stall hiện có đã đạt hoặc vượt MaxTotalStalls
            var existingCount = await db.Stalls.CountAsync();
            if (existingCount >= MaxTotalStalls)
                return;

            var random   = new Random(42);
            var now      = DateTimeOffset.UtcNow;
            var usedSlugs = new HashSet<string>();
            int totalCreated = existingCount; // bắt đầu đếm từ số stall hiện có

            var businesses = await db.Businesses.ToListAsync();

            foreach (var business in businesses)
            {
                if (LimitTotalStalls && totalCreated >= MaxTotalStalls)
                    break;

                // Số stall tối đa theo plan
                int maxForPlan = SubscriptionPlan.GetMaxStalls(business.Plan);
                maxForPlan = Math.Min(maxForPlan, 5); // cap Pro ở 5 cho seed

                if (maxForPlan == 0)
                    continue;

                int stallCount = random.Next(1, maxForPlan + 1);

                if (LimitTotalStalls)
                    stallCount = Math.Min(stallCount, MaxTotalStalls - totalCreated);

                for (int s = 0; s < stallCount; s++)
                {
                    var nameEntry = StallNames[random.Next(StallNames.Length)];
                    var slug      = GenerateUniqueSlug(nameEntry.SlugBase, totalCreated + 1, usedSlugs);
                    usedSlugs.Add(slug);

                    // Stall tạo sau business, random trong khoảng từ business.CreatedAt đến now
                    var availableSeconds = (now - business.CreatedAt).TotalSeconds;
                    var stallCreatedAt   = business.CreatedAt.AddSeconds(random.NextDouble() * availableSeconds);

                    // Tọa độ random quanh trung tâm
                    var lat = CenterLat + (decimal)((random.NextDouble() * 2 - 1) * MaxOffsetDegrees);
                    var lng = CenterLng + (decimal)((random.NextDouble() * 2 - 1) * MaxOffsetDegrees);

                    var stallId = Guid.NewGuid();

                    db.Stalls.Add(new Stall
                    {
                        Id          = stallId,
                        BusinessId  = business.Id,
                        Name        = nameEntry.Name,
                        Description = $"Gian hàng {nameEntry.Name} tại phố ẩm thực",
                        Slug        = slug,
                        IsActive    = true,
                        CreatedAt   = stallCreatedAt
                    });

                    db.StallLocations.Add(new StallLocation
                    {
                        Id           = Guid.NewGuid(),
                        StallId      = stallId,
                        Latitude     = Math.Round(lat, 6),
                        Longitude    = Math.Round(lng, 6),
                        RadiusMeters = random.Next(15, 35),   // 25–50m
                        Address      = $"Phố Ẩm Thực, Quận 1, TP.HCM",
                        IsActive     = true,
                        UpdatedAt    = stallCreatedAt
                    });

                    totalCreated++;

                    if (LimitTotalStalls && totalCreated >= MaxTotalStalls)
                        break;
                }
            }

            await db.SaveChangesAsync();
        }

        // Tạo slug duy nhất: slugBase → slugBase-1, slugBase-2, ...
        private static string GenerateUniqueSlug(string slugBase, int globalIndex, HashSet<string> used)
        {
            // Thử dùng index toàn cục trước cho ngắn gọn
            var candidate = $"{slugBase}-{globalIndex}";
            if (!used.Contains(candidate))
                return candidate;

            int suffix = globalIndex + 1;
            do { candidate = $"{slugBase}-{suffix++}"; }
            while (used.Contains(candidate));

            return candidate;
        }

    }
}
