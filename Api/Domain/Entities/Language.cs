namespace Api.Domain.Entities
{
    public class Language
    {
        public Guid Id { get; set; }                 // DEFAULT NEWSEQUENTIALID()
        public string Name { get; set; } = null!;    // nvarchar(64) NOT NULL
        public string Code { get; set; } = null!;    // nvarchar(16) UNIQUE NOT NULL
        public bool IsActive { get; set; }           // bit NOT NULL DEFAULT 1

        // Navigation
        public ICollection<VisitorProfile> VisitorProfiles { get; set; } = new List<VisitorProfile>();

    }
}
