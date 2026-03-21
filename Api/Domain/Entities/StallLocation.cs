namespace Api.Domain.Entities
{
    public class StallLocation
    {
        public Guid Id { get; set; }                      // DEFAULT NEWSEQUENTIALID()
        public Guid StallId { get; set; }                 // FK -> Stalls(Id)
        public decimal Latitude { get; set; }            // decimal(9,6) NOT NULL
        public decimal Longitude { get; set; }           // decimal(9,6) NOT NULL
        public decimal RadiusMeters { get; set; }        // decimal(10,2) NOT NULL
        public string? Address { get; set; }             // nvarchar(256)
        public string? MapProviderPlaceId { get; set; }  // nvarchar(128)
        public DateTimeOffset? UpdatedAt { get; set; }   // datetimeoffset
        public bool IsActive { get; set; }               // bit NOT NULL DEFAULT 1

        // Navigation
        public Stall Stall { get; set; } = null!;

    }
}
