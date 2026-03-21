namespace Api.Domain.Entities
{
    public class StallGeoFence
    {
        public Guid Id { get; set; }                // DEFAULT NEWSEQUENTIALID()
        public Guid StallId { get; set; }           // FK -> Stalls(Id)
        public string PolygonJson { get; set; } = null!; // nvarchar(max) NOT NULL
        public int? MinZoom { get; set; }           // int
        public int? MaxZoom { get; set; }           // int

        // Navigation
        public Stall Stall { get; set; } = null!;

    }
}
