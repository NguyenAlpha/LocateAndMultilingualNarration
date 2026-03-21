namespace Api.Domain.Entities
{
    public class VisitorLocationLog
    {
        public Guid Id { get; set; }                 // DEFAULT NEWSEQUENTIALID()
        public Guid UserId { get; set; }             // FK -> Users(Id)
        public decimal Latitude { get; set; }        // decimal(9,6) NOT NULL
        public decimal Longitude { get; set; }       // decimal(9,6) NOT NULL
        public decimal? AccuracyMeters { get; set; } // decimal(10,2)
        public DateTime CapturedAtUtc { get; set; }  // datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME()

        // Navigation
        public User User { get; set; } = null!;

    }
}
