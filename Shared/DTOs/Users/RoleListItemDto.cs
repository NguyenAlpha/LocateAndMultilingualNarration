namespace Shared.DTOs.Users
{
    public class RoleListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int UserCount { get; set; }
    }
}
