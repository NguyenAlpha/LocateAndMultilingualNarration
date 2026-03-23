namespace Shared.DTOs.VisitorLocationLogs
{
    public class VisitorLocationLogDetailDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal? AccuracyMeters { get; set; }
        public DateTime CapturedAtUtc { get; set; }
    }
}
