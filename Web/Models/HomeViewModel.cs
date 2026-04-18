namespace Web.Models
{
    public class HomeViewModel
    {
        // Stats từ API
        public int TotalBusinesses { get; set; }   // [Authorize] – chỉ khi đã login
        public int TotalStalls { get; set; }        // [Authorize] – chỉ khi đã login
        public int TotalLanguages { get; set; }     // [AllowAnonymous] – luôn lấy được
        public int TotalUsers { get; set; }         // [AdminOnly] – chỉ Admin

        // Thông tin session
        public bool IsLoggedIn { get; set; }
        public bool IsAdmin { get; set; }
        public string UserRole { get; set; } = "";
        public string UserPlan { get; set; } = "";
        public bool PlanIsExpired { get; set; }
        public string EffectivePlan { get; set; } = "";
        public DateTimeOffset? PlanExpiresAt { get; set; }
    }
}
