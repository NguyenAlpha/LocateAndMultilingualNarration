using Api.Domain.Entities;

namespace Api.Infrastructure.Persistence.Seeds
{
    public static class BusinessOwnerSeeder
    {
        private const string DefaultPassword = "Owner@123";
        private const int TotalUsers = 100;

        // Họ tiếng Việt phổ biến
        private static readonly string[] LastNames =
        [
            "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng",
            "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Đinh", "Tô", "Trịnh", "Cao"
        ];

        // Tên đệm + tên tiếng Việt
        private static readonly string[] MiddleAndFirstNames =
        [
            "Văn An", "Thị Bình", "Minh Châu", "Quốc Dũng", "Thị Hoa", "Văn Hùng",
            "Thị Lan", "Đức Long", "Thị Mai", "Văn Nam", "Thị Nga", "Quang Phúc",
            "Thị Quỳnh", "Văn Sơn", "Thị Thảo", "Minh Tuấn", "Thị Uyên", "Văn Việt",
            "Thị Xuân", "Công Thành", "Thị Yến", "Hoàng Anh", "Ngọc Bảo", "Thị Cẩm",
            "Văn Đạt", "Thị Điệp", "Minh Đức", "Thị Giang", "Văn Hiếu", "Thị Hạnh",
            "Quốc Huy", "Thị Huyền", "Văn Khoa", "Thị Linh", "Thanh Lịch", "Văn Lộc",
            "Thị Lý", "Công Nghĩa", "Thị Nhung", "Văn Phong", "Thị Phương", "Đình Quân",
            "Thị Quyên", "Văn Sang", "Thị Tâm", "Hữu Thắng", "Thị Thu", "Văn Tiến",
            "Thị Trang", "Minh Trí"
        ];

        // Loại hình doanh nghiệp
        private static readonly string[] BusinessTypes =
        [
            "Công ty TNHH",
            "Công ty Cổ phần",
            "Hộ kinh doanh",
            "Cơ sở",
            "Doanh nghiệp tư nhân",
            "Công ty TNHH MTV"
        ];

        // Từ khoá mô tả lĩnh vực
        private static readonly string[] BusinessKeywords =
        [
            "Ẩm Thực", "Thương Mại", "Dịch Vụ", "Thực Phẩm", "Nhà Hàng",
            "Bếp Việt", "Hương Vị", "Đặc Sản", "Tinh Hoa", "Sơn Nam",
            "Phương Đông", "Hương Đồng", "Quê Nhà", "Bếp Quê", "Vị Ngon",
            "Phú Quốc", "Hội An", "Hà Nội", "Sài Gòn", "Huế",
            "Bắc Hương", "Nam Phong", "Trung Việt", "Miền Tây", "Miền Trung",
            "Đại Dương", "Tây Nguyên", "Mekong", "Phồn Thịnh", "Thuận Phát",
            "Vạn Thành", "Cát Tường", "Bình An", "Thịnh Vượng", "Đồng Tâm",
            "Kim Ngân", "Phúc Lộc", "Thiên Phúc", "Long Thành", "Hồng Phát"
        ];

        // Hậu tố bổ sung tạo sự đa dạng
        private static readonly string[] BusinessSuffixes =
        [
            "& Cộng Sự", "Việt Nam", "Group", "Food", "Trading",
            "Phát Triển", "Hợp Tác", "Liên Doanh", "", "", ""  // "" để không phải lúc nào cũng có hậu tố
        ];

