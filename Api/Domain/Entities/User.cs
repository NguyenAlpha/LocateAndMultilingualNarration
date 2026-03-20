namespace Api.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }                     // DEFAULT NEWSEQUENTIALID()
        public string? UserName { get; set; }            // nvarchar(256)
        public string? NormalizedUserName { get; set; }  // nvarchar(256) UNIQUE
        public string? Email { get; set; }               // nvarchar(256)
        public string? NormalizedEmail { get; set; }     // nvarchar(256)
        public bool EmailConfirmed { get; set; }         // bit NOT NULL DEFAULT 0
        public string? PasswordHash { get; set; }        // nvarchar(256)
        public string? SecurityStamp { get; set; }       // nvarchar(256)
        public string? ConcurrencyStamp { get; set; }    // nvarchar(256)
        public string? PhoneNumber { get; set; }         // nvarchar(32)
        public bool PhoneNumberConfirmed { get; set; }   // bit NOT NULL DEFAULT 0
        public string? DisplayName { get; set; }         // nvarchar(256)
        public string? Sex { get; set; }                 // nvarchar(16)
        public DateTime? DateOfBirth { get; set; }       // DATE
        public bool TwoFactorEnabled { get; set; }       // bit NOT NULL DEFAULT 0
        public DateTimeOffset? LockoutEnd { get; set; }  // datetimeoffset(3)
        public bool LockoutEnabled { get; set; }         // bit NOT NULL DEFAULT 1
        public int AccessFailedCount { get; set; }       // int NOT NULL DEFAULT 0
        public DateTime? LastLoginAt { get; set; }       // datetime2(3)
        public DateTime CreatedAt { get; set; }          // datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME()
        public DateTime UpdatedAt { get; set; }          // datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME()
        public DateTime? DeletedAt { get; set; }         // datetime2(3)
        public bool IsActive { get; set; }               // bit NOT NULL DEFAULT 1

        // Navigation
        public BusinessOwnerProfile? BusinessOwnerProfile { get; set; }
        public VisitorProfile? VisitorProfile { get; set; }
        public VisitorPreference? VisitorPreference { get; set; }
        public EmployeeProfile? EmployeeProfile { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<Business> Businesses { get; set; } = new List<Business>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<VisitorLocationLog> VisitorLocationLogs { get; set; } = new List<VisitorLocationLog>();

    }
}
