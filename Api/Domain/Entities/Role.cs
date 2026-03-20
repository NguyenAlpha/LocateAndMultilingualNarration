namespace Api.Domain.Entities
{
    public class Role
    {
        public Guid Id { get; set; }                   // DEFAULT NEWSEQUENTIALID()
        public string Name { get; set; } = null!;      // nvarchar(256) UNIQUE NOT NULL
        public string NormalizedName { get; set; } = null!; // nvarchar(256) UNIQUE NOT NULL
        public string? ConcurrencyStamp { get; set; }  // nvarchar(256)

        // Navigation
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    }
}
