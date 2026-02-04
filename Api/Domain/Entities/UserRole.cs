namespace Api.Domain.Entities
{
    public class UserRole
    {
        public Guid UserId { get; set; }  // FK -> Users(Id)
        public Guid RoleId { get; set; }  // FK -> Roles(Id)

        // Navigation
        public User User { get; set; } = null!;
        public Role Role { get; set; } = null!;

    }
}
