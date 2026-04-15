namespace Shared.DTOs.Users
{
    public class UserListItemDto
    {
        public Guid Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public List<string> Roles { get; set; } = [];
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
