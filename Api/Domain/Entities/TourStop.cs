namespace Api.Domain.Entities
{
    public class TourStop
    {
        public Guid Id { get; set; }                              // DEFAULT NEWSEQUENTIALID()
        public Guid TourId { get; set; }                          // FK -> Tours(Id)
        public Guid StallId { get; set; }                         // FK -> Stalls(Id)
        public int Order { get; set; }                            // int NOT NULL
        public string? Note { get; set; }                         // nvarchar(256)
        public DateTimeOffset CreatedAt { get; set; }             // datetimeoffset NOT NULL DEFAULT SYSDATETIMEOFFSET()

        // Navigation
        public Tour Tour { get; set; } = null!;
        public Stall Stall { get; set; } = null!;
    }
}
