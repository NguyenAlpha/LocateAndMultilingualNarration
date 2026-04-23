namespace Api.Domain.Entities
{
    public class Tour
    {
        public Guid Id { get; set; }                              // DEFAULT NEWSEQUENTIALID()
        public string Name { get; set; } = null!;                 // nvarchar(128) NOT NULL
        public string? Description { get; set; }                  // nvarchar(1024)
        public int? EstimatedMinutes { get; set; }                // int
        public bool IsActive { get; set; }                        // bit NOT NULL DEFAULT 1
        public Guid CreatedByUserId { get; set; }                 // FK -> Users(Id)
        public DateTimeOffset CreatedAt { get; set; }             // datetimeoffset NOT NULL DEFAULT SYSDATETIMEOFFSET()
        public DateTimeOffset? UpdatedAt { get; set; }            // datetimeoffset

        // Navigation
        public User CreatedByUser { get; set; } = null!;
        public ICollection<TourStop> Stops { get; set; } = new List<TourStop>();
    }
}
