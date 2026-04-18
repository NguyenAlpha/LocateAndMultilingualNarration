namespace Shared.DTOs.Users
{
    public class UserDetailDto
    {
        public Guid Id { get; set; }
        public string? UserName { get; set; }
        public string? NormalizedUserName { get; set; }
        public string? Email { get; set; }
        public string? NormalizedEmail { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public string? DisplayName { get; set; }
        public string? Sex { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsActive { get; set; }
        public IEnumerable<string> Roles { get; set; } = new List<string>();
        public BusinessOwnerProfileDto? BusinessOwnerProfile { get; set; }
public EmployeeProfileDto? EmployeeProfile { get; set; }
    }

    public class BusinessOwnerProfileDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? OwnerName { get; set; }
        public string? ContactInfo { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }


    public class EmployeeProfileDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
