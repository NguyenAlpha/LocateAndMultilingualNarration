using Shared.DTOs.Businesses;
using Shared.DTOs.Languages;
using Shared.DTOs.Stalls;
using Shared.DTOs.SubscriptionOrders;

namespace Web.Models
{
    public class AdminDashboardViewModel
    {
        // --- Stats thật: core ---
        public int TotalBusinesses { get; set; }
        public int TotalStalls { get; set; }
        public int ActiveLanguages { get; set; }
        public int TotalNarrationContents { get; set; }

        // --- Stats thật: Users ---
        public int TotalUsers { get; set; }

        // --- Stats thật: QR Codes ---
        public int TotalQrCodes { get; set; }
        public int UsedQrCodes { get; set; }

        // --- Stats thật: Subscription Orders ---
        public int CompletedOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        // --- Danh sách mới nhất (thật) ---
        public IReadOnlyList<BusinessDetailDto> RecentBusinesses { get; set; } = [];
        public IReadOnlyList<StallDetailDto> RecentStalls { get; set; } = [];
        public IReadOnlyList<LanguageDetailDto> Languages { get; set; } = [];
        public IReadOnlyList<SubscriptionOrderDetailDto> RecentOrders { get; set; } = [];
    }
}
