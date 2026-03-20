using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Api.Domain.Entities;

namespace Api.Domain.Entities
{
    public class Stall
    {
        public Guid Id { get; set; }                              // DEFAULT NEWSEQUENTIALID()
        public Guid BusinessId { get; set; }                      // FK -> Businesses(Id)
        public string Name { get; set; } = null!;                 // nvarchar(128) NOT NULL
        public string? Description { get; set; }                  // nvarchar(256)
        public string Slug { get; set; } = null!;                 // nvarchar(256) UNIQUE NOT NULL
        public string? ContactEmail { get; set; }                 // nvarchar(256)
        public string? ContactPhone { get; set; }                 // nvarchar(16)
        public bool IsActive { get; set; }                        // bit NOT NULL DEFAULT 1
        public DateTimeOffset CreatedAt { get; set; }             // datetimeoffset NOT NULL DEFAULT SYSDATETIMEOFFSET()
        public DateTimeOffset? UpdatedAt { get; set; }            // datetimeoffset

        // Navigation
        public Business Business { get; set; } = null!;
        public ICollection<StallGeoFence> StallGeoFences { get; set; } = new List<StallGeoFence>();
        public ICollection<StallLocation> StallLocations { get; set; } = new List<StallLocation>();
        public ICollection<StallMedia> StallMedia { get; set; } = new List<StallMedia>();
        public ICollection<StallNarrationContent> StallNarrationContents { get; set; } = new List<StallNarrationContent>();

    }
}
