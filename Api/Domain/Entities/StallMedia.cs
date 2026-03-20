namespace Api.Domain.Entities
{
    public class StallMedia
    {
        public Guid Id { get; set; }                // DEFAULT NEWSEQUENTIALID()
        public Guid StallId { get; set; }           // FK -> Stalls(Id)
        public string MediaUrl { get; set; } = null!;  // nvarchar(512) NOT NULL
        public string MediaType { get; set; } = null!; // nvarchar(32) NOT NULL
        public string? Caption { get; set; }        // nvarchar(256)
        public int SortOrder { get; set; }          // int NOT NULL DEFAULT 0
        public bool IsActive { get; set; }          // bit NOT NULL DEFAULT 1

        // Navigation
        public Stall Stall { get; set; } = null!;

    }
}
