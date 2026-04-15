using Shared.DTOs.Users;

namespace Web.Models
{
    public class UserRoleManagementViewModel
    {
        public List<UserListItemDto> Users { get; set; } = [];
        public List<RoleListItemDto> Roles { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string? Search { get; set; }
        public string? RoleFilter { get; set; }
        public bool? IsActiveFilter { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