        public static async Task SeedAsync(AppDbContext db)
        {
            // Idempotent: skip nếu đã có >= 50 business owner
            if (db.Users.Count(u => u.UserRoles.Any(ur => ur.Role.NormalizedName == "BUSINESSOWNER")) >= 50)
                return;

            var businessOwnerRole = db.Roles.First(r => r.NormalizedName == "BUSINESSOWNER");
            var random = new Random(42); // seed cố định để kết quả nhất quán
            var now = DateTime.UtcNow;
            var seedStart = now.AddDays(-365);

            var usedUsernames = new HashSet<string>(db.Users.Select(u => u.NormalizedUserName ?? ""));
            var usedBusinessNames = new HashSet<string>(db.Businesses.Select(b => b.Name));

            for (int i = 1; i <= TotalUsers; i++)
            {
                var lastName = LastNames[random.Next(LastNames.Length)];
                var middleFirst = MiddleAndFirstNames[random.Next(MiddleAndFirstNames.Length)];
                var displayName = $"{lastName} {middleFirst}";

                var baseUsername = $"owner{i:D3}";
                var username = baseUsername;
                while (usedUsernames.Contains(username.ToUpperInvariant()))
                    username = $"{baseUsername}_{random.Next(100)}";
                usedUsernames.Add(username.ToUpperInvariant());

                var email = $"{username}@business.local";
                var userId = Guid.NewGuid();

                var userCreatedAt = seedStart.AddSeconds(random.NextInt64((long)(now - seedStart).TotalSeconds));

                var user = new User
                {
                    Id                 = userId,
                    UserName           = username,
                    NormalizedUserName = username.ToUpperInvariant(),
                    Email              = email,
                    NormalizedEmail    = email.ToUpperInvariant(),
                    EmailConfirmed     = true,
                    PasswordHash       = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
                    SecurityStamp      = Guid.NewGuid().ToString(),
                    ConcurrencyStamp   = Guid.NewGuid().ToString(),
                    DisplayName        = displayName,
                    IsActive           = true,
                    LockoutEnabled     = true,
                    CreatedAt          = userCreatedAt,
                    UpdatedAt          = userCreatedAt
                };

                var profile = new BusinessOwnerProfile
                {
                    Id          = Guid.NewGuid(),
                    UserId      = userId,
                    OwnerName   = displayName,
                    ContactInfo = $"0{random.Next(300_000_000, 999_999_999)}",
                    CreatedAt   = userCreatedAt
                };

                db.Users.Add(user);
                db.BusinessOwnerProfiles.Add(profile);
                db.UserRoles.Add(new UserRole { UserId = userId, RoleId = businessOwnerRole.Id });

                // Mỗi user sở hữu 1–3 business
                int businessCount = random.Next(1, 4);
                for (int b = 0; b < businessCount; b++)
                {
                    var businessName = GenerateUniqueBusinessName(random, usedBusinessNames);
                    usedBusinessNames.Add(businessName);

                    // Business tạo sau user tối đa 30 ngày, không vượt quá now
                    var maxBusinessOffset = Math.Min(30, (now - userCreatedAt).Days);
                    var businessCreatedAt = new DateTimeOffset(userCreatedAt.AddDays(random.Next(0, maxBusinessOffset + 1)));

                    db.Businesses.Add(new Business
                    {
                        Id            = Guid.NewGuid(),
                        Name          = businessName,
                        TaxCode       = GenerateTaxCode(random),
                        ContactEmail  = $"lienhe.{username}.b{b + 1}@business.local",
                        ContactPhone  = $"0{random.Next(300_000_000, 999_999_999)}",
                        OwnerUserId   = userId,
                        CreatedAt     = businessCreatedAt,
                        IsActive      = true,
                        Plan          = Api.Domain.SubscriptionPlan.Free,
                        PlanExpiresAt = null
                    });
                }
            }

            await db.SaveChangesAsync();
        }

        private static string GenerateUniqueBusinessName(Random random, HashSet<string> used)
        {
            var type    = BusinessTypes[random.Next(BusinessTypes.Length)];
            var keyword = BusinessKeywords[random.Next(BusinessKeywords.Length)];
            var suffix  = BusinessSuffixes[random.Next(BusinessSuffixes.Length)];

            var baseName = string.IsNullOrEmpty(suffix)
                ? $"{type} {keyword}"
                : $"{type} {keyword} {suffix}";

            var candidate = baseName;
            int seq = 2;
            while (used.Contains(candidate))
                candidate = $"{baseName} {seq++}";

            return candidate;
        }

        private static string GenerateTaxCode(Random random)
        {
            // Mã số thuế 10 chữ số (dạng thực tế của Việt Nam)
            var prefix = random.Next(1000, 9999).ToString();
            var suffix = random.Next(100000, 999999).ToString();
            return $"{prefix}{suffix}";
        }
    }
}
