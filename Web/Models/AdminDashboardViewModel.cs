using Shared.DTOs.Businesses;
using Shared.DTOs.Languages;
using Shared.DTOs.Stalls;

namespace Web.Models
{
    public class AdminDashboardViewModel
    {
        // --- Stats thật từ DB ---
        public int TotalBusinesses { get; set; }
        public int TotalStalls { get; set; }
        public int ActiveLanguages { get; set; }
        public int TotalNarrationContents { get; set; }

        // --- Danh sách mới nhất (thật) ---
        public IReadOnlyList<BusinessDetailDto> RecentBusinesses { get; set; } = [];
        public IReadOnlyList<StallDetailDto> RecentStalls { get; set; } = [];
        public IReadOnlyList<LanguageDetailDto> Languages { get; set; } = [];
    }
}
